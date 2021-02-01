using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

using DZNetwork;

public enum ServerCode //TODO somehow implement / catch disconnection => its not a packet so not sure how to do this
{
    Null,
    InitializeConnection,
    ClientSnapshot,
    ServerSnapshot
}

public class ServerHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ServerHandle.PacketHandle = (Packet) =>
        {
            ServerCode Job = (ServerCode)Packet.Data.ReadInt();
            PerformServerAction(Packet.Client, Packet.Data, Job);
        };

        ServerHandle.LostPacketHandle = (SentPacketWrapper) =>
        {
            HandleLostPacket(SentPacketWrapper.Code);
        };
    }

    private void HandleLostPacket(ServerCode Job)
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

    private void PerformServerAction(EndPoint Client, Packet Data, ServerCode Job)
    {
        switch (Job)
        {
            case ServerCode.InitializeConnection:
                Game.SyncClient(Data);
                break;
            case ServerCode.ServerSnapshot:
                Game.UnWrapSnapshot(Data);
                break;
            default:
                Debug.LogWarning("Unknown ServerCode: " + Job);
                break;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        ServerHandle.FixedUpdate();
        Game.FixedUpdate();
    }
}
