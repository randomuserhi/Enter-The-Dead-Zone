using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using DeadZoneEngine.Entities;

public class AbstractWorld : MonoBehaviour
{
    [SerializeField]
    public DZSettings.EntityType Type;

    public object Self;

    public object Context;
}
