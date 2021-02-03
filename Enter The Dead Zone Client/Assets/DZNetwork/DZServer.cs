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
        public Action<EndPoint> DisconnectHandle;
        public Action<EndPoint> ConnectHandle;
        public Action<SentPacketWrapper> PacketLostHandle;

        public const int ConnectionLifeTime = 150;
        private object DeviceUpdate = new object();
        public Dictionary<EndPoint, uint> ConnectedDevices;

        public DZServer() : base(4096)
        {
            ConnectedDevices = new Dictionary<EndPoint, uint>();
        }

        private List<EndPoint> Disconnects = new List<EndPoint>();
        public void FixedUpdate()
        {
            Tick();

            Disconnects.Clear();
            List<EndPoint> Connections = ConnectedDevices.Keys.ToList();
            foreach (EndPoint EndPoint in Connections)
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
            List<EndPoint> Connections = ConnectedDevices.Keys.ToList();
            if (Connections.Count == 0) return;

            foreach (EndPoint EndPoint in Connections)
                if (EndPoint != null)
                {
                    PacketHandler.PacketGroup PacketGroup = PacketHandler.GeneratePackets(Packet, ServerCode, EndPoint);
                    for (int i = 0; i < PacketGroup.Packets.Length; i++)
                        SendTo((ushort)(PacketGroup.StartingPacketSequence + i), ServerCode, PacketGroup.Packets[i], EndPoint);
                }
        }

        public void SendTo(Packet Packet, ServerCode ServerCode, EndPoint EndPoint)
        {
            PacketHandler.PacketGroup PacketGroup = PacketHandler.GeneratePackets(Packet, ServerCode, EndPoint);
            for (int i = 0; i < PacketGroup.Packets.Length; i++)
                SendTo((ushort)(PacketGroup.StartingPacketSequence + i), ServerCode, PacketGroup.Packets[i], EndPoint);
        }

        protected override void OnReceive(EndPoint ReceivedEndPoint)
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
