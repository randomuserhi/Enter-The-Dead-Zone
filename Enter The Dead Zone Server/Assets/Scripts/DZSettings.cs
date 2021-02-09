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
        PlayerCreature,
        Tilemap,
        TriggerPlate,
        BulletEntity,
        EnemyCreature,
        Turret,
        CoinEntity,
        CrystalEntity
    }

    public static int NumPhysicsIterations = 10;
    public static bool ActiveRenderers = false;
    public static bool ActiveControllers = false;
    public static bool ClientSidePrediction = false;
    public static bool Client = false;
}
