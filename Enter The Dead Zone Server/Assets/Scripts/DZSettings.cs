using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class DZSettings
{
    public enum EntityType
    {
        Null,
        PlayerCreature
    }

    public static int NumPhysicsIterations = 10;
    public static bool ActiveRenderers = true;
}
