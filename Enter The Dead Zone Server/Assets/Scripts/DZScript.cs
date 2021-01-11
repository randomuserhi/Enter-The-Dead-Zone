using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;

//Unity Script to update DZEngine
public class DZScript : MonoBehaviour
{
    public void Start()
    {
        DZEngine.Initialize();
        Main.Start();
    }

    public void FixedUpdate()
    {
        DZEngine.FixedUpdate();
        Main.FixedUpdate();
    }

    private void OnApplicationQuit()
    {
        DZEngine.ReleaseResources();
    }
}
