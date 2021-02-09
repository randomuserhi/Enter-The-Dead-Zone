using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;
using DeadZoneEngine;

public class EnemyCreature : AbstractCreature, IServerSendable
{
    public struct WayPoint
    {
        public int Direction;
        public Vector2Int Position;
    }

    public struct Path
    {
        public List<WayPoint> Traversal;
        public string Map;
    }

    public int ServerObjectType { get; set; } = (int)DZSettings.EntityType.EnemyCreature;
    public bool RecentlyUpdated { get; set; } = false;
    public bool ProtectedDeletion { get; set; } = false;

    public int CurrentWayPoint = 0;
    public Path Traversal;

    public float DecayTimer = 5;
    public int CorpseHP = 5;
    public int Health = 5;
    public float Speed = 1;
    private float[] DynamicRunSpeed;
    public BodyState State;

    public enum BodyState
    {
        Standing,
        Limp
    }

    public EnemyCreature(ushort ID) : base(ID)
    {
        Initialize();
    }
    public EnemyCreature()
    {
        Initialize();
    }

    public override void Render()
    {
        BodyChunks[0].RenderObject.gameObject.transform.localScale = new Vector2(0.5f, 0.5f);
        BodyChunks[1].RenderObject.gameObject.transform.localScale = new Vector2(0.5f, 0.5f);

        if (State == BodyState.Limp) BodyColor = Color.grey;
        BodyChunks[0].RenderColor = BodyColor;
        BodyChunks[1].RenderColor = BodyColor;
    }

    Color BodyColor;
    private void Initialize()
    {
        BodyChunks = new BodyChunk[2];
        BodyChunks[0] = new BodyChunk(this);
        BodyChunks[1] = new BodyChunk(this);
        BodyChunks[0].Collider.radius = 0.25f;
        BodyChunks[1].Collider.radius = 0.25f;
        BodyChunks[0].Context = this;
        BodyChunks[0].ContextType = DZSettings.EntityType.EnemyCreature;
        BodyChunks[1].Context = this;
        BodyChunks[1].ContextType = DZSettings.EntityType.EnemyCreature;
        SetGravity(0f);

        BodyChunkConnections = new DistanceJoint[1];
        BodyChunkConnections[0] = new DistanceJoint();
        BodyChunkConnections[0].Set(new DistanceJointData(BodyChunks[0], BodyChunks[1], 0.5f, Vector2.zero));
        BodyChunkConnections[0].Active = false;

        Physics2D.IgnoreCollision(BodyChunks[0].Collider, BodyChunks[1].Collider, true); //Ignore collisions between body parts

        DynamicRunSpeed = new float[2];

        BodyColor = new Color(0, 1, 0);
        BodyChunks[0].RenderColor = BodyColor;
        BodyChunks[1].RenderColor = BodyColor;
    }

    public Vector2 Position
    {
        get
        {
            if (BodyChunks[0] != null)
                return BodyChunks[0].Position;
            return Vector2.zero;
        }
        set
        {
            if (BodyChunks[0] != null)
                BodyChunks[0].Position = value;
            if (BodyChunks[1] != null)
                BodyChunks[1].Position = value;
        }
    }

    public void ApplyVelocity(Vector2 Direction, float Force)
    {
        BodyChunks[0].Velocity += Direction * Force;
    }

    public void ApplyVelocity(Vector2 Vel)
    {
        BodyChunks[0].Velocity += Vel;
    }

    public void ServerUpdate()
    {
    }

    public Func<Tilemap, WayPoint, Vector2, Vector2> PathingAlgorithm = (Map, WP, Position) =>
    {
        Vector2 WPActualPosition = Main.Tilemap.TilemapToWorldPosition(WP.Position);
        return (WPActualPosition - Position).normalized;
    };

    private bool Death = false;
    public override void Update()
    {
        if (Health < 0)
        {
            State = BodyState.Limp;
            if (DecayTimer > 0)
                DecayTimer -= Time.fixedDeltaTime;
            else
            {
                DecayTimer = 5;
                Health--;
            }
            if (!Death)
            {
                Death = true;
                int Count = UnityEngine.Random.Range(1, 4);
                for (int i = 0; i < Count; i++)
                {
                    CoinEntity CE = new CoinEntity();
                    CE.Money = 1;
                    CE.Position = Position;
                    CE.Health = UnityEngine.Random.Range(0f, 1f) < 0.25f ? 1 : 0;
                }
            }
        }
        if (Health < -CorpseHP)
        {
            DZEngine.Destroy(this);
        }

        if (CurrentWayPoint < Traversal.Traversal.Count)
        {
            WayPoint WP = Traversal.Traversal[CurrentWayPoint];
            Vector2 WPActualPosition = Main.Tilemap.TilemapToWorldPosition(WP.Position);
            MovementDirection = PathingAlgorithm(Main.Tilemap, WP, Position);
            if ((Position.x < WPActualPosition.x + 0.5 && Position.x > WPActualPosition.x - 0.5) &&
                (Position.y < WPActualPosition.y + 0.5 && Position.y > WPActualPosition.y - 0.5))
                CurrentWayPoint++;
        }
        else
        {
            Main.TakeLifeForce();
            DZEngine.Destroy(this);
        }

        UpdateBodyState();
        UpdateMovement();
    }

