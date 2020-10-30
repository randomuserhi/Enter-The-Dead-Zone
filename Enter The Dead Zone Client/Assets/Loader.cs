using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

using Network;
using Network.IPC;
using Network.TCPUDP;

public class Loader
{
    public static IPCNamedClient IPCClient;
    public static TCPUDPClient TCPUDPClient;
    public static int ClientIndex;

    public static string ServerIP = "192.168.2.26"; //"172.16.6.165";//"192.168.2.51"; //"192.168.2.26"; //"172.16.6.165";
    public static int ServerPort = 26950;

    [RuntimeInitializeOnLoadMethod]
    private static void Start()
    {
        Application.quitting += Dispose;
        Physics2D.simulationMode = SimulationMode2D.Script;

        TCPUDPClient = new TCPUDPClient(4096);
        ServerHandler.Initialise();

        //Remove later
        TCPConnectToServer(ServerIP, ServerPort);
    }

    public static void TCPConnectToServer(string ServerIP, int Port)
    {
        TCPUDPClient.TCPConnect(ServerIP, Port);
    }

    public static void UDPConnectToServer(int ClientIndex)
    {
        Loader.ClientIndex = ClientIndex;
        TCPUDPClient.UDPConnect(((IPEndPoint)(TCPUDPClient.TCPSocket.Client.LocalEndPoint)).Port, ServerIP, ServerPort);

        Packet EstablishPacket = new Packet();
        EstablishPacket.Write((int)ServerCode.UDPConnectionEstablished);
        TCPUDPClient.UDPSendMessage(EstablishPacket, ClientIndex);
    }

    private static void Dispose()
    {
        if (IPCClient != null)
            IPCClient.Dispose();
        if (TCPUDPClient != null)
            TCPUDPClient.Dispose();
    }
}
