using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using InternalEngine;

public class PhysicsHandler : MonoBehaviour
{
    public bool DebugMode = false;

    public GameObject DebugPoint;

    public List<GameObject> DebugPoints;

    // Start is called before the first frame update
    void Start()
    {
        DebugPoints = new List<GameObject>();

        IntEngine.Initialise();
        for (int i = 0; i < IntEngine.Entities.Count; i++)
        {
            DebugPoints.Add(Instantiate(DebugPoint));
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        for (int i = 0; i < DebugPoints.Count; i++)
        {
            if (DebugMode)
                IntEngine.Entities[i].Position = DebugPoints[i].transform.position;
            else
                DebugPoints[i].transform.position = IntEngine.Entities[i].Position;
        }

        IntEngine.PerformTimeStep();
    }
}
