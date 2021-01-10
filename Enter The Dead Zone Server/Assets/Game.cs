using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using DeadZoneEngine;
using Templates;
using Network;
using DeadZoneEngine.Entities;

public class Game
{
    public readonly static Dictionary<int, Player> Clients = new Dictionary<int, Player>();
    public static DZEngine.ManagedList<IServerSendable> ServerItems = new DZEngine.ManagedList<IServerSendable>();
    public static ulong ServerTicks = 0;
    public static int ServerTickRate = 30;

    public static void FixedUpdate()
    {
        ServerTicks++;
        Player[] Players = Clients.Values.ToArray();
        for (int i = 0; i < Players.Length; i++)
        {
            SendSnapshot(ref Players[i]);
        }
    }

    private static void SendSnapshot(ref Player Client) //Sends world snapshot to given client
    {
        Packet SnapshotPacket = new Packet();
        SnapshotPacket.Write((int)ServerCode.SnapshotData);
        SnapshotPacket.Write(ServerTickRate);
        SnapshotPacket.Write(ServerTicks);

        SnapshotPacket.Write(ServerItems.Count);
        foreach (IServerSendable Item in ServerItems)
        {
            SnapshotPacket.Write(DZEngine.GetBytes(Item));
        }

        Debug.Log("SnapShotSent");
        Loader.TCPUDPServer.UDPSendMessage(SnapshotPacket, Client.ClientIndex);
    }

    public static void AddClient(int ClientIndex)
    {
        Player P = new Player();
        P.ClientIndex = ClientIndex;
        Clients.Add(ClientIndex, P);
    }

    public static void RemoveClient(int ClientIndex) //On Client disconnect
    {
        throw new NotImplementedException();
    }
}

