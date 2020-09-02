﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Network;

public enum ServerCode
{
    EstablishUPDConnection,
    UDPConnectionEstablished
}

public class ServerHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ServerHandle.PacketHandle = (Packet) =>
        {

            int SizeOfPacket = Packet.Packet.ReadInt();
            long Epoch = Packet.Packet.ReadLong();
            int ClientIndex = Packet.Packet.ReadInt();
            ServerCode Job = (ServerCode)Packet.Packet.ReadInt();

            PerformServerAction(SizeOfPacket, ClientIndex, Packet, Job);

            ServerHandle.Ping = (int)(Packet.Epoch - Epoch);

        };
    }

    public static void Initialise()
    {
        Network.TCPUDP.TCPUDPServer Server = Loader.TCPUDPServer;

        Server.OnTCPClientConnection += (ClientIndex) =>
        {
            Packet EstablishPacket = new Packet();
            EstablishPacket.Write((int)ServerCode.EstablishUPDConnection);
            Server.TCPSendMessage(EstablishPacket, ClientIndex);
        };
    }

    private void PerformServerAction(int SizeOfPacket, int ClientIndex, PacketWrapper Packet, ServerCode Job)
    {
        switch(Job)
        {
            case ServerCode.EstablishUPDConnection:
                break;
            case ServerCode.UDPConnectionEstablished:
                Debug.Log("Client " + ClientIndex + " succesfully connected.");
                break;

            default:
                Debug.LogError("Unknown ServerCode: " + Job + ", from Client " + ClientIndex);
                break;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        ServerHandle.FixedUpdate();
    }
}