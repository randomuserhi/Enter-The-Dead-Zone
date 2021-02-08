using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;
using DZNetwork;

public class BulletEntity : AbstractWorldEntity, IPhysicsUpdatable, IRenderer, IServerSendable
{
    public int SortingLayer { get; set; }
    public int ServerObjectType { get; set; } = (int)DZSettings.EntityType.BulletEntity;
    public bool RecentlyUpdated { get; set; } = false;
    public bool ProtectedDeletion { get; set; } = false;

    BodyChunk Bolt;
    public float Speed = 10;
    public Vector2 Direction = Vector2.up;

    public BulletEntity(ushort ID) : base(ID)
    {
        Init();
    }

    public BulletEntity() : base()
    {
        Init();
    }

    public void Init()
    {
        Bolt = new BodyChunk();
        Bolt.Collider.radius = 0.2f;
        Bolt.Kinematic = true;
    }

    public Vector2 Position
    {
        get
        {
            if (Bolt != null)
                return Bolt.Position;
            return Vector2.zero;
        }
        set
        {
            if (Bolt != null)
                Bolt.Position = value;
        }
    }

    public void InitializeRenderer()
    {

    }

    public void Render()
    {

    }

    public void ServerUpdate()
    {

    }

    private RaycastHit2D[] RayCasts = new RaycastHit2D[6];
    public void FixedUpdate()
    {
        float ScaledSpeed = Speed * Time.fixedDeltaTime;
        Vector2 NormalDirection = Vector2.Perpendicular(Direction).normalized * (Bolt.Collider.radius + 0.01f);
        RayCasts[0] = Physics2D.Raycast(Bolt.Position, Direction, ScaledSpeed);
        RayCasts[1] = Physics2D.Raycast(Bolt.Position + NormalDirection, Direction, ScaledSpeed);
        RayCasts[2] = Physics2D.Raycast(Bolt.Position - NormalDirection, Direction, ScaledSpeed);
        Vector2 End = Bolt.Position + Direction * ScaledSpeed;
        RayCasts[3] = Physics2D.Raycast(End, -Direction, ScaledSpeed - Bolt.Collider.radius - 0.1f);
        RayCasts[4] = Physics2D.Raycast(End + NormalDirection, -Direction, ScaledSpeed);
        RayCasts[5] = Physics2D.Raycast(End - NormalDirection, -Direction, ScaledSpeed);
        bool FoundHit = false;
        int Index = 0;
        for (int i = 0; i < RayCasts.Length; i++)
        {
            if (RayCasts[i].collider != null && Vector2.Dot(RayCasts[i].normal, Direction) < 0)
            {
                FoundHit = true;
                Index = i;
                break;
            }
        }
        if (FoundHit)
        {
            RaycastHit2D Hit = RayCasts[Index];
            float Distance = Mathf.Abs((Hit.point - Bolt.Position).magnitude) - Bolt.Collider.radius;
            Vector2 NewPosition = Bolt.Position + Direction * Distance;
            AbstractWorld WorldContext = Hit.collider.gameObject.GetComponent<AbstractWorld>();
            Direction = Vector2.Reflect(Direction, Hit.normal).normalized;
            if (WorldContext != null)
            {
                if (WorldContext.Type == DZSettings.EntityType.PlayerCreature)
                {
                    PlayerCreature Player = (PlayerCreature)WorldContext.Context;
                    Player.ApplyVelocity(-Direction, Speed);
                }
            }
            Bolt.Position = NewPosition + Direction * 0.1f + Hit.normal * 0.1f;
        }
        else
        {
            Bolt.Position += Direction * ScaledSpeed;
        }
    }

    public void IsolateVelocity() { }

    public void RestoreVelocity() { }

    public override byte[] GetBytes()
    {
        List<byte> Data = new List<byte>();
        Data.AddRange(BitConverter.GetBytes(Speed));
        Data.AddRange(BitConverter.GetBytes(Direction.x));
        Data.AddRange(BitConverter.GetBytes(Direction.y));
        Data.AddRange(Bolt.GetBytes());
        return Data.ToArray();
    }

    public override void ParseBytes(Packet Data)
    {
        ParseSnapshot(ParseBytesToSnapshot(Data));
    }

    public struct Data
    {
        public float Speed;
        public Vector2 Direction;
        public BodyChunk.Data Bolt;
    }

    public override object GetSnapshot()
    {
        return new Data()
        {
            Speed = Speed,
            Direction = Direction,
            Bolt = (BodyChunk.Data)Bolt.GetSnapshot()
        };
    }

    public static object ParseBytesToSnapshot(DZNetwork.Packet Data)
    {
        return new Data()
        {
            Speed = Data.ReadFloat(),
            Direction = new Vector2(Data.ReadFloat(), Data.ReadFloat()),
            Bolt = (BodyChunk.Data)BodyChunk.ParseBytesToSnapshot(Data)
        };
    }

    public override void ParseSnapshot(object ObjectData)
    {
        Data Data = (Data)ObjectData;
        Speed = Data.Speed;
        Direction = Data.Direction;
        Bolt.ParseSnapshot(Data.Bolt);
    }

    public override void Interpolate(object FromData, object ToData, float Time)
    {
        Data From = (Data)FromData;
        Data To = (Data)ToData;
        Direction = From.Direction;
        Speed = From.Speed;
        Bolt.Interpolate(From.Bolt, To.Bolt, Time);
    }
}
