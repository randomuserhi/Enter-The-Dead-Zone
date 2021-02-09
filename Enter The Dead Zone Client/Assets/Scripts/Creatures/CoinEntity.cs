using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;
using DZNetwork;

public class CoinEntity : AbstractWorldEntity, IPhysicsUpdatable, IRenderer, IServerSendable
{
    public int SortingLayer { get; set; }
    public int ServerObjectType { get; set; } = (int)DZSettings.EntityType.CoinEntity;
    public bool RecentlyUpdated { get; set; } = false;
    public bool ProtectedDeletion { get; set; } = false;

    public int Money = 1;
    public int Health = 0;
    public BodyChunk Coin;

    public CoinEntity(ushort ID) : base(ID)
    {
        Init();
    }

    public CoinEntity() : base()
    {
        Init();
    }

    public void Init()
    {
        Coin = new BodyChunk();
        Coin.Context = this;
        Coin.ContextType = DZSettings.EntityType.CoinEntity;
        Coin.Collider.radius = 0.01f;
        Coin.Kinematic = true;
    }

    public Vector2 Position
    {
        get
        {
            if (Coin != null)
                return Coin.Position;
            return Vector2.zero;
        }
        set
        {
            if (Coin != null)
                Coin.Position = value;
        }
    }

    public void InitializeRenderer()
    {

    }

    public void Render()
    {
        Coin.RenderObject.transform.localScale = new Vector2(0.2f, 0.2f);
        if (Health > 0)
            Coin.RenderColor = Color.red;
        else
            Coin.RenderColor = Color.magenta;
    }

    public void ServerUpdate()
    {

    }

    public void FixedUpdate()
    {

    }

    public void IsolateVelocity() { }

    public void RestoreVelocity() { }

    protected override void OnDelete()
    {
        DZEngine.Destroy(Coin);
    }

    public override byte[] GetBytes()
    {
        List<byte> Data = new List<byte>();
        Data.AddRange(BitConverter.GetBytes(Money));
        Data.AddRange(BitConverter.GetBytes(Health));
        Data.AddRange(Coin.GetBytes());
        return Data.ToArray();
    }

    public override void ParseBytes(Packet Data)
    {
        ParseSnapshot(ParseBytesToSnapshot(Data));
    }

    public struct Data
    {
        public int Money;
        public int Health;
        public BodyChunk.Data Coin;
    }

    public override object GetSnapshot()
    {
        return new Data()
        {
            Money = Money,
            Health = Health,
            Coin = (BodyChunk.Data)Coin.GetSnapshot()
        };
    }

    public static object ParseBytesToSnapshot(DZNetwork.Packet Data)
    {
        return new Data()
        {
            Money = Data.ReadInt(),
            Health = Data.ReadInt(),
            Coin = (BodyChunk.Data)BodyChunk.ParseBytesToSnapshot(Data)
        };
    }

    public override void ParseSnapshot(object ObjectData)
    {
        Data Data = (Data)ObjectData;
        Money = Data.Money;
        Health = Data.Health;
        Coin.ParseSnapshot(Data.Coin);
    }

    public override void Interpolate(object FromData, object ToData, float Time)
    {
        Data From = (Data)FromData;
        Data To = (Data)ToData;
        Money = From.Money;
        Health = From.Health;
        Coin.Interpolate(From.Coin, To.Coin, Time);
    }

    public override void Extrapolate(object FromData, float Time)
    {
        Data From = (Data)FromData;
        Money = From.Money;
        Health = From.Health;
        Coin.Extrapolate(From.Coin, Time);
    }
}
