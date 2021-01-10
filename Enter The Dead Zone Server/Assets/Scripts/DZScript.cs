using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;

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
