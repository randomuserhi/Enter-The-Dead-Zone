using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DZNetwork;

public enum ServerCode //TODO somehow implement / catch disconnection => its not a packet so not sure how to do this
{
    EstablishUPDConnection,
    UDPConnectionEstablished,
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

    public static void Initialise()
    {
        
    }

    private void PerformServerAction(Packet Packet, ServerCode Job)
    { 
        switch (Job)
        {
            case ServerCode.EstablishUPDConnection:
                Debug.Log("ServerHandler.EstablishUPDConnection");
                break;
            case ServerCode.UDPConnectionEstablished:
                Debug.Log("ServerHandler.UDPConnectionEstablished");
                break;
            case ServerCode.SnapshotData:
                Game.UnWrapSnapshot(Packet);
                break;

            default:
                Debug.LogError("Unknown ServerCode: " + Job);
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
