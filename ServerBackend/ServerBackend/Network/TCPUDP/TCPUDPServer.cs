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
    public class TCPUDPServer : IDisposable
    {
        public readonly int MaxNumberConnections;

        public readonly int Port;
        private TcpListener TCPListener;
        private UdpClient UDPListener;

        private TCPUDPClient[] Clients;

        public TCPUDPServer(int Port, int MaxNumberConnections, int DataBufferSize)
        {
            this.MaxNumberConnections = MaxNumberConnections;
            Clients = new TCPUDPClient[MaxNumberConnections];
            for (int i = 0; i < MaxNumberConnections; i++)
                Clients[i] = new TCPUDPClient(DataBufferSize);

            this.Port = Port;
            TCPListener = new TcpListener(IPAddress.Any, Port);
            UDPListener = new UdpClient(Port);
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
            OnStart();

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

        public virtual void OnStart()
        {

        }

        /// <summary>
        /// Sends a given Packet to a selected client
        /// </summary>
        public void UDPSendMessage(Packet Packet, int ClientIndex)
        {
            try
            {
                if (Clients[ClientIndex].EndPoint == null)
                    return;

                //Check if packet bytes are available, if not convert bytes
                if (Packet.ReadableBuffer == null)
                    Packet.ConvertBufferToArray();

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
            catch (System.Net.Sockets.SocketException)
            {
                Console.WriteLine("Not implemented handle");
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

                    Packet Test = new Packet();
                    Test.Write("TCP Hello From Server");
                    TCPSendMessage(Test, 0);

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
