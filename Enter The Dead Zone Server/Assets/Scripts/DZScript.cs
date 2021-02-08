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
    }

    public void FixedUpdate()
    {
        ServerHandle.FixedUpdate();
        DZEngine.FixedUpdate();
        Game.FixedUpdate();
        Main.FixedUpdate();
    }

    private void OnApplicationQuit()
    {
        DZEngine.ReleaseResources();
    }
}
