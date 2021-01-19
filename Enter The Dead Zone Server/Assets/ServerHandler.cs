using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DZNetwork;

public enum ServerCode //TODO somehow implement / catch disconnection => its not a packet so not sure how to do this
{
    ClientPing,
    SnapshotData
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
    }

    private void PerformServerAction(Packet Packet, ServerCode Job)
    {
        switch(Job)
        {
            case ServerCode.ClientPing:
                Debug.Log(Packet.ReadByte() + " number of players");
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
