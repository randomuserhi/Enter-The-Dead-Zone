using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace DZNetwork
{
    public class DZClient : DZUDPSocket
    {
        public Action<Packet, long> PacketHandle;  //Delegate that is called on recieving a packet
        public Action DisconnectHandle;
        private bool DisconnectTrigger = true;
        public Action ConnectHandle;
        public const int ConnectionLifeTime = 150;
        public uint TicksSinceLastConnection = 0;
        public Action<SentPacketWrapper> PacketLostHandle;

        public bool Connected = false;
        public bool SocketConnected
        {
            get { return Socket.Connected; }
            private set { }
        }

        public DZClient() : base(4096)
        {
            TicksSinceLastConnection = ConnectionLifeTime;
        }

        public void FixedUpdate()
        {
            Tick();

            TicksSinceLastConnection++;
            if (TicksSinceLastConnection > ConnectionLifeTime)
            {
                Connected = false;
                if (!DisconnectTrigger)
                {
                    DisconnectHandle();
                    DisconnectTrigger = true;
                }
            }
            else
            {
                Connected = true;
                DisconnectTrigger = false;
            }
        }

        public void Connect(string Address, int Port)
        {
            Socket.Connect(IPAddress.Parse(Address), Port);
            BeginReceive();
        }

        public void Send(Packet Packet)
        {
            PacketHandler.PacketGroup PacketGroup = PacketHandler.GeneratePackets(Packet);
            for (int i = 0; i < PacketGroup.Packets.Length; i++)
                Send((ushort)(PacketGroup.StartingPacketSequence + i), PacketGroup.Packets[i]);
        }

        Dictionary<long, PacketReconstructor> PacketsToReconstruct = new Dictionary<long, PacketReconstructor>();
        private class PacketReconstructor
        {
            public int PacketByteCount;
            public int ProcessedPacketCount;
            public byte[] PacketIndex;
            public byte[] Data;
        }

        protected override void OnReceive(EndPoint ReceivedEndPoint)
        {
            TicksSinceLastConnection = 0;
            if (Connected == false)
            {
                Connected = true;
                ConnectHandle();
            }
        }

        protected override void OnReceiveConstructedPacket(EndPoint Client, Packet Data, long Ping)
        {
            PacketHandle?.Invoke(Data, Ping);
        }

        protected override void OnPacketLost(SentPacketWrapper Packet)
        {
            PacketLostHandle(Packet);
        }
    }
}
