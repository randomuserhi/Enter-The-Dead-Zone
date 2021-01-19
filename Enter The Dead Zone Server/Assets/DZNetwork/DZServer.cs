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
        public Action<Packet, long> PacketHandle; //Delegate that is called on recieving a packet
        public Action<EndPoint> DisconnectHandle;
        public Action<EndPoint> ConnectHandle;

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

        public void Send(Packet Packet)
        {
            List<EndPoint> Connections = ConnectedDevices.Keys.ToList();
            if (Connections.Count == 0) return;

            byte[][] PacketData = PacketHandler.GeneratePackets(Packet);
            foreach (EndPoint EndPoint in Connections)
                if (EndPoint != null)
                    for (int i = 0; i < PacketData.Length; i++)
                        SendTo(PacketData[i], EndPoint);
        }

        public void SendTo(Packet Packet, EndPoint EndPoint)
        {
            SendTo(Packet, EndPoint);
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

        protected override void OnReceiveConstructedPacket(Packet Data, long Ping)
        {
            PacketHandle?.Invoke(Data, Ping);
        }
    }
}
