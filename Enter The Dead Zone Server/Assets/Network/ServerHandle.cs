using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace Network
{
    public struct PacketWrapper
    {
        public Packet Packet;
        public long Epoch;

        public PacketWrapper(Packet Packet, long Epoch)
        {
            this.Packet = Packet;
            this.Epoch = Epoch;
        }
    }

    public class ServerHandle
    {
        public static int Ping = -1;

        public static Action<PacketWrapper> PacketHandle = null;

        private static readonly List<PacketWrapper> PacketsToProcess = new List<PacketWrapper>();
        private static readonly Queue<PacketWrapper> PacketsProcessing = new Queue<PacketWrapper>();

        /// <summary>
        /// Pushes a given packet onto a list to process them
        /// </summary>
        /// <param name="Epoch">Time of packet being received in Epoch time</param>
        public static void ProcessPacket(Packet Packet, long Epoch)
        {
            lock (PacketsToProcess)
            {
                PacketsToProcess.Add(new PacketWrapper(Packet, Epoch));
            }
        }

        /// <summary>
        /// Called once per frame
        /// </summary>
        public static void FixedUpdate()
        {
            //Push all recieved packets onto a queue to process
            lock (PacketsToProcess)
            {
                if (PacketsToProcess.Count > 0)
                {
                    for (int i = 0; i < PacketsToProcess.Count; i++)
                        PacketsProcessing.Enqueue(PacketsToProcess[i]);
                    PacketsToProcess.Clear();
                }
            }

            //Process all packets on the queue
            while (PacketsProcessing.Count > 0)
            {
                PacketHandle?.Invoke(PacketsProcessing.Dequeue());
            }
        }
    }
}
