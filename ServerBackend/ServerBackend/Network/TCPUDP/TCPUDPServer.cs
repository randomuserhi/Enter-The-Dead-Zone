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
    public class _TCPUDPClient : IDisposable
    {
        private int TCPDataBufferSize;
        private byte[] TCPReceiveBuffer;
        private NetworkStream TCPStream;
        private Packet TCPReceivedData = new Packet();

        public TcpClient TCPSocket = null;
        public UdpClient UDPSocket = null;

        public IPEndPoint EndPoint;

        public _TCPUDPClient(int DataBufferSize)
        {
            TCPDataBufferSize = DataBufferSize;
            TCPReceiveBuffer = new byte[DataBufferSize];
        }

        public void UDPConnect(IPEndPoint EndPoint)
        {
            this.EndPoint = EndPoint;
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

        public void UDPHandleData(byte[] Data, long Epoch)
        {
            ServerHandle.ProcessPacket(new Packet(Data), Epoch);
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
            catch (Exception E)
            {
                Console.WriteLine("TCPUDPClient.TCPDisconnect => " + E);
            }
        }

        public void UDPDisconnect()
        {
            EndPoint = null;
        }

        public void DisconnectAll()
        {
            Console.WriteLine("Disconnecting...");
            TCPDisconnect();
            UDPDisconnect();
        }
        ~_TCPUDPClient()
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
            }

            //Disposed unmannaged resources
        }
    }

    public class TCPUDPServer : IDisposable
    {
        public readonly int MaxNumberConnections;

        public readonly int Port;
        private TcpListener TCPListener;
        private UdpClient UDPListener;

        private _TCPUDPClient[] Clients;

        public Action OnStart;
        public Action<int> OnClientConnection;

        public TCPUDPServer(int Port, int MaxNumberConnections, int DataBufferSize)
        {
            this.MaxNumberConnections = MaxNumberConnections;
            Clients = new _TCPUDPClient[MaxNumberConnections];
            for (int i = 0; i < MaxNumberConnections; i++)
                Clients[i] = new _TCPUDPClient(DataBufferSize);

            this.Port = Port;
            TCPListener = new TcpListener(IPAddress.Any, Port);
            UDPListener = new UdpClient(Port);

            //https://stackoverflow.com/questions/38191968/c-sharp-udp-an-existing-connection-was-forcibly-closed-by-the-remote-host
            UDPListener.Client.IOControl(
                (IOControlCode)(-1744830452),
                new byte[] { 0, 0, 0, 0 },
                null
            ); //Currently ignores all UDP exceptions => might want to check this
        }

        /// <summary>
        /// Asynchronously listen for client connection
        /// </summary>
        /// <returns></returns>
        public async Task Connect(int Timeout = -1)
        {
            TCPListener.Start();
            IAsyncResult TCPConnectionResult = TCPListener.BeginAcceptTcpClient(TCPConnectCallback, null);
            UDPListener.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine("Server started on Port: " + Port);

            //Do additional server startup code
            OnStart?.Invoke();

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

        /// <summary>
        /// Sends a given Packet to a selected client
        /// </summary>
        public void UDPSendMessage(Packet Packet, int ClientIndex = -1)
        {
            try
            {
                //Console.WriteLine(Clients[ClientIndex].EndPoint == null);
                if (Clients[ClientIndex].EndPoint == null)
                    return;

                //Check if packet bytes are available, if not convert bytes
                if (Packet.ReadableBuffer == null)
                    Packet.ConvertBufferToArray(ClientIndex);
                
                UDPListener.BeginSend(Packet.ReadableBuffer, Packet.ReadableBuffer.Length, Clients[ClientIndex].EndPoint, null, null);
            }
            catch (Exception E)
            {
                Console.WriteLine("TCPUDPServer.UDPSendMessage => " + E);
            }
        }

        private void UDPReceiveCallback(IAsyncResult Result)
        {
            try
            {
                //Get Current Time => for ping calculation
                long Epoch = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;

                IPEndPoint ClientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] Data = UDPListener.EndReceive(Result, ref ClientEndPoint);
                UDPListener.BeginReceive(UDPReceiveCallback, null); //Continue listening

                if (Data.Length < sizeof(int))
                    return;

                Packet ReceivedData = new Packet(Data);
                int PacketSize = ReceivedData.ReadInt();
                long PacketEpoch = ReceivedData.ReadLong();
                int ClientIndex = ReceivedData.ReadInt();
                
                if (ClientIndex < 0) return;

                if (Clients[ClientIndex].UDPSocket == null)
                {
                    Clients[ClientIndex].UDPConnect(ClientEndPoint);
                }

                //refuse connections from endpoints that dont match
                if (Clients[ClientIndex].EndPoint.ToString() != ClientEndPoint.ToString())
                    return;

                Clients[ClientIndex].UDPHandleData(Data, Epoch); //Might be nicer to just pass ReceivedData rather than creating a new packet
            }
            catch (Exception E)
            {
                Console.WriteLine("TCPUDPServer.UDPReceiveCallback => " + E);
            }
        }

        /// <summary>
        /// Sends a given Packet to a selected client
        /// </summary>
        public void TCPSendMessage(Packet Packet, int ClientIndex)
        {
            try
            {
                Clients[ClientIndex].TCPSendMessage(Packet, ClientIndex);
            }
            catch (Exception E)
            {
                Console.WriteLine("TCPUDPServer.TCPSendMessage => " + E);
            }
        }

        private void TCPConnectCallback(IAsyncResult Result)
        {
            TcpClient Client = TCPListener.EndAcceptTcpClient(Result);
            TCPListener.BeginAcceptTcpClient(TCPConnectCallback, null); //Continue listening for new connections

            for (int i = 0; i < MaxNumberConnections; i++)
            {
                if (Clients[i].TCPSocket == null)
                {
                    Clients[i].TCPConnect(Client);

                    OnClientConnection?.Invoke(i);

                    return;
                }
            }

            Console.WriteLine("Unable to establish connection, server is full");
        }

        ~TCPUDPServer()
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
                TCPListener.Stop();
                UDPListener.Close();
            }

            //Disposed unmannaged resources

            //TODO:: Fix this to properly dispose
            for (int i = 0; i < MaxNumberConnections; i++)
            {
                Clients[i].Dispose();
            }
        }
    }
}
