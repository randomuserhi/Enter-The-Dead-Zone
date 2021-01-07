using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeadZoneEngine;

public class PhysicsHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DZEngine.Initialize();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        DZEngine.FixedUpdate();
    }

    void OnApplicationQuit()
    {
        DZEngine.ReleaseResources();
    }
}
