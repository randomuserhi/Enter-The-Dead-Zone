﻿using System;
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

/// <summary>
/// Manages the server connection and game functionality
/// </summary>
public class Game
{
    public static ulong ServerTicks = 0;
    public static int ServerTickRate = 30;

    /// <summary>
    /// Called once a frame
    /// </summary>
    public static void FixedUpdate()
    {
        Loader.Socket.FixedUpdate();

        UpdateClients();

        ServerTicks++;
        SendSnapshot();
    }

    private static void UpdateClients()
    {
        List<Client> Clients = ClientID.ConnectedClients.Values.ToList();
        foreach (Client C in Clients)
        {
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

        Loader.Socket.Send(SnapshotPacket, ServerCode.ServerSnapshot);
    }

    public static void SyncClient(EndPoint EndPoint, Packet Data)
    {
        Client C = Client.GetClient(EndPoint);
        byte NumPlayers = Data.ReadByte();
        if (!C.Setup)
        {
            for (int i = 0; i < NumPlayers; i++)
                C.AddPlayer();

            C.Setup = true;
        }
        else
            while (C.NumPlayers < NumPlayers)
                C.AddPlayer();

        Packet SyncPacket = new Packet();
        SyncPacket.Write(NumPlayers);
        SyncPacket.Write(C.ID);
        for (int i = 0; i < C.NumPlayers; i++)
        {
            if (C.Players[i] == null)
                SyncPacket.Write((byte)255);
            else
                SyncPacket.Write(C.Players[i].GetBytes());
        }
        Loader.Socket.SendTo(SyncPacket, ServerCode.InitializeConnection, EndPoint);
    }
    
    public static void AddConnection(EndPoint EndPoint)
    {
        IPEndPoint IP = (IPEndPoint)EndPoint;
        Debug.Log("Client Connected: " + IP.Address + ":" + IP.Port);

        Client ConnectedClient = Client.GetClient(EndPoint);
        ConnectedClient.LostConnection = false;
    }

    public static void RemoveConnection(EndPoint EndPoint)
    {
        IPEndPoint IP = (IPEndPoint)EndPoint;
        Debug.Log("Client Disconnected: " + IP.Address + ":" + IP.Port);

        Client DisconnectedClient = Client.GetClient(EndPoint);
        DisconnectedClient.LostConnection = true;
    }
}

