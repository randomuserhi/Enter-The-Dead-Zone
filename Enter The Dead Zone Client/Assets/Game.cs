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
    public static DZEngine.ManagedList<IServerSendable> ServerItems = new DZEngine.ManagedList<IServerSendable>();
    public static int ServerTickRate = 30;
    public static int ClientTickRate = 60;

    private static float ConversionRate = 0.5f;

    private static ulong TrueClientTicks = 0;
    public static ulong ClientTicks = 0;

    private struct Snapshot
    {
        public ulong Ticks;
    }

    private static Snapshot PrevSnapshot;

    public static void FixedUpdate()
    {
        TrueClientTicks++;
        ClientTicks = (ulong)(TrueClientTicks * ConversionRate);
    }

    public static void UnWrapSnapshot(PacketWrapper Packet)
    {
        Packet P = Packet.Data;
        ServerTickRate = P.ReadInt();
        ulong ServerTick = P.ReadULong();

        if (PrevSnapshot.Ticks >= ServerTick)
        {
            return;
        }

        ConversionRate = (float)ServerTickRate / ClientTickRate;

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
                Debug.LogWarning("Unable to Parse item from server snapshot");
                return;
            }

            ServerItem.ParseBytes(P, ServerTick);
            ServerItem.RecentlyUpdated = true;
        }
        foreach (IServerSendable Item in ServerItems)
        {
            if (!Item.RecentlyUpdated) //Item was not contained in recent snapshot so remove it
            {
                DZEngine.Destroy(Item);
            }
        }

        PrevSnapshot.Ticks = ServerTick; 
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

