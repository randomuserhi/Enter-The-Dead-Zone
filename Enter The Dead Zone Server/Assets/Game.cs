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

    public static void FixedUpdate()
    {
        Player[] Players = Clients.Values.ToArray();
        for (int i = 0; i < Players.Length; i++)
        {
            SendSnapshot(ref Players[i]);
        }
    }

    private static void SendSnapshot(ref Player Client) //Sends world snapshot to given client
    {
        //const int SnapshotSize = 10; //Square dictating how big the snapshot of the world is

        Packet SnapshotPacket = new Packet();
        SnapshotPacket.Write((int)ServerCode.SnapshotData);

        Debug.Log(ServerItems.Count); //TODO IMPLEMENT THIS SYSTEM

        //for time being just send everything
        SnapshotPacket.Write(DZEngine.UpdatableObjects.Count);
        for (int i = 0; i < DZEngine.UpdatableObjects.Count; i++)
        {
            AbstractWorldEntity Entity = (AbstractWorldEntity)DZEngine.UpdatableObjects[i];
            SnapshotPacket.Write(Entity.GetBytes());
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

