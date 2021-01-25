using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DZNetwork;

public enum ServerCode //TODO somehow implement / catch disconnection => its not a packet so not sure how to do this
{
    Null,
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
            ServerCode Job = (ServerCode)Packet.ReadInt();
            PerformServerAction(Packet, Job);
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

    private void PerformServerAction(Packet Packet, ServerCode Job)
    {
        switch(Job)
        {
            case ServerCode.ClientSnapshot:
                //Debug.Log(Packet.ReadByte() + " number of players");
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
