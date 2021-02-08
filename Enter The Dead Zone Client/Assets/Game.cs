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
    public static int ServerTickRate = 30;
    public static int ClientSendRate = 30;
    public static int ClientTickRate = 60;
    private static int ClientTicksTillSend = 0;
    public static ulong ClientTicks = 0;
    public static ulong ClientTickAsServerTick = 0;
    public static bool Initialized = false;

    private class ExtrapolatedQueue
    {
        private Packet[] ExtrapolatedSnapshots = new Packet[ServerTickRate];
        private int HeadPosition = 0;
        private int TailPosition = 0;
        public int Count = 0;
        private bool OverwriteTail = false;

        public void Enqueue(Packet Packet)
        {
            Count++;
            ExtrapolatedSnapshots[HeadPosition] = Packet;
            HeadPosition++;
            if (OverwriteTail)
            {
                Count--;
                TailPosition++;
                if (TailPosition == ExtrapolatedSnapshots.Length)
                    TailPosition = 0;
                OverwriteTail = false;
            }
            if (HeadPosition == ExtrapolatedSnapshots.Length)
                HeadPosition = 0;
            if (HeadPosition == TailPosition)
                OverwriteTail = true;
        }

        public Packet Dequeue()
        {
            if (Count == 0) return null;
            Count--;
            Packet P = ExtrapolatedSnapshots[TailPosition];
            TailPosition++;
            if (TailPosition == ExtrapolatedSnapshots.Length)
                TailPosition = 0;
            return P;
        }

        public Packet DequeueFromFront()
        {
            if (Count == 0) return null;
            Count--;
            HeadPosition--;
            if (HeadPosition < 0)
                HeadPosition = ExtrapolatedSnapshots.Length - 1;
            Packet P = ExtrapolatedSnapshots[HeadPosition];
            return P;
        }

        public Packet this[int Index]
        {
            get
            {
                int Seek = HeadPosition - 1 - Index;
                while (Seek < 0) Seek += ExtrapolatedSnapshots.Length;
                return ExtrapolatedSnapshots[Seek];
            }
        }
    }

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
                    }
                    float FromTick = (From.ServerTick * ServerToClientTick);
                    float Origin = (ClientTicks - FromTick);
                    float Time = Origin / ((To.ServerTick * ServerToClientTick) - FromTick);
                    Interpolate(From, To, Time);

                    int LargestHistogram = 0;
                    for (int i = 0; i < Client.Players.Length; i++)
                    {
                        if (Client.Players[i] != null && Client.Players[i].Entity.Histogram.Count > LargestHistogram)
                        {
                            Client.Players[i].Entity.StartClientPrediction(From);
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
                else
                    LoadSnapshot(From, true);
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
        }

        DZEngine.FixedUpdate();

        ClientTicksTillSend++;
        if (ClientTicksTillSend >= ClientTickRate / ClientSendRate)
        {
            SendData();
            ClientTicksTillSend = 0;
        }
        ClientTicks++;
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

    public static void SendData()
    {
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
            Loader.Socket.Send(Setup, ServerCode.SyncPlayers);
            return;
        }

        Packet SnapshotPacket = new Packet();
        SnapshotPacket.Write(Client.NumPlayers);
        SnapshotPacket.Write(Histogram.Count == 0 ? 0 : Histogram.Last.ServerTick);
        SnapshotPacket.Write(InputMapping.GetBytes());
        Loader.Socket.Send(SnapshotPacket, ServerCode.ClientSnapshot);
    }

    public static void SyncClient(DZUDPSocket.RecievePacketWrapper Packet)
    {
        if (Client.Setup == false)
        {
            int NumPlayers = Packet.Data.ReadByte();
            if (NumPlayers != Client.NumPlayers)
            {
                Debug.LogWarning("Sync failed due to inconsistent player count");
                return;
            }
            Client.ID.ChangeID(Packet.Data.ReadUShort());
            for (int i = 0; i < NumPlayers; i++)
            {
                int ID = Packet.Data.ReadByte();
                if (ID == byte.MaxValue)
                    continue;
                ushort PlayerEntityID = Packet.Data.ReadUShort();
                Debug.Log(PlayerEntityID);
                Client.Players[i].Entity.ID.ChangeID(PlayerEntityID, true);
            }
            Client.Setup = true;
        }
    }

    public static void UnWrapSnapshot(DZUDPSocket.RecievePacketWrapper Packet)
    {
        if (Client == null || !Client.Setup) return;

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
            case DZSettings.EntityType.PlayerCreature: return PlayerCreature.ParseBytesToData(Data);
            case DZSettings.EntityType.Tilemap: return Tilemap.ParseBytesToData(Data);
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
    }
}

