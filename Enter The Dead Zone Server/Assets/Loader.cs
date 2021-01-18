using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;
using System.Threading.Tasks;

using DZNetwork;

public class Loader
{
    public static DZServer Server = new DZServer();

    [RuntimeInitializeOnLoadMethod] //Runs on application start
    private static void Start()
    {
        Application.quitting += Dispose; //Setup dispose to call when game is closed
        Application.targetFrameRate = Game.ServerTickRate; //Limit server tick rate / frame rate
        Time.fixedDeltaTime = 1f / Game.ServerTickRate; //Fixed physics update rate
        QualitySettings.vSyncCount = 0; //Turn off vsync
        Physics2D.simulationMode = SimulationMode2D.Script; //My program controls when unity updates

        Server.ConnectHandle += Game.AddConnection;
        Server.DisconnectHandle += Game.RemoveConnection;
        Server.PacketHandle += ServerHandle.ProcessPacket;
        Server.Connect(26950); //Startup server
    }

    private static void Dispose()
    {
        Server.Dispose();
    }
}
