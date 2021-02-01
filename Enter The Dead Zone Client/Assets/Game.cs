using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using ClientHandle;
using UnityEngine;
using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;
using DZNetwork;

public class Game
{
    public static Client Client;

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

        //Framerate of client may not be the same as tick rate, this ensures that tickupdate is called at the correct timings or close to the correct timings
        ulong ClientTickUpdate = (ulong)(TrueClientTicks * ConversionRate);
        if (ClientTickUpdate != ClientTicks)
        {
            ClientTicks = ClientTickUpdate;
            TickUpdate();
        }
    }

    private static void TickUpdate()
    {
        Loader.Socket.FixedUpdate();

        if (Loader.Socket.SocketConnected)
            SendSnapshot();

        if (!Loader.Socket.Connected)
            return;
    }

    private static void SendSnapshot()
    {
        if (!Client.Setup)
        {
            Packet Setup = new Packet();
            Setup.Write(Client.NumPlayers);
            Loader.Socket.Send(Setup, ServerCode.InitializeConnection);
            return;
        }

        Packet SnapshotPacket = new Packet();
        SnapshotPacket.Write(Client.NumPlayers);
        Loader.Socket.Send(SnapshotPacket, ServerCode.ClientSnapshot);
    }

    public static void SyncClient(Packet Data)
    {
        if (Client.Setup == false)
        {
            int NumPlayers = Data.ReadByte();
            if (NumPlayers != Client.NumPlayers)
            {
                Debug.LogWarning("Sync failed due to inconsistent player count");
                return;
            }
            Client.ID.ChangeID(Data.ReadUShort());
            for (int i = 0; i < NumPlayers; i++)
            {
                int ID = Data.ReadByte();
                if (ID == 255)
                    continue;
                ushort PlayerEntityID = Data.ReadUShort();
                Debug.Log(PlayerEntityID);
                Client.Players[i].Entity.ID.ChangeID(PlayerEntityID);
            }
            Client.Setup = true;
        }
    }

    public static void UnWrapSnapshot(Packet Packet)
    {
        if (Client == null || !Client.Setup) return;

        ServerTickRate = Packet.ReadInt();
        ulong ServerTick = Packet.ReadULong();

        if (PrevSnapshot.Ticks >= ServerTick)
        {
            Debug.LogWarning("Received a late packet");
            return;
        }

        ConversionRate = (float)ServerTickRate / ClientTickRate;

        int NumSnapshotItems = Packet.ReadInt();
        for (int i = 0; i < NumSnapshotItems; i++)
        {
            ushort ID = Packet.ReadUShort();
            _IInstantiatableDeletable Item = EntityID.GetObject(ID);
            bool FlaggedToDelete = Packet.ReadBool();
            if (FlaggedToDelete)
            {
                if (Item != null)
                    DZEngine.Destroy(Item);
                continue;
            }
            IServerSendable ServerItem = Item as IServerSendable;
            DZSettings.EntityType Type = (DZSettings.EntityType)Packet.ReadInt();
            if (ServerItem == null)
                ServerItem = Parse(ID, Type);
            if (ServerItem == null)
            {
                Debug.LogWarning("Unable to Parse item from server snapshot");
                return;
            }

            if ((DZSettings.EntityType)ServerItem.ServerObjectType != Type)
            {
                Debug.LogWarning("Entity Types of ID " + ID + " do not match... resetting IDs and re-parsing");
                ServerItem.ID.ChangeID();

                Item = EntityID.GetObject(ID);
                ServerItem = Item as IServerSendable;
                if (ServerItem == null)
                    ServerItem = Parse(ID, Type);
                if (ServerItem == null)
                {
                    Debug.LogWarning("Unable to Parse item from server snapshot");
                    return;
                }
            }
            ServerItem.ParseBytes(Packet, ServerTick);
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

    private static IServerSendable Parse(ushort ID, DZSettings.EntityType Type)
    {
        switch (Type)
        {
            case DZSettings.EntityType.PlayerCreature: return new PlayerCreature(ID);
            case DZSettings.EntityType.Tilemap: return new Tilemap(ID);
            case DZSettings.EntityType.Null: return null;
            default: Debug.LogWarning("Parsing unknown entity type");  return null;
        }
    }

    public static void Connected()
    {
        Debug.Log("Client Connected");
    }

    public static void Disconnected()
    {
        Debug.Log("Client Disconnected");
    }
}

