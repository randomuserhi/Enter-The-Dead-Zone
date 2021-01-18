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

        public DZClient() : base(4096)
        {
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

            long CurrentEpoch = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;
            long ReceivedEpoch = BitConverter.ToInt64(ReceiveBuffer, sizeof(int));
            PacketHandle?.Invoke(new Packet(ReceiveBuffer, HeaderDetails.HeaderSize, NumBytesReceived - HeaderDetails.HeaderSize), CurrentEpoch - ReceivedEpoch);
        }
    }
}
