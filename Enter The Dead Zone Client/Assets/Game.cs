using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;
using Network;

public class Game
{
    public static int ServerTickRate = 30;
    public static ulong ClientTicks = 0;

    public static void FixedUpdate()
    {
    }

    public static void UnWrapSnapshot(PacketWrapper Packet) //TODO:: place specific entity unwraps into their respective classes / abstract methods
    {
        Packet P = Packet.Data;
        ServerTickRate = P.ReadInt();
        ulong ServerTick = P.ReadULong();

        int NumSnapshotItems = P.ReadInt();
        for (int i = 0; i < NumSnapshotItems; i++)
        {
            ulong ID = P.ReadULong();
            _IInstantiatableDeletable Item = EntityID.GetObject(ID);
            bool FlaggedToDelete = P.ReadBool();
            if (FlaggedToDelete)
            {
                if (Item != null)
                    DZEngine.Destroy(Item);
                continue;
            }
            IServerSendable ServerItem = Item as IServerSendable;
            DZSettings.EntityType Type = (DZSettings.EntityType)P.ReadInt();
            if (ServerItem == null)
                ServerItem = Parse(P, ID, Type);
            if (ServerItem == null)
            {
                Debug.LogError("Unable to Parse item from server snapshot");
                return;
            }

            ServerItem.ParseBytes(P, ServerTick);
        }
    }

    private static IServerSendable Parse(Packet P, ulong ID, DZSettings.EntityType Type)
    {
        switch (Type)
        {
            case DZSettings.EntityType.PlayerCreature: return new PlayerCreature(ID);
            case DZSettings.EntityType.Null: return null;
            default: Debug.LogWarning("Parsing unknown entity type");  return null;
        }
    }
}

