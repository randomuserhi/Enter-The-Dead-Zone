using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

using DZNetwork;

public class Loader
{
    public static DZClient Client = new DZClient();

    public static string ServerIP = "192.168.2.26"; //"172.16.6.165";//"192.168.2.51"; //"192.168.2.26"; //"172.16.6.165";
    public static int ServerPort = 26950;

    [RuntimeInitializeOnLoadMethod]
    private static void Start()
    {
        Application.quitting += Dispose;
        Time.fixedDeltaTime = 1f / Game.ClientTickRate;
        Physics2D.simulationMode = SimulationMode2D.Script;

        ServerHandler.Initialise();

        //Remove later
        Client.ConnectHandle += Game.Connected;
        Client.DisconnectHandle += Game.Disconnected;
        Client.PacketHandle += ServerHandle.ProcessPacket;
        Client.Connect(ServerIP, ServerPort);
    }

    private static void Dispose()
    {
        Client.Dispose();
    }
}
