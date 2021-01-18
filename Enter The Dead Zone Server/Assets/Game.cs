using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using DeadZoneEngine;
using Templates;
using DZNetwork;
using DeadZoneEngine.Entities;

/// <summary>
/// Manages the server connection and game functionality
/// </summary>
public class Game
{
    public static Dictionary<System.Net.EndPoint, Client> ConnectedClients = new Dictionary<System.Net.EndPoint, Client>();

    public static ulong ServerTicks = 0;
    public static int ServerTickRate = 30;

    /// <summary>
    /// Called once a frame
    /// </summary>
    public static void FixedUpdate()
    {
        Loader.Server.FixedUpdate();

        ServerTicks++;
        SendSnapshot();
    }

    private static void SendSnapshot() //Sends world snapshot to given client
    {
        Packet SnapshotPacket = new Packet();
        SnapshotPacket.Write((int)ServerCode.SnapshotData);
        SnapshotPacket.Write(ServerTickRate);
        SnapshotPacket.Write(ServerTicks);

        SnapshotPacket.Write(DZEngine.ServerSendableObjects.Count);
        for (int i = 0; i < DZEngine.ServerSendableObjects.Count; i++)
        {
            SnapshotPacket.Write(DZEngine.GetBytes(DZEngine.ServerSendableObjects[i]));
        }

        Loader.Server.Send(SnapshotPacket);
    }

    
    public static void AddConnection(System.Net.EndPoint EndPoint)
    {
        System.Net.IPEndPoint IP = (System.Net.IPEndPoint)EndPoint;
        Debug.Log("Client Connected: " + IP.Address + ":" + IP.Port);

        if (!ConnectedClients.ContainsKey(EndPoint))
            ConnectedClients.Add(EndPoint, new Client());
    }

    public static void RemoveConnection(System.Net.EndPoint EndPoint)
    {
        System.Net.IPEndPoint IP = (System.Net.IPEndPoint)EndPoint;
        Debug.Log("Client Disconnected: " + IP.Address + ":" + IP.Port);
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

