using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network.TCPUDP
{
    public class TCPUDPClient : IDisposable
    {
        private int TCPDataBufferSize;
        private byte[] TCPReceiveBuffer;
        private NetworkStream TCPStream;
        private Packet TCPReceivedData = new Packet();

        public TcpClient TCPSocket = null;
        public UdpClient UDPSocket = null;
        public IPEndPoint EndPoint;

        public TCPUDPClient(int DataBufferSize)
        {
            this.TCPDataBufferSize = DataBufferSize;
            TCPReceiveBuffer = new byte[DataBufferSize];
        }

        public void UDPConnect(IPEndPoint EndPoint)
        {
            this.EndPoint = EndPoint;
        }

        public void UDPConnect(int LocalPort, string ServerIP, int ServerPort)
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort);

            UDPSocket = new UdpClient(LocalPort);
            UDPSocket.Connect(EndPoint);

            UDPSocket.BeginReceive(UDPReceiveCallback, null);

            Packet Test = new Packet(0);
            Test.Write("UDP Hello From Server");
            UDPSendMessage(Test);
        }

        public void UDPSendMessage(Packet Packet)
        {
            try
            {
                //Check if packet bytes are available, if not convert bytes
                if (Packet.ReadableBuffer == null)
                    Packet.ConvertBufferToArray();

                UDPSocket.BeginSend(Packet.ReadableBuffer, Packet.ReadableBuffer.Length, null, null);
            }
            catch (Exception E)
            {
                Console.WriteLine("TCPUDPClient.UDPSendMessage => " + E);
            }
        }

        private void UDPReceiveCallback(IAsyncResult Result)
        {
            try
            {
                //Get Current Time => for ping calculation
                long Epoch = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;

                byte[] Data = UDPSocket.EndReceive(Result, ref EndPoint);
                UDPSocket.BeginReceive(UDPReceiveCallback, null); //Continue receiving messages

                if (Data.Length < sizeof(int))
                    return;

                UDPHandleData(Data, Epoch);
            }
            catch(Exception E)
            {
                Console.WriteLine("TCPUDPClient.UDPReceiveCallback => " + E);
                DisconnectAll();
            }
        }

        public void UDPHandleData(byte[] Data, long Epoch)
        {
            ServerHandle.ProcessPacket(new Packet(Data), Epoch);
        }

        public async Task TCPConnect(string ServerIP, int ServerPort, int Timeout = -1)
        {
            TCPSocket = new TcpClient();
            TCPSocket.ReceiveBufferSize = TCPDataBufferSize;
            TCPSocket.SendBufferSize = TCPDataBufferSize;

            IAsyncResult TCPConnectionResult = TCPSocket.BeginConnect(ServerIP, ServerPort, TCPConnectCallback, null);

            if (Timeout > 0)
            {
                //Wait for connection to complete
                bool Result = WaitHandle.WaitAll(new[] { TCPConnectionResult.AsyncWaitHandle }, Timeout);
                if (Result == false)
                {
                    throw new TimeoutException();
                    //TODO:: handle timeout exception => dispose and close server
                }
            }
        }

        public void TCPConnect(TcpClient IncomingSocket)
        {
            TCPSocket = IncomingSocket;
            TCPSocket.ReceiveBufferSize = TCPDataBufferSize;
            TCPSocket.SendBufferSize = TCPDataBufferSize;

            TCPStream = TCPSocket.GetStream();
            TCPStream.BeginRead(TCPReceiveBuffer, 0, TCPDataBufferSize, TCPReceiveCallback, null);
        }

        /// <summary>
        /// Sends a given Packet to the client
        /// </summary>
        public void TCPSendMessage(Packet Packet, int ClientIndex = -1)
        {
            try
            {
                //Check if packet bytes are available, if not convert bytes
                if (Packet.ReadableBuffer == null)
                    Packet.ConvertBufferToArray(ClientIndex);

                TCPStream.BeginWrite(Packet.ReadableBuffer, 0, Packet.ReadableBuffer.Length, null, null);
            }
            catch (Exception E)
            {
                Console.WriteLine("TCPUDPClient.TCPSendMessage => " + E);
            }
        }

        private void TCPConnectCallback(IAsyncResult Result)
        {
            try
            {
                TCPSocket.EndConnect(Result);
                if (!TCPSocket.Connected)
                    return;

                TCPStream = TCPSocket.GetStream();
                TCPStream.BeginRead(TCPReceiveBuffer, 0, TCPDataBufferSize, TCPReceiveCallback, null);

                Packet Test = new Packet();
                Test.Write("TCP Hello From Server");
                TCPSendMessage(Test);
            }
            catch (Exception E)
            {
                Console.WriteLine("TCPUDPClient.TCPConnectCallback => " + E);
            }
        }

        /// <summary>
        /// Handles message recieved by the server
        /// </summary>
        /// <param name="Result"></param>
        private void TCPReceiveCallback(IAsyncResult Result)
        {
            try
            {
                //Get Current Time => for ping calculation
                long Epoch = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;

                int ByteLength = TCPStream.EndRead(Result);
                if (ByteLength <= 0)
                {
                    return;
                }

                byte[] Data = new byte[ByteLength];
                Array.Copy(TCPReceiveBuffer, 0, Data, 0, ByteLength);

                //Handle data as NamedPipes are streambased => Packet data can be sent in 2 deliveries causing the packet to be broken up
                //If a packet is sent over 2 deliveries, HandleData will return false such that the currently recieved data is not reset such
                //that the rest of the packet can be recieved
                TCPReceivedData.Reset(TCPHandleData(Data, Epoch));

                //Start read for next packet
                TCPStream.BeginRead(TCPReceiveBuffer, 0, TCPDataBufferSize, TCPReceiveCallback, null);
            }
            catch (Exception E)
            {
                Console.WriteLine("TCPUDPClient.ReceiveCallback => " + E);
                DisconnectAll();
            }
        }

        /// <summary>
        /// Handles received data and cases of missing bytes
        /// </summary>
        private bool TCPHandleData(byte[] Data, long Epoch)
        {
            int PacketLength = 0;
            TCPReceivedData.WriteRange(Data);

            //Check for packet header existing
            if (TCPReceivedData.UnreadLength() >= sizeof(int))
            {
                PacketLength = TCPReceivedData.ReadInt();
                if (PacketLength < 1) //If the header states the length, check if the length is 0, if so return true and reset packet reciever
                    return true;
            }

            //Process the packet fully
            while (PacketLength > 0 && PacketLength <= TCPReceivedData.UnreadLength())
            {
                ServerHandle.ProcessPacket(new Packet(TCPReceivedData.ReadableBuffer), Epoch);

                PacketLength = 0;

                if (TCPReceivedData.UnreadLength() >= sizeof(int))
                {
                    PacketLength = TCPReceivedData.ReadInt();
                    if (PacketLength < 1)
                        return true;
                }
            }

            if (PacketLength <= 1)
                return true;

            return false; //packet is over multiple deliveries, dont reset buffer
        }

        public void TCPDisconnect()
        {
            try
            {
                TCPSocket.Close();
                TCPStream = null;
                TCPReceiveBuffer = new byte[TCPDataBufferSize];
                TCPReceivedData = new Packet();
                TCPSocket = null;
            }
            catch(Exception E)
            {
                Console.WriteLine("TCPUDPClient.TCPDisconnect => " + E);
            }
        }

        public void UDPDisconnect()
        {
            try
            {
                EndPoint = null;

                if (UDPSocket != null)
                {
                    UDPSocket.Close();
                    UDPSocket = null;
                }
            }
            catch (Exception E)
            {
                Console.WriteLine("TCPUDPClient.UDPDisconnect => " + E);
            }
        }

        public void DisconnectAll()
        {
            Console.WriteLine("Disconnecting...");
            TCPDisconnect();
            UDPDisconnect();
        }

        ~TCPUDPClient()
        {
            Dispose(false);
        }

        //https://stackoverflow.com/questions/18336856/implementing-idisposable-correctly
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool Disposing)
        {
            //Dispose managed resources
            if (Disposing)
            {
                if (TCPSocket != null)
                    TCPSocket.Dispose();
                if (TCPStream != null)
                    TCPStream.Dispose();
                if (UDPSocket != null)
                    UDPSocket.Dispose();
            }

            //Disposed unmannaged resources
        }
    }
}
