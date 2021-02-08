using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DZNetwork;
using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Controllers;

//Unity Script to update DZEngine
public class DZScript : MonoBehaviour
{
    public void Start()
    {
        ServerHandler.Start();
        DZEngine.Initialize();
        Main.Start();

        if (DZSettings.ActiveControllers) //TODO FIX SUCH THAT THIS CAN BE PLACED IN DZENGINE.INITIALIZE
            InputMapping.Initialize();
    }

    public void FixedUpdate()
    {
        ServerHandle.FixedUpdate();
        InputMapping.Tick();
        Game.FixedUpdate();
        Main.FixedUpdate();
    }

    private void OnApplicationQuit()
    {
        DZEngine.ReleaseResources();
    }
}
