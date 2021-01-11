using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;

using UnityEngine;

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

        public Action OnTCPClientConnection;

        public TCPUDPClient(int DataBufferSize)
        {
            TCPDataBufferSize = DataBufferSize;
            TCPReceiveBuffer = new byte[DataBufferSize];
        }

        /// <summary>
        /// Connect to server with UDP
        /// </summary>
        public void UDPConnect(int LocalPort, string ServerIP, int ServerPort)
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort);

            UDPSocket = new UdpClient(LocalPort);
            UDPSocket.Connect(EndPoint);

            UDPSocket.BeginReceive(UDPReceiveCallback, null);
        }

        /// <summary>
        /// Send a packet to the server with UDP
        /// </summary>
        /// <param name="ClientIndex">This clients index</param>
        public void UDPSendMessage(Packet Packet, int ClientIndex = -1)
        {
            try
            {
                //Check if packet bytes are available, if not convert bytes
                if (Packet.ReadableBuffer == null)
                    Packet.ConvertBufferToArray(ClientIndex);

                UDPSocket.BeginSend(Packet.ReadableBuffer, Packet.ReadableBuffer.Length, null, null);
            }
            catch (Exception E)
            {
                Debug.Log("TCPUDPClient.UDPSendMessage => " + E);
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
            catch (Exception E)
            {
                Debug.Log("TCPUDPClient.UDPReceiveCallback => " + E);
                DisconnectAll();
            }
        }

        /// <summary>
        /// Handles received data
        /// </summary>
        public void UDPHandleData(byte[] Data, long Epoch)
        {
            ServerHandle.ProcessPacket(new Packet(Data), Epoch);
        }

        /// <summary>
        /// Asynchronously attempts connecting to server
        /// </summary>
        /// <param name="Timeout">Time in milliseconds untill connection will timeout, a value of -1 indicates no timeout</param>
        /// <returns></returns>
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
                Debug.Log("TCPUDPClient.TCPSendMessage => " + E);
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

                OnTCPClientConnection?.Invoke();
            }
            catch (Exception E)
            {
                Debug.Log("TCPUDPClient.TCPConnectCallback => " + E);
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
                Debug.Log("TCPUDPClient.ReceiveCallback => " + E);
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

        /// <summary>
        /// Closes TCP connection
        /// </summary>
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
                Debug.Log("TCPUDPClient.TCPDisconnect => " + E);
            }
        }

        /// <summary>
        /// Closes UDP Connection
        /// </summary>
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
                Debug.Log("TCPUDPClient.UDPDisconnect => " + E);
            }
        }

        /// <summary>
        /// Closes TCP and UDP connections
        /// </summary>
        public void DisconnectAll()
        {
            Debug.Log("Disconnecting...");
            TCPDisconnect();
            UDPDisconnect();
        }

        ~TCPUDPClient()
        {
            Dispose(false);
        }

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
