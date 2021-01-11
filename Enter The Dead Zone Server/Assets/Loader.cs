using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;
using System.Threading.Tasks;

using Network;
using Network.IPC;
using Network.TCPUDP;

public class Loader
{
    public static IPCNamedServer IPCServer;
    public static TCPUDPServer TCPUDPServer;

    [RuntimeInitializeOnLoadMethod] //Runs on application start
    private static void Start()
    {
        Application.quitting += Dispose; //Setup dispose to call when game is closed
        Application.targetFrameRate = Game.ServerTickRate; //Limit server tick rate / frame rate
        QualitySettings.vSyncCount = 0; //Turn off vsync
        Physics2D.simulationMode = SimulationMode2D.Script; //My program controls when unity updates

        //change such that max number of players etc is changable
        TCPUDPServer = new TCPUDPServer(26950, 4, 4096);
        ServerHandler.Initialise();

        TCPUDPServer.Connect(); //Startup server
    }

    private static void Dispose()
    {
        if (IPCServer != null)
            IPCServer.Dispose();
        if (TCPUDPServer != null)
            TCPUDPServer.Dispose();
    }
}
