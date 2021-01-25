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

        ServerTicks++;
        SendSnapshot();
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

    
    public static void AddConnection(EndPoint EndPoint)
    {
        IPEndPoint IP = (IPEndPoint)EndPoint;
        Debug.Log("Client Connected: " + IP.Address + ":" + IP.Port);

        new Client(EndPoint);
    }

    public static void RemoveConnection(EndPoint EndPoint)
    {
        IPEndPoint IP = (IPEndPoint)EndPoint;
        Debug.Log("Client Disconnected: " + IP.Address + ":" + IP.Port);

        Client.Remove(Client.EndPointToID[EndPoint]);
    }

    /// <summary>
    /// Remove a client from the game world
    /// </summary>
    /// <param name="ClientIndex"></param>
    public static void RemoveClient(int ClientIndex) //On Client disconnect
    {
        throw new NotImplementedException();
    }
}

