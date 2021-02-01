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

        public class RecievePacketWrapper
        {
            public EndPoint Client;
            public Packet Data;
        }
        public class SentPacketWrapper
        {
            public EndPoint Client;
            public int Lifetime = 0;
            public ushort PacketSequence = 0;
            public ServerCode Code = ServerCode.Null;
        }
        private Dictionary<PacketIdentifier, SentPacketWrapper> SentPackets = new Dictionary<PacketIdentifier, SentPacketWrapper>();
        private void UpdateAcknowledgedPackets()
        {
            lock (SentPackets)
            {
                List<PacketIdentifier> Keys = SentPackets.Keys.ToList();
                foreach (PacketIdentifier Key in Keys)
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

        private struct PacketIdentifier
        {
            public EndPoint Client;
            public ushort ID;

            public override bool Equals(object Obj)
            {
                return Obj is PacketIdentifier && this == (PacketIdentifier)Obj;
            }
            public static bool operator ==(PacketIdentifier A, PacketIdentifier B)
            {
                if (ReferenceEquals(A, null) && ReferenceEquals(B, null))
                    return true;
                else if (ReferenceEquals(A, null) || ReferenceEquals(B, null))
                    return false;
                return A.ID == B.ID && A.Client.Equals(B.Client);
            }
            public static bool operator !=(PacketIdentifier A, PacketIdentifier B)
            {
                if (ReferenceEquals(A, null) && ReferenceEquals(B, null))
                    return false;
                else if (ReferenceEquals(A, null) || ReferenceEquals(B, null))
                    return true;
                return A.ID != B.ID || !A.Client.Equals(B.Client);
            }
        }
        private int ReconstructedPacketsClear = 0;
        private HashSet<PacketIdentifier> ReconstructedPackets = new HashSet<PacketIdentifier>();
        private Dictionary<PacketIdentifier, PacketReconstructor> PacketsToReconstruct = new Dictionary<PacketIdentifier, PacketReconstructor>();
        private void UpdateReconstructedPackets()
        {
            ReconstructedPacketsClear++;
            if (ReconstructedPacketsClear > PacketLifetime)
                ReconstructedPackets.Clear();
            lock (PacketsToReconstruct)
            {
                List<PacketIdentifier> Keys = PacketsToReconstruct.Keys.ToList();
                foreach (PacketIdentifier Key in Keys)
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
            PacketIdentifier PacketIdentifier = new PacketIdentifier()
            {
                Client = EndPoint,
                ID = PacketID
            };


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
                PacketIdentifier Identifier = new PacketIdentifier()
                {
                    ID = PacketAcknowledgement,
                    Client = EndPoint
                };

                if (SentPackets.ContainsKey(Identifier))
                    SentPackets.Remove(Identifier);
                for (int i = 0; i < 32; i++)
                {
                    Identifier.ID--;
                    if (SentPackets.ContainsKey(Identifier) && ((PacketAcknowledgementBitField & (1 << i)) != 0))
                        SentPackets.Remove(Identifier);
                }
            }

            if (ReconstructedPackets.Contains(PacketIdentifier))
                return; //Duplicate Packet

            int PacketByteCount = Data.ReadInt();
            int PacketIndex = Data.ReadInt();

            byte[] ByteData = new byte[NumBytesReceived - PacketHandler.HeaderSize];
            Buffer.BlockCopy(Data.ReadableBuffer, PacketHandler.HeaderSize, ByteData, 0, ByteData.Length);

            lock (PacketsToReconstruct)
            {
                if (!PacketsToReconstruct.ContainsKey(PacketIdentifier))
                {
                    PacketReconstructor Reconstructor = new PacketReconstructor()
                    {
                        PacketIndex = new byte[UnityEngine.Mathf.CeilToInt(PacketByteCount / (float)BufferSize)], //TODO:: buffer size should be client buffer size
                        Data = new byte[PacketByteCount]
                    };

                    PacketsToReconstruct.Add(PacketIdentifier, Reconstructor);
                }
                if (PacketsToReconstruct[PacketIdentifier].PacketIndex[PacketIndex] == 0)
                {
                    PacketsToReconstruct[PacketIdentifier].PacketIndex[PacketIndex] = 1;
                    PacketsToReconstruct[PacketIdentifier].ProcessedPacketCount += 1;
                    Buffer.BlockCopy(ByteData, 0, PacketsToReconstruct[PacketIdentifier].Data, PacketIndex * BufferStride, ByteData.Length);

                    if (PacketsToReconstruct[PacketIdentifier].ProcessedPacketCount == PacketsToReconstruct[PacketIdentifier].PacketIndex.Length)
                    {
                        OnReceiveConstructedPacket(new RecievePacketWrapper()
                        {
                            Client = EndPoint,
                            Data = new Packet(PacketsToReconstruct[PacketIdentifier].Data, 0, PacketByteCount)
                        }, CurrentEpoch - ReceivedEpoch);

                        PacketsToReconstruct.Remove(PacketIdentifier);
                        ReconstructedPackets.Add(PacketIdentifier);
                    }
                }
            }

            OnReceive(EndPoint);
        }

        protected virtual void OnReceive(EndPoint ReceivedEndPoint) { }

        protected virtual void OnReceiveConstructedPacket(RecievePacketWrapper ReconstructedPacket, long Ping) { }

        public void Send(ushort PacketSequence, ServerCode ServerCode, byte[] Bytes)
        {
            SentPackets.Add(new PacketIdentifier()
            {
                ID = PacketSequence,
                Client = Socket.RemoteEndPoint
            }, new SentPacketWrapper()
            {
                PacketSequence = PacketSequence,
                Code = ServerCode
            });
            Socket.BeginSend(Bytes, 0, Bytes.Length, SocketFlags.None, null, null);
        }

        public void SendTo(ushort PacketSequence, ServerCode ServerCode, byte[] Bytes, EndPoint Destination)
        {
            SentPackets.Add(new PacketIdentifier()
            {
                ID = PacketSequence,
                Client = Destination
            }, new SentPacketWrapper()
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
