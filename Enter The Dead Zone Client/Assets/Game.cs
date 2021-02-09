using ClientHandle;
using DeadZoneEngine;
using DeadZoneEngine.Controllers;
using DeadZoneEngine.Entities;
using DZNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Game
{
    public static Client Client;

    public static DZEngine.ManagedList<IServerSendable> ServerItems = new DZEngine.ManagedList<IServerSendable>();
    public static int ServerTickRate = 60;
    public static int ClientTickRate = 60;
    public static ulong ClientTicks = 0;
    public static ulong ClientTickAsServerTick = 0;
    public static bool Initialized = false;

    public class ServerSnapshot
    {
        public struct Object
        {
            public DZSettings.EntityType Type;
            public bool FlaggedToDelete;
            public object Data;
        }

        public ulong ServerTick;
        public Dictionary<ushort, Object> Data = new Dictionary<ushort, Object>();
    }
    public static ulong InterpolationRatio = 2;
    public static ulong TargetInterpolationRatio = 2;
    public static ulong LowerInterpolationRatioMargin = 1;
    public static ulong UpperInterpolationRatioMargin = 5;
    public static ulong IterpolationCap = 30;
    private static JitterBuffer<ServerSnapshot> Histogram = new JitterBuffer<ServerSnapshot>();
    private static ServerSnapshot CurrentLoaded = null;

    public static void FixedUpdate()
    {
        Loader.Socket.FixedUpdate();

        float ServerToClientTick = (float)ClientTickRate / ServerTickRate;
        float ClientToServerTick = (float)ServerTickRate / ClientTickRate;
        ClientTickAsServerTick = (ulong)(ClientTicks * ClientToServerTick);

        if (Initialized == false && Histogram.Count > 1)
        {
            ulong NumServerTicksPassed = Histogram.Last.ServerTick - Histogram.First.ServerTick;
            if (NumServerTicksPassed >= InterpolationRatio)
            {
                ClientTicks = (ulong)((Histogram.Last.ServerTick - InterpolationRatio) * ServerToClientTick);
                Initialized = true;
            }
        }

        DZEngine.FixedUpdate();

        SendData();

        Interp = 0;
        ClientTicks++;
    }

    private static float Interp = 0;
    public static void Update()
    {
        float ServerToClientTick = (float)ClientTickRate / ServerTickRate;
        float ClientToServerTick = (float)ServerTickRate / ClientTickRate;

        if (Initialized == true)
        {
            ServerSnapshot From = null;
            ServerSnapshot To = null;
            Histogram.Iterate(S =>
            {
                if (S.Value.ServerTick >= ClientTickAsServerTick)
                {
                    From = S.Value;
                    if (S.Next != null)
                        To = S.Next.Value;
                }
            }, S => S.Value.ServerTick >= ClientTickAsServerTick);
            if (From != null)
            {
                Histogram.Dequeue(From);
                if (To != null)
                {
                    if (CurrentLoaded != From)
                    {
                        LoadSnapshot(From, false);
                        CurrentLoaded = From;

                        int LargestHistogram = 0;
                        for (int i = 0; i < Client.Players.Length; i++)
                        {
                            if (Client.Players[i] != null)
                            {
                                Client.Players[i].Entity.StartClientPrediction(From);
                                if (Client.Players[i].Entity.Histogram.Count > LargestHistogram)
                                    LargestHistogram = Client.Players[i].Entity.Histogram.Count;
                            }
                        }
                        for (int j = 0; j < LargestHistogram; j++)
                        {
                            for (int i = 0; i < Client.Players.Length; i++)
                            {
                                if (Client.Players[i] != null)
                                {
                                    Client.Players[i].Entity.ClientPrediction();
                                }
                            }
                            DZEngine.PhysicsUpdate();
                        }

                        for (int i = 0; i < Client.Players.Length; i++)
                        {
                            if (Client.Players[i] != null)
                            {
                                Client.Players[i].Entity.EndClientPrediction();
                            }
                        }
                    }
                    float FromTick = (From.ServerTick * ServerToClientTick);
                    float Origin = (ClientTicks - FromTick);
                    float Time = Origin / ((To.ServerTick * ServerToClientTick) - FromTick) + Interp;
                    Interpolate(From, To, Time);
                }
                else
                {
                    LoadSnapshot(From, true);
                    float Time = (ClientTicks - From.ServerTick * ServerToClientTick) / ClientTickRate;
                    Extrapolate(From, Time);
                }
            }
            else
            {
                Histogram.Dequeue(Histogram.Last);
            }

            if ((ulong)Histogram.Count - 1 < TargetInterpolationRatio - LowerInterpolationRatioMargin)
            {
                InterpolationRatio += 1;
                ClientTicks = (ulong)((Histogram.Last.ServerTick - InterpolationRatio) * ServerToClientTick);
            }
            else if ((ulong)Histogram.Count - 1 > TargetInterpolationRatio + UpperInterpolationRatioMargin)
            {
                if (InterpolationRatio != 0)
                    InterpolationRatio -= 1;
                ClientTicks = (ulong)((Histogram.Last.ServerTick - InterpolationRatio) * ServerToClientTick);
            }
            if (InterpolationRatio > IterpolationCap) InterpolationRatio = IterpolationCap;

            Interp += Time.deltaTime;
        }
    }

    private static void Interpolate(ServerSnapshot From, ServerSnapshot To, float Time)
    {
        List<ushort> IDs = To.Data.Keys.ToList();
        foreach (ushort ID in IDs)
        {
            if (!From.Data.ContainsKey(ID))
                continue;

            _IInstantiatableDeletable Item = EntityID.GetObject(ID);
            ServerSnapshot.Object ToData = To.Data[ID];
            ServerSnapshot.Object FromData = From.Data[ID];

            IServerSendable ServerItem = Item as IServerSendable;
            DZSettings.EntityType ToType = ToData.Type;
            DZSettings.EntityType FromType = FromData.Type;
            if (ToData.Data == null || FromData.Data == null || ToType != FromType || (int)ToType != ServerItem.ServerObjectType || (int)FromType != ServerItem.ServerObjectType)
                continue;

            if (ServerItem == null)
                continue;

            ServerItem.Interpolate(FromData.Data, ToData.Data, Time);
        }
    }

    private static void Extrapolate(ServerSnapshot From, float Time)
    {
        List<ushort> IDs = From.Data.Keys.ToList();
        foreach (ushort ID in IDs)
        {
            if (!From.Data.ContainsKey(ID))
                continue;

            _IInstantiatableDeletable Item = EntityID.GetObject(ID);
            ServerSnapshot.Object FromData = From.Data[ID];

            IServerSendable ServerItem = Item as IServerSendable;
            DZSettings.EntityType FromType = FromData.Type;
            if (FromData.Data == null || (int)FromType != ServerItem.ServerObjectType)
                continue;

            if (ServerItem == null)
                continue;

            ServerItem.Extrapolate(FromData.Data, Time);
        }
    }

    public static void SendData()
    {
        if (Loader.Socket.SocketConnected)
            SendSnapshot();

        if (!Loader.Socket.Connected)
            return;
    }

    private static void SendSnapshot()
    {
        Packet Setup = new Packet();
        Setup.Write(Client.NumPlayers);
        Loader.Socket.Send(Setup, ServerCode.SyncPlayers);

        Packet SnapshotPacket = new Packet();
        SnapshotPacket.Write(Client.NumPlayers);
        SnapshotPacket.Write(InputMapping.GetBytes());
        Loader.Socket.Send(SnapshotPacket, ServerCode.ClientSnapshot);
    }

    public static void SyncClient(DZUDPSocket.RecievePacketWrapper Packet)
    {
        int NumPlayers = Packet.Data.ReadByte();
        if (NumPlayers != Client.NumPlayers)
        {
            Debug.LogWarning("Sync failed due to inconsistent player count");
            return;
        }
        ushort CID = Packet.Data.ReadUShort();
        if (Client.ID != CID)
            Client.ID.ChangeID(CID);
        for (int i = 0; i < NumPlayers; i++)
        {
            int ID = Packet.Data.ReadByte();
            if (ID == byte.MaxValue)
                continue;
            ushort PlayerEntityID = Packet.Data.ReadUShort();
            if (Client.Players[i].Entity.ID != PlayerEntityID)
                Client.Players[i].Entity.ID.ChangeID(PlayerEntityID, true);
        }
    }

    public static void UnWrapSnapshot(DZUDPSocket.RecievePacketWrapper Packet)
    {
        if (Client == null) return;

        int CheckSum = Packet.Data.ReadInt();
        ServerTickRate = Packet.Data.ReadInt();
        ulong ServerTick = Packet.Data.ReadULong();
        if (ServerTick <= (Histogram.Count == 0 ? 0 : Histogram.Last.ServerTick))
        {
            Debug.LogWarning("Received a late packet");
            return;
        }

        ServerSnapshot Snapshot = new ServerSnapshot();
        Snapshot.ServerTick = ServerTick;

        int NumSnapshotItems = Packet.Data.ReadInt();
        for (int i = 0; i < NumSnapshotItems; i++)
        {
            ServerSnapshot.Object Object = new ServerSnapshot.Object();

            ushort ID = Packet.Data.ReadUShort();
            bool FlaggedToDelete = Packet.Data.ReadBool();
            Object.FlaggedToDelete = FlaggedToDelete;
            if (FlaggedToDelete)
            {
                Snapshot.Data.Add(ID, Object);
                continue;
            }

            DZSettings.EntityType Type = (DZSettings.EntityType)Packet.Data.ReadInt();
            Object.Type = Type;

            object ServerItem = Parse(Type, Packet.Data);
            if (ServerItem == null)
            {
                Debug.LogWarning("Unable to Parse item from server snapshot");
                return;
            }
            Object.Data = ServerItem;

            Snapshot.Data.Add(ID, Object);
        }

        Histogram.Add(Snapshot);
    }

    private static void LoadSnapshot(ServerSnapshot Snapshot, bool ParseData = true)
    {
        List<ushort> IDs = Snapshot.Data.Keys.ToList();
        foreach (ushort ID in IDs)
        {
            _IInstantiatableDeletable Item = EntityID.GetObject(ID);
            ServerSnapshot.Object Object = Snapshot.Data[ID];
            bool FlaggedToDelete = Object.FlaggedToDelete;
            if (FlaggedToDelete)
            {
                if (Item != null)
                    DZEngine.Destroy(Item);
                continue;
            }

            IServerSendable ServerItem = Item as IServerSendable;
            DZSettings.EntityType Type = Object.Type;

            if (ServerItem == null)
                ServerItem = Parse(ID, Type);
            if (ServerItem == null)
            {
                Debug.LogWarning("Unable to Parse item from server snapshot");
                return;
            }

            if ((DZSettings.EntityType)ServerItem.ServerObjectType != Type)
            {
                Debug.LogWarning("Entity Types of ID " + ID + " do not match... (ServerID = " + Type + ", LocalID = " + (DZSettings.EntityType)ServerItem.ServerObjectType + ") resetting IDs and re-parsing");
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
            if (ParseData)
                ServerItem.ParseSnapshot(Object.Data);
            ServerItem.RecentlyUpdated = true;
        }
        foreach (IServerSendable Item in ServerItems)
        {
            if (!Item.ProtectedDeletion && !Item.RecentlyUpdated)
            {
                DZEngine.Destroy(Item);
                Debug.LogWarning("An Item was destroyed as it was not updated by the server");
            }
        }
    }

    private static object Parse(DZSettings.EntityType Type, Packet Data)
    {
        switch (Type)
        {
            case DZSettings.EntityType.PlayerCreature: return PlayerCreature.ParseBytesToSnapshot(Data);
            case DZSettings.EntityType.Tilemap: return Tilemap.ParseBytesToSnapshot(Data);
            case DZSettings.EntityType.TriggerPlate: return TriggerPlate.ParseBytesToSnapshot(Data);
            case DZSettings.EntityType.BulletEntity: return BulletEntity.ParseBytesToSnapshot(Data);
            case DZSettings.EntityType.EnemyCreature: return EnemyCreature.ParseBytesToSnapshot(Data);
            case DZSettings.EntityType.Turret: return Turret.ParseBytesToSnapshot(Data);
            case DZSettings.EntityType.CoinEntity: return CoinEntity.ParseBytesToSnapshot(Data);
            case DZSettings.EntityType.Null: return null;
            default: Debug.LogWarning("Parsing unknown entity type");  return null;
        }
    }

    private static IServerSendable Parse(ushort ID, DZSettings.EntityType Type)
    {
        switch (Type)
        {
            case DZSettings.EntityType.PlayerCreature: return new PlayerCreature(ID);
            case DZSettings.EntityType.Tilemap: return new Tilemap(ID);
            case DZSettings.EntityType.TriggerPlate: return new TriggerPlate(ID);
            case DZSettings.EntityType.BulletEntity: return new BulletEntity(ID);
            case DZSettings.EntityType.EnemyCreature: return new EnemyCreature(ID);
            case DZSettings.EntityType.Turret: return new Turret(ID);
            case DZSettings.EntityType.CoinEntity: return new CoinEntity(ID);
            case DZSettings.EntityType.Null: return null;
            default: Debug.LogWarning("Parsing unknown entity type"); return null;
        }
    }

    public static void Connected()
    {
        Debug.Log("Client Connected");
    }

    public static void Disconnected()
    {
        Debug.Log("Client Disconnected");
        Initialized = false;
        Histogram.Clear();
    }
}

