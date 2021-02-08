using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using DeadZoneEngine;
using ClientHandle;
using DZNetwork;
using DeadZoneEngine.Entities;
using System.Net;
using DeadZoneEngine.Controllers;

/// <summary>
/// Manages the server connection and game functionality
/// </summary>
public class Game
{
    public class ServerSnapshot
    {
        public struct Object
        {
            public DZSettings.EntityType Type;
            public bool FlaggedToDelete;
            public object Data;
        }

        public ulong ServerTick;
        public Dictionary<ushort, Object> Data = new Dictionary<ushort, Object>();
    }

    public static ulong ServerTicks = 0;
    public static int ServerTickRate = 60;

    /// <summary>
    /// Called once a frame
    /// </summary>
    public static void FixedUpdate()
    {
        Loader.Socket.FixedUpdate();

        UpdateClients();
        SendSnapshot();

        ServerTicks++;
    }

    private static void UpdateClients()
    {
        List<Client> Clients = ClientID.ConnectedClients.Values.ToList();
        foreach (Client C in Clients)
        {
            if (C != null)
                if (C.LostConnection)
                {
                    if (C.TicksSinceConnectionLoss > Client.TicksToTimeout)
                        C.Destroy();
                    C.TicksSinceConnectionLoss++;
                }
                else
                    C.TicksSinceConnectionLoss = 0;
        }
    }

    private static void SendSnapshot() //Sends world snapshot to given client
    {
        Packet SnapshotPacket = new Packet();
        SnapshotPacket.Write(ServerTickRate);
        SnapshotPacket.Write(ServerTicks);

        SnapshotPacket.Write(DZEngine.ServerSendableObjects.Count);
        for (int i = 0; i < DZEngine.ServerSendableObjects.Count; i++)
        {
            SnapshotPacket.Write(DZEngine.GetBytes(DZEngine.ServerSendableObjects[i]));
        }
        SnapshotPacket.InsertCheckSum(sizeof(int) + sizeof(ulong));
        Loader.Socket.Send(SnapshotPacket, ServerCode.ServerSnapshot);
    }

    public static Client SyncPlayers(DZUDPSocket.RecievePacketWrapper Packet)
    {
        Client C = Client.GetClient(Packet.Client);
        byte NumPlayers = Packet.Data.ReadByte();
        if (C.NumPlayers != NumPlayers)
        {
            while (C.NumPlayers < NumPlayers)
                C.AddPlayer();
        }

        Packet SyncPacket = new Packet();
        SyncPacket.Write(NumPlayers);
        SyncPacket.Write(C.ID);
        for (int i = 0; i < C.NumPlayers; i++)
        {
            if (C.Players[i] == null)
                SyncPacket.Write(byte.MaxValue);
            else
                SyncPacket.Write(C.Players[i].GetBytes());
        }
        Loader.Socket.SendTo(SyncPacket, ServerCode.SyncPlayers, Packet.Client);
        return C;
    }
    
    public static void UnWrapSnapshot(DZUDPSocket.RecievePacketWrapper Packet)
    {
        Client C = SyncPlayers(Packet);
        InputMapping.ParseBytes(Packet.Data, C);
    }

    public static void AddConnection(IPEndPoint EndPoint)
    {
        Debug.Log("Client Connected: " + EndPoint.Address + ":" + EndPoint.Port);

        Client ConnectedClient = Client.GetClient(EndPoint);
        ConnectedClient.LostConnection = false;
    }

    public static void RemoveConnection(IPEndPoint EndPoint)
    {
        Debug.Log("Client Disconnected: " + EndPoint.Address + ":" + EndPoint.Port);

        Client DisconnectedClient = Client.GetClient(EndPoint);
        DisconnectedClient.LostConnection = true;
    }
}

