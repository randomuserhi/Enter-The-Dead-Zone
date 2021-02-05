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
        switch (Job)
        {
            case ServerCode.Null:
                break;

            default:
                Debug.LogWarning("Unknown ServerCode: " + Job);
                break;
        }
    }

    private void PerformServerAction(IPEndPoint Client, Packet Data, ServerCode Job)
    {
        switch(Job)
        {
            case ServerCode.SyncPlayers:
                Game.SyncPlayers(Client, Data);
                break;
            case ServerCode.ClientSnapshot:
                Game.UnWrapSnapshot(Client, Data);
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
