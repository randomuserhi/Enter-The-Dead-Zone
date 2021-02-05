using System;
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
        public Action<RecievePacketWrapper, long> PacketHandle; //Delegate that is called on recieving a packet
        public Action<IPEndPoint> DisconnectHandle;
        public Action<IPEndPoint> ConnectHandle;
        public Action<SentPacketWrapper> PacketLostHandle;

        public const int ConnectionLifeTime = 150;
        private object DeviceUpdate = new object();
        public Dictionary<IPEndPoint, uint> ConnectedDevices;

        public DZServer() : base(4096)
        {
            ConnectedDevices = new Dictionary<IPEndPoint, uint>(new IPEndPointComparer());
        }

        private List<IPEndPoint> Disconnects = new List<IPEndPoint>();
        public void FixedUpdate()
        {
            Tick();

            Disconnects.Clear();
            List<IPEndPoint> Connections = ConnectedDevices.Keys.ToList();
            foreach (IPEndPoint EndPoint in Connections)
            {
                ConnectedDevices[EndPoint]++;
                if (ConnectedDevices[EndPoint] > ConnectionLifeTime)
                {
                    Disconnects.Add(EndPoint);
                }
            }
            for (int i = 0; i < Disconnects.Count; i++)
            {
                PacketHandler.RemoveAcknowledgement(Disconnects[i]);
                ConnectedDevices.Remove(Disconnects[i]);
                DisconnectHandle(Disconnects[i]);
            }
        }

        public void Connect(int Port)
        {
            Socket.Bind(new IPEndPoint(IPAddress.Any, Port));
            UnityEngine.Debug.Log("Server opened on port: " + Port);
            BeginReceive();
        }

        public void Send(Packet Packet, ServerCode ServerCode)
        {
            List<IPEndPoint> Connections = ConnectedDevices.Keys.ToList();
            if (Connections.Count == 0) return;

            Packet.InsertServerCode(ServerCode);
            foreach (IPEndPoint EndPoint in Connections)
                if (EndPoint != null)
                {
                    PacketHandler.PacketGroup PacketGroup = PacketHandler.GeneratePackets(Packet, EndPoint);
                    for (int i = 0; i < PacketGroup.Packets.Length; i++)
                        SendTo((ushort)(PacketGroup.StartingPacketSequence + i), ServerCode, PacketGroup.Packets[i], EndPoint);
                }
        }

        public void SendTo(Packet Packet, ServerCode ServerCode, IPEndPoint EndPoint)
        {
            Packet.InsertServerCode(ServerCode);
            PacketHandler.PacketGroup PacketGroup = PacketHandler.GeneratePackets(Packet, EndPoint);
            for (int i = 0; i < PacketGroup.Packets.Length; i++)
                SendTo((ushort)(PacketGroup.StartingPacketSequence + i), ServerCode, PacketGroup.Packets[i], EndPoint);
        }

        protected override void OnReceive(IPEndPoint ReceivedEndPoint)
        {
            lock (DeviceUpdate)
            {
                if (!ConnectedDevices.ContainsKey(ReceivedEndPoint))
                {
                    ConnectHandle(ReceivedEndPoint);
                    ConnectedDevices.Add(ReceivedEndPoint, 0);
                }
                else
                    ConnectedDevices[ReceivedEndPoint] = 0;
            }
        }

        protected override void OnReceiveConstructedPacket(RecievePacketWrapper Packet, long Ping)
        {
            PacketHandle?.Invoke(Packet, Ping);
        }

        protected override void OnPacketLost(SentPacketWrapper Packet)
        {
            PacketLostHandle?.Invoke(Packet);
        }
    }
}
