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

        public bool Connected = false;

        public DZClient() : base(4096)
        {
            TicksSinceLastConnection = ConnectionLifeTime;
        }

        public void FixedUpdate()
        {
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
            Send(Packet.GetBytes());
        }

        protected override void OnReceive(EndPoint ReceivedEndPoint, int NumBytesReceived)
        {
            int ReceivedProtocolID = BitConverter.ToInt32(ReceiveBuffer, 0);
            if (ReceivedProtocolID != HeaderDetails.ProtocolID) return;

            TicksSinceLastConnection = 0;
            if (Connected == false)
            {
                Connected = true;
                ConnectHandle();
            }

            long CurrentEpoch = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;
            long ReceivedEpoch = BitConverter.ToInt64(ReceiveBuffer, sizeof(int));
            int PacketSequence = BitConverter.ToInt32(ReceiveBuffer, sizeof(int) + sizeof(long));
            int PacketAcknowledgement = BitConverter.ToInt32(ReceiveBuffer, sizeof(int) + sizeof(long) + sizeof(int));
            int PacketAcknowledgementBitField = BitConverter.ToInt32(ReceiveBuffer, sizeof(int) + sizeof(long) + sizeof(int) + sizeof(int));
            PacketHandle?.Invoke(new Packet(ReceiveBuffer, HeaderDetails.HeaderSize, NumBytesReceived - HeaderDetails.HeaderSize), CurrentEpoch - ReceivedEpoch);
        }
    }
}
