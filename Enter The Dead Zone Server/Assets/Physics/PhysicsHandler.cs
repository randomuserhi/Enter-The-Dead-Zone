using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using InternalEngine;

public class PhysicsHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        IntEngine.PerformTimeStep();
    }
}
