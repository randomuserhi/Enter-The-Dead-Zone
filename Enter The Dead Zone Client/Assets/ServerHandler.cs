using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

using DZNetwork;

public enum ServerCode //TODO somehow implement / catch disconnection => its not a packet so not sure how to do this
{
    Null,
    SyncPlayers,
    ClientSnapshot,
    ServerSnapshot
}

public class ServerHandler
{
    public static void Start()
    {
        ServerHandle.PacketHandle = (Packet) =>
        {
            ServerCode Job = (ServerCode)Packet.Data.ReadInt();
            PerformServerAction(Packet, Job);
        };

        ServerHandle.LostPacketHandle = (SentPacketWrapper) =>
        {
            HandleLostPacket(SentPacketWrapper.Code);
        };
    }

    private static void HandleLostPacket(ServerCode Job)
    {
        //If socket is not connected and packets are lost well.. theres a good reason why packets are lost
        if (!Loader.Socket.Connected)
            return;

        switch (Job)
        {
            case ServerCode.Null:
                break;

            default:
                Debug.LogWarning("Unknown ServerCode: " + Job);
                break;
        }
    }

    private static void PerformServerAction(DZUDPSocket.RecievePacketWrapper Packet, ServerCode Job)
    {
        switch (Job)
        {
            case ServerCode.SyncPlayers:
                Game.SyncClient(Packet);
                break;
            case ServerCode.ServerSnapshot:
                Game.UnWrapSnapshot(Packet);
                break;
            default:
                Debug.LogWarning("Unknown ServerCode: " + Job);
                break;
        }
    }
}
