using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace DZNetwork
{
    public class DZUDPSocket
    {
        //TODO:: implement reliable acknowledgements
        public static int PacketSequence = 0;
        public static int PacketAcknowledgement = 0;
        public static int PacketAcknowledgementBitField = 0;

        protected byte[] ReceiveBuffer;

        protected Socket Socket;
        protected EndPoint EndPoint = new IPEndPoint(IPAddress.Any, 0);

        public DZUDPSocket(int BufferSize, AddressFamily AddressFamily = AddressFamily.InterNetwork)
        {
            ReceiveBuffer = new byte[BufferSize];
            Socket = new Socket(AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            //https://stackoverflow.com/questions/38191968/c-sharp-udp-an-existing-connection-was-forcibly-closed-by-the-remote-host
            Socket.IOControl(
                (IOControlCode)(-1744830452),
                new byte[] { 0, 0, 0, 0 },
                null
            ); //Ignores UDP exceptions
        }

        protected void BeginReceive()
        {
            Socket.BeginReceiveFrom(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, ref EndPoint, ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult Result)
        {
            int NumBytesReceived = Socket.EndReceiveFrom(Result, ref EndPoint);
            Socket.BeginReceiveFrom(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, ref EndPoint, ReceiveCallback, null);

            OnReceive(EndPoint, NumBytesReceived);
        }

        protected virtual void OnReceive(EndPoint ReceivedEndPoint, int NumBytesReceived) { }

        public void Send(byte[] Bytes)
        {
            Socket.BeginSend(Bytes, 0, Bytes.Length, SocketFlags.None, null, null);
        }

        public void SendTo(byte[] Bytes, EndPoint Destination)
        {
            Socket.BeginSendTo(Bytes, 0, Bytes.Length, SocketFlags.None, Destination, SendToCallback, null);
        }

        private void SendToCallback(IAsyncResult Result)
        {
            int NumBytesSent = Socket.EndSendTo(Result);
            OnSendTo(NumBytesSent);
        }

        protected virtual void OnSendTo(int NumBytesSent) { }

        public void Dispose()
        {
            Socket.Close();
            OnDispose();
        }

        protected virtual void OnDispose() { }
    }
}
