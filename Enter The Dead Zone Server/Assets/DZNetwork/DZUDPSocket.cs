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
        public const int PacketLifetime = 30;

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

        public void Tick()
        {
            UpdateReconstructedPackets();
            UpdateAcknowledgedPackets();
        }

        public class SentPacketWrapper
        {
            public EndPoint Client;
            public int Lifetime = 0;
            public ushort PacketSequence = 0;
            public ServerCode Code = ServerCode.Null;
        }
        private Dictionary<ushort, SentPacketWrapper> SentPackets = new Dictionary<ushort, SentPacketWrapper>();
        private void UpdateAcknowledgedPackets()
        {
            lock (SentPackets)
            {
                List<ushort> Keys = SentPackets.Keys.ToList();
                foreach (ushort Key in Keys)
                {
                    SentPackets[Key].Lifetime++;
                    if (SentPackets[Key].Lifetime > PacketLifetime)
                    {
                        OnPacketLost(SentPackets[Key]);
                        SentPackets.Remove(Key);
                    }
                }
            }
        }

        protected virtual void OnPacketLost(SentPacketWrapper Packet) { }

        private int ReconstructedPacketsClear = 0;
        private HashSet<ushort> ReconstructedPackets = new HashSet<ushort>();
        private Dictionary<ushort, PacketReconstructor> PacketsToReconstruct = new Dictionary<ushort, PacketReconstructor>();
        private void UpdateReconstructedPackets()
        {
            ReconstructedPacketsClear++;
            if (ReconstructedPacketsClear > PacketLifetime)
                ReconstructedPackets.Clear();
            lock (PacketsToReconstruct)
            {
                List<ushort> Keys = PacketsToReconstruct.Keys.ToList();
                foreach (ushort Key in Keys)
                {
                    PacketsToReconstruct[Key].Lifetime++;
                    if (PacketsToReconstruct[Key].Lifetime > PacketLifetime)
                        PacketsToReconstruct.Remove(Key);
                }
            }
        }

        private class PacketReconstructor
        {
            public int Lifetime = 0;
            public int PacketByteCount = 0;
            public int ProcessedPacketCount = 0;
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

            ushort PacketID = Data.ReadUShort();

            ushort RemotePacketSequence = Data.ReadUShort();
            ushort PacketAcknowledgement = Data.ReadUShort();
            int PacketAcknowledgementBitField = Data.ReadInt();

            //Update Acknowledgements to return
            if (RemotePacketSequence > PacketHandler.PacketAcknowledgement)
            {
                int SkippedSequences = RemotePacketSequence - PacketHandler.PacketAcknowledgement;
                PacketHandler.PacketAcknowledgement = RemotePacketSequence;
                PacketHandler.PacketAcknowledgementBitField = (PacketHandler.PacketAcknowledgementBitField << SkippedSequences) | (1 << (SkippedSequences - 1));
            }
            else if (RemotePacketSequence < PacketHandler.PacketAcknowledgement)
            {
                int Difference = PacketHandler.PacketAcknowledgement - RemotePacketSequence;
                if (Difference < ushort.MaxValue / 2)
                {
                    int AcknowledgementPosition = PacketHandler.PacketAcknowledgement - RemotePacketSequence;
                    PacketHandler.PacketAcknowledgementBitField = PacketHandler.PacketAcknowledgementBitField | (1 << (AcknowledgementPosition));
                }
                else //Sequence number wrap around
                {
                    int SkippedSequences = ushort.MaxValue - PacketHandler.PacketAcknowledgement + RemotePacketSequence;
                    PacketHandler.PacketAcknowledgement = RemotePacketSequence;
                    //its different to the normal update as the skipped sequences calculation does not include 0 so its 1 behind
                    PacketHandler.PacketAcknowledgementBitField = (PacketHandler.PacketAcknowledgementBitField << (SkippedSequences + 1)) | (1 << SkippedSequences);
                }
            }

            //Update packet queue to check what packets were lost
            lock (SentPackets)
            {
                if (SentPackets.ContainsKey(PacketAcknowledgement))
                    SentPackets.Remove(PacketAcknowledgement);
                ushort Sequence = PacketAcknowledgement;
                for (int i = 0; i < 32; i++)
                {
                    Sequence--;
                    if (SentPackets.ContainsKey(Sequence) && ((PacketAcknowledgementBitField & (1 << i)) != 0))
                        SentPackets.Remove(Sequence);
                }
            }

            if (ReconstructedPackets.Contains(PacketID))
                return; //Duplicate Packet

            int PacketByteCount = Data.ReadInt();
            int PacketIndex = Data.ReadInt();

            byte[] ByteData = new byte[NumBytesReceived - PacketHandler.HeaderSize];
            Buffer.BlockCopy(Data.ReadableBuffer, PacketHandler.HeaderSize, ByteData, 0, ByteData.Length);

            lock (PacketsToReconstruct)
            {
                if (!PacketsToReconstruct.ContainsKey(PacketID))
                {
                    PacketReconstructor Reconstructor = new PacketReconstructor()
                    {
                        PacketIndex = new byte[UnityEngine.Mathf.CeilToInt(PacketByteCount / (float)BufferSize)], //TODO:: buffer size should be client buffer size
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
                        OnReceiveConstructedPacket(EndPoint, new Packet(PacketsToReconstruct[PacketID].Data, 0, PacketByteCount), CurrentEpoch - ReceivedEpoch);
                        PacketsToReconstruct.Remove(PacketID);
                        ReconstructedPackets.Add(PacketID);
                    }
                }
            }

            OnReceive(EndPoint);
        }

        protected virtual void OnReceive(EndPoint ReceivedEndPoint) { }

        protected virtual void OnReceiveConstructedPacket(EndPoint Client, Packet Data, long Ping) { }

        public void Send(ushort PacketSequence, ServerCode ServerCode, byte[] Bytes)
        {
            SentPackets.Add(PacketSequence, new SentPacketWrapper()
            {
                PacketSequence = PacketSequence,
                Code = ServerCode
            });
            Socket.BeginSend(Bytes, 0, Bytes.Length, SocketFlags.None, null, null);
        }

        public void SendTo(ushort PacketSequence, ServerCode ServerCode, byte[] Bytes, EndPoint Destination)
        {
            SentPackets.Add(PacketSequence, new SentPacketWrapper()
            {
                PacketSequence = PacketSequence,
                Code = ServerCode
            });
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
