using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Settings for DZEngine
/// </summary>
public static class DZSettings
{
    public enum EntityType //Different entity types
    {
        Null,
        PlayerCreature
    }

    public static int NumPhysicsIterations = 10; //Number of physics iterations the impulse engine uses
    public static bool ActiveRenderers = true; //Set to true to render objects
}
