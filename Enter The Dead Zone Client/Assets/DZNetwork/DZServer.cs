﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace DZNetwork
{
    public class DZServer : DZUDPSocket
    {
        public Action<Packet, long> PacketHandle; //Delegate that is called on recieving a packet

        public HashSet<EndPoint> ConnectedDevices;

        public DZServer() : base(4096)
        {
            ConnectedDevices = new HashSet<EndPoint>();
        }

        public void Connect(int Port)
        {
            Socket.Bind(new IPEndPoint(IPAddress.Any, Port));
            BeginReceive();
        }

        public void Send(Packet Packet)
        {
            foreach (EndPoint EndPoint in ConnectedDevices)
                if (EndPoint != null)
                    SendTo(Packet.GetBytes(), EndPoint);
        }

        public void SendTo(Packet Packet, EndPoint EndPoint)
        {
            SendTo(Packet, EndPoint);
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
