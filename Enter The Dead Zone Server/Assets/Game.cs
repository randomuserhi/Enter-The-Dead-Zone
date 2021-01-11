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

/// <summary>
/// Manages the server connection and game functionality
/// </summary>
public class Game
{
    public readonly static Dictionary<int, Player> Clients = new Dictionary<int, Player>();
    public static ulong ServerTicks = 0;
    public static int ServerTickRate = 30;

    /// <summary>
    /// Called once a frame
    /// </summary>
    public static void FixedUpdate()
    {
        ServerTicks++;
        foreach (Player P in Clients.Values) //Send a snapshot of the world to each client
        {
            SendSnapshot(P);
        }
    }

    private static void SendSnapshot(Player Client) //Sends world snapshot to given client
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

        Debug.Log("SnapShotSent");
        Loader.TCPUDPServer.UDPSendMessage(SnapshotPacket, Client.ClientIndex);
    }

    /// <summary>
    /// Adds a given client to the game world
    /// </summary>
    /// <param name="ClientIndex"></param>
    public static void AddClient(int ClientIndex)
    {
        Player P = new Player();
        P.ClientIndex = ClientIndex;
        Clients.Add(ClientIndex, P);
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