    private void UpdateBodyState()
    {

    }

    private Vector2 MovementDirection;
    private void UpdateMovement()
    {
        switch (State)
        {
            case BodyState.Limp:
                {
                    BodyChunks[0].Velocity *= 0.8f;
                    BodyChunks[1].Velocity *= 0.8f;
                }
                break;
            case BodyState.Standing:
                {
                    DynamicRunSpeed[0] = 1f;
                    DynamicRunSpeed[1] = 1.5f;

                    BodyChunks[0].Velocity += new Vector2(Speed * DynamicRunSpeed[0] * MovementDirection.x, Speed * DynamicRunSpeed[0] * MovementDirection.y);
                    BodyChunks[1].Velocity += new Vector2(Speed * DynamicRunSpeed[1] * MovementDirection.x, Speed * DynamicRunSpeed[1] * MovementDirection.y);

                    BodyChunks[0].Velocity *= 0.8f;
                    BodyChunks[1].Velocity *= 0.8f;
                }
                break;
        }
    }

    public override void BodyPhysicsUpdate()
    {
        switch (State)
        {
            case BodyState.Limp:
                {
                    SetGravity(0f);
                    BodyChunks[1].SpriteOffset = Vector2.Lerp(BodyChunks[1].SpriteOffset, Vector2.zero, 4 * Time.fixedDeltaTime);
                    BodyChunkConnections[0].Active = true;
                }
                break;
            case BodyState.Standing:
                {
                    SetGravity(0f);
                    BodyChunks[1].SpriteOffset = Vector2.Lerp(BodyChunks[1].SpriteOffset, new Vector2(0, 0.3f), 4 * Time.fixedDeltaTime);
                    BodyChunkConnections[0].Active = false;

                    float Dist = Vector2.Distance(BodyChunks[0].Position, BodyChunks[1].Position);
                    Vector2 Dir = (BodyChunks[0].Position - BodyChunks[1].Position).normalized;
                    BodyChunks[1].Position += Dist * Dir * 0.8f;
                    BodyChunks[1].Velocity += Dist * Dir * 0.8f;

                    BodyChunks[0].Velocity *= 0.8f;
                    BodyChunks[1].Velocity *= 0.8f;
                }
                break;
        }
    }

    public void SetGravity(float Gravity)
    {
        BodyChunks[0].Gravity = Gravity;
        BodyChunks[1].Gravity = Gravity;
    }

    protected override void OnDelete()
    {
        BodyChunks[0].Delete();
        BodyChunks[1].Delete();
        BodyChunkConnections[0].Delete();
    }

    public override byte[] GetBytes()
    {
        List<byte> Data = new List<byte>();
        Data.AddRange(BitConverter.GetBytes((int)State));
        Data.AddRange(BodyChunks[0].GetBytes());
        Data.AddRange(BodyChunks[1].GetBytes());
        Data.AddRange(BodyChunkConnections[0].GetBytes());
        return Data.ToArray();
    }

    public override void ParseBytes(DZNetwork.Packet Data)
    {
        ParseSnapshot((Data)ParseBytesToSnapshot(Data));
    }

    public struct Data
    {
        public BodyState State;
        public BodyChunk.Data BodyChunk0;
        public BodyChunk.Data BodyChunk1;
        public DistanceJoint.Data BodyChunkConnections0;
    }

    public static object ParseBytesToSnapshot(DZNetwork.Packet Data)
    {
        return new Data()
        {
            State = (BodyState)Data.ReadInt(),
            BodyChunk0 = (BodyChunk.Data)BodyChunk.ParseBytesToSnapshot(Data),
            BodyChunk1 = (BodyChunk.Data)BodyChunk.ParseBytesToSnapshot(Data),
            BodyChunkConnections0 = (DistanceJoint.Data)DistanceJoint.ParseBytesToData(Data)
        };
    }
    public override void ParseSnapshot(object ObjectData)
    {
        Data Data = (Data)ObjectData;
        State = Data.State;
        BodyChunks[0].ParseSnapshot(Data.BodyChunk0);
        BodyChunks[1].ParseSnapshot(Data.BodyChunk1);
        BodyChunkConnections[0].ParseSnapshot(Data.BodyChunkConnections0);
    }

    public override object GetSnapshot()
    {
        return new Data()
        {
            State = State,
            BodyChunk0 = (BodyChunk.Data)BodyChunks[0].GetSnapshot(),
            BodyChunk1 = (BodyChunk.Data)BodyChunks[1].GetSnapshot(),
            BodyChunkConnections0 = (DistanceJoint.Data)BodyChunkConnections[0].GetSnapshot()
        };
    }

    public override void Interpolate(object FromData, object ToData, float Time)
    {
        Data From = (Data)FromData;
        Data To = (Data)ToData;
        BodyChunks[0].Interpolate(From.BodyChunk0, To.BodyChunk0, Time);
        BodyChunks[1].Interpolate(From.BodyChunk1, To.BodyChunk1, Time);
        BodyChunkConnections[0].Interpolate(From.BodyChunkConnections0, To.BodyChunkConnections0, Time);
    }
}
