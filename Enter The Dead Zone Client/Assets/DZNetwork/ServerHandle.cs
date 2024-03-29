﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace DZNetwork
{
    public static class ServerHandle
    {
        public static Action<DZUDPSocket.RecievePacketWrapper> PacketHandle = null;
        public static Action<DZUDPSocket.SentPacketWrapper> LostPacketHandle = null;

        private static readonly List<DZUDPSocket.RecievePacketWrapper> PacketsToProcess = new List<DZUDPSocket.RecievePacketWrapper>();
        private static readonly Queue<DZUDPSocket.RecievePacketWrapper> PacketsProcessing = new Queue<DZUDPSocket.RecievePacketWrapper>();

        private static readonly List<DZUDPSocket.SentPacketWrapper> LostPacketsToProcess = new List<DZUDPSocket.SentPacketWrapper>();
        private static readonly Queue<DZUDPSocket.SentPacketWrapper> LostPacketsProcessing = new Queue<DZUDPSocket.SentPacketWrapper>();

        public static void ProcessPacket(DZUDPSocket.RecievePacketWrapper Packet)
        {
            lock (PacketsToProcess)
            {
                PacketsToProcess.Add(Packet);
            }
        }

        public static void HandleLostPacket(DZUDPSocket.SentPacketWrapper Packet)
        {
            lock (LostPacketsToProcess)
            {
                LostPacketsToProcess.Add(Packet);
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

            lock (LostPacketsToProcess)
            {
                if (LostPacketsToProcess.Count > 0)
                {
                    for (int i = 0; i < LostPacketsToProcess.Count; i++)
                        LostPacketsProcessing.Enqueue(LostPacketsToProcess[i]);
                    LostPacketsToProcess.Clear();
                }
            }

            while (PacketsProcessing.Count > 0)
            {
                PacketHandle?.Invoke(PacketsProcessing.Dequeue());
            }

            while (LostPacketsProcessing.Count > 0)
            {
                LostPacketHandle?.Invoke(LostPacketsProcessing.Dequeue());
            }
        }
    }
}
