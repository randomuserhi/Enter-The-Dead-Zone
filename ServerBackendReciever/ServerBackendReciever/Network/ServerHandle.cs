using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        //public static Action<Packet> PacketHandle = null;

        public static Action<PacketWrapper> PacketHandle = (Packet) =>
        {

            Console.WriteLine("1: " + Packet.Packet.ReadInt());
            long Epoch = Packet.Packet.ReadLong();
            Console.WriteLine("2: " + Epoch);
            Console.WriteLine("3: " + Packet.Packet.ReadInt());
            Console.WriteLine("4: " + Packet.Packet.ReadString());
            Console.WriteLine("Ping = " + (Packet.Epoch - Epoch));

        };

        private static readonly List<PacketWrapper> PacketsToProcess = new List<PacketWrapper>();
        private static readonly Queue<PacketWrapper> PacketsProcessing = new Queue<PacketWrapper>();

        public static void ProcessPacket(Packet Packet, long Epoch)
        {
            lock (PacketsToProcess)
            {
                PacketsToProcess.Add(new PacketWrapper(Packet, Epoch));
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
