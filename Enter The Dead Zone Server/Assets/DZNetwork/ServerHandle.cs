using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace DZNetwork
{
    public static class ServerHandle
    {
        public static int Ping = -1;

        public static Action<Packet> PacketHandle = null;

        private static readonly List<Packet> PacketsToProcess = new List<Packet>();
        private static readonly Queue<Packet> PacketsProcessing = new Queue<Packet>();

        public static void ProcessPacket(Packet Packet, long Ping)
        {
            lock (PacketsToProcess)
            {
                ServerHandle.Ping = (int)Ping;
                PacketsToProcess.Add(Packet);
            }
        }

        public static void FixedUpdate()
        {
            lock (PacketsToProcess)
            {
                if (PacketsToProcess.Count > 0)
                {
                    for (int i = 0; i < PacketsToProcess.Count; i++)
                        PacketsProcessing.Enqueue(PacketsToProcess[i]);
                    PacketsToProcess.Clear();
                }
            }

            while (PacketsProcessing.Count > 0)
            {
                PacketHandle?.Invoke(PacketsProcessing.Dequeue());
            }
        }
    }
}
