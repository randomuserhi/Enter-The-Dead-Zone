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
        public readonly int BufferSize;
        protected readonly int BufferStride;

        private object ReceiveBufferLock = new object();
        protected byte[] ReceiveBuffer;

        protected Socket Socket;
        protected EndPoint EndPoint = new IPEndPoint(IPAddress.Any, 0);

        public DZUDPSocket(int BufferSize, AddressFamily AddressFamily = AddressFamily.InterNetwork)
        {
            this.BufferSize = BufferSize;
            BufferStride = BufferSize - PacketHandler.HeaderSize;
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

        private Dictionary<long, PacketReconstructor> PacketsToReconstruct = new Dictionary<long, PacketReconstructor>();
        private class PacketReconstructor
        {
            public int PacketByteCount;
            public int ProcessedPacketCount;
            public byte[] PacketIndex;
            public byte[] Data;
        }

        private void ReceiveCallback(IAsyncResult Result)
        {
            Packet Data = null;
            int NumBytesReceived = 0;
            lock (ReceiveBufferLock)
            {
                NumBytesReceived = Socket.EndReceiveFrom(Result, ref EndPoint);
                Socket.BeginReceiveFrom(ReceiveBuffer, 0, ReceiveBuffer.Length, SocketFlags.None, ref EndPoint, ReceiveCallback, null);

                Data = new Packet(ReceiveBuffer, 0, NumBytesReceived);
            }

            int ReceivedProtocolID = Data.ReadInt();
            if (ReceivedProtocolID != PacketHandler.ProtocolID) return;

            long CurrentEpoch = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;
            long ReceivedEpoch = Data.ReadLong();

            long PacketID = Data.ReadLong();

            int PacketSequence = Data.ReadInt();
            int PacketAcknowledgement = Data.ReadInt();
            int PacketAcknowledgementBitField = Data.ReadInt();

            int PacketByteCount = Data.ReadInt();
            int PacketIndex = Data.ReadInt();

            byte[] ByteData = new byte[NumBytesReceived - PacketHandler.HeaderSize];
            Buffer.BlockCopy(Data.ReadableBuffer, PacketHandler.HeaderSize, ByteData, 0, ByteData.Length);

            if (!PacketsToReconstruct.ContainsKey(PacketID))
            {
                PacketReconstructor Reconstructor = new PacketReconstructor()
                {
                    PacketIndex = new byte[UnityEngine.Mathf.CeilToInt(PacketByteCount / (float)BufferSize)],
                    Data = new byte[PacketByteCount]
                };

                PacketsToReconstruct.Add(PacketID, Reconstructor);
            }
            if (PacketsToReconstruct[PacketID].PacketIndex[PacketIndex] == 0)
            {
                PacketsToReconstruct[PacketID].PacketIndex[PacketIndex] = 1;
                PacketsToReconstruct[PacketID].ProcessedPacketCount += 1;
                Buffer.BlockCopy(ByteData, 0, PacketsToReconstruct[PacketID].Data, PacketIndex * BufferStride, ByteData.Length);

                if (PacketsToReconstruct[PacketID].ProcessedPacketCount == PacketsToReconstruct[PacketID].PacketIndex.Length)
                {
                    OnReceiveConstructedPacket(new Packet(PacketsToReconstruct[PacketID].Data, 0, PacketByteCount), CurrentEpoch - ReceivedEpoch);
                    PacketsToReconstruct.Remove(PacketID);
                }
            }

            OnReceive(EndPoint);
        }

        protected virtual void OnReceive(EndPoint ReceivedEndPoint) { }

        protected virtual void OnReceiveConstructedPacket(Packet Data, long Ping) { }

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
            Socket.Dispose();
            OnDispose();
        }

        protected virtual void OnDispose() { }
    }
}
