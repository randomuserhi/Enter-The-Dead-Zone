using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;
using System.Threading.Tasks;

using DZNetwork;

public class Loader
{
    public static DZServer Socket = new DZServer();

    [RuntimeInitializeOnLoadMethod] //Runs on application start
    private static void Start()
    {
        Physics2D.queriesStartInColliders = false;
        Application.quitting += Dispose; //Setup dispose to call when game is closed
        Application.targetFrameRate = Game.ServerTickRate; //Limit server tick rate / frame rate
        Time.fixedDeltaTime = 1f / Game.ServerTickRate; //Fixed physics update rate
        QualitySettings.vSyncCount = 0; //Turn off vsync
        Physics2D.simulationMode = SimulationMode2D.Script; //My program controls when unity updates

        Socket.ConnectHandle += Game.AddConnection;
        Socket.DisconnectHandle += Game.RemoveConnection;
        Socket.PacketHandle += ServerHandle.ProcessPacket;
        Socket.Connect(26950); //Startup server
    }

    private static void Dispose()
    {
        Socket.Dispose();
    }
}
