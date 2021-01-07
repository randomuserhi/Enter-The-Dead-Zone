using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeadZoneEngine;

public class DZScript : MonoBehaviour
{
    PlayerCreature P;

    // Start is called before the first frame update
    void Start()
    {
        DZEngine.Initialize();
        P = new PlayerCreature();

        Tilemap T1 = new Tilemap(32, new Vector2Int(4, 2));
        Tilemap T2 = new Tilemap(32, new Vector2Int(4, 2));
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
