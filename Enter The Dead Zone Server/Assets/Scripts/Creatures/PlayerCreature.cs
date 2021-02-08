using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using ClientHandle;
using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

public class PlayerCreature : AbstractCreature, IServerSendable
{
    public int ServerObjectType { get; set; } = (int)DZSettings.EntityType.PlayerCreature;
    public bool RecentlyUpdated { get; set; } = false;
    public bool ProtectedDeletion { get; set; } = false;

    public bool Out;
    public float RunSpeed;

    public class Control
    {
        public PlayerController Owner;
        public Vector2 MovementDirection;
        public Vector2 ShieldVector;
        public float Interact;

        public struct Snapshot
        {
            public ulong InputID;
            public Vector2 MovementDirection;
        }

        public ulong InputID;

        public Snapshot GetSnapshot()
        {
            return new Snapshot()
            {
                InputID = InputID++,
                MovementDirection = MovementDirection
            };
        }

        public void ParseSnapshot(Snapshot Snapshot)
        {
            MovementDirection = Snapshot.MovementDirection;
        }
    }
    public Control Controller { get; private set; } //Controller for player movement

    private float[] DynamicRunSpeed; //Controls Speed of each bodychunk

    public PlayerCreature(ushort ID) : base(ID)
    {
        Initialize();
    }
    public PlayerCreature()
    {
        Initialize();
    }

    Color BodyColor;
    private void Initialize()
    {
        if (DZSettings.ClientSidePrediction)
            Histogram = new DZNetwork.JitterBuffer<PlayerSnapshot>();

        Controller = new Control();
        RunSpeed = 2f;

        BodyChunks = new BodyChunk[2];
        BodyChunks[0] = new BodyChunk(this);
        BodyChunks[1] = new BodyChunk(this);
        BodyChunks[0].Context = this;
        BodyChunks[0].ContextType = DZSettings.EntityType.PlayerCreature;
        BodyChunks[1].Context = this;
        BodyChunks[1].ContextType = DZSettings.EntityType.PlayerCreature;
        SetGravity(0f);

        BodyChunkConnections = new DistanceJoint[1];
        BodyChunkConnections[0] = new DistanceJoint();
        BodyChunkConnections[0].Set(new DistanceJointData(BodyChunks[0], BodyChunks[1], 0.5f, Vector2.zero));
        BodyChunkConnections[0].Active = false;

        Physics2D.IgnoreCollision(BodyChunks[0].Collider, BodyChunks[1].Collider, true); //Ignore collisions between body parts

        DynamicRunSpeed = new float[2];

        BodyColor = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
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
        if (Controller.Owner == null || DZSettings.ClientSidePrediction == false) return;

        UpdateReconcilliation();
        LerpReconcilleError();

        BodyChunks[0].PhysicallyActive = true;
        BodyChunks[1].PhysicallyActive = true;
        BodyChunkConnections[0].PhysicallyActive = true;
        BodyChunks[0].Kinematic = false;
        BodyChunks[1].Kinematic = false;
    }

    public override void Update()
    {
        UpdateBodyState();
        UpdateMovement();
    }

    private void UpdateBodyState()
    {

    }

    private void UpdateMovement()
    {
        DynamicRunSpeed[0] = 1f;
        DynamicRunSpeed[1] = 1.5f;
        if (Controller != null)
        {
            BodyChunks[0].Velocity += new Vector2(RunSpeed * DynamicRunSpeed[0] * Controller.MovementDirection.x, RunSpeed * DynamicRunSpeed[0] * Controller.MovementDirection.y);
            BodyChunks[1].Velocity += new Vector2(RunSpeed * DynamicRunSpeed[1] * Controller.MovementDirection.x, RunSpeed * DynamicRunSpeed[1] * Controller.MovementDirection.y);
        }

        BodyChunks[0].Velocity *= 0.8f;
        BodyChunks[1].Velocity *= 0.8f;
    }

    public override void BodyPhysicsUpdate()
    {
        BodyChunks[0].Kinematic = false;
        BodyChunks[1].Kinematic = false;

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
        Data.AddRange(BitConverter.GetBytes(Controller.InputID));
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
        public ulong InputID;
        public BodyChunk.Data BodyChunk0;
        public BodyChunk.Data BodyChunk1;
        public DistanceJoint.Data BodyChunkConnections0;
    }

    public static object ParseBytesToSnapshot(DZNetwork.Packet Data)
    {
        return new Data()
        {
            InputID = Data.ReadULong(),
            BodyChunk0 = (BodyChunk.Data)BodyChunk.ParseBytesToSnapshot(Data),
            BodyChunk1 = (BodyChunk.Data)BodyChunk.ParseBytesToSnapshot(Data),
            BodyChunkConnections0 = (DistanceJoint.Data)DistanceJoint.ParseBytesToData(Data)
        };
    }
    public override void ParseSnapshot(object ObjectData)
    {
        if (Controller.Owner != null && DZSettings.ClientSidePrediction && !Reconcille)
            return;
        Data Data = (Data)ObjectData;
        BodyChunks[0].ParseSnapshot(Data.BodyChunk0);
        BodyChunks[1].ParseSnapshot(Data.BodyChunk1);
        BodyChunkConnections[0].ParseSnapshot(Data.BodyChunkConnections0);
    }

    public override object GetSnapshot()
    {
        return new Data()
        {
            InputID = Controller.InputID,
            BodyChunk0 = (BodyChunk.Data)BodyChunks[0].GetSnapshot(),
            BodyChunk1 = (BodyChunk.Data)BodyChunks[1].GetSnapshot(),
            BodyChunkConnections0 = (DistanceJoint.Data)BodyChunkConnections[0].GetSnapshot()
        };
    }

    public class PlayerSnapshot
    {
        public Data Snapshot;
        public Control.Snapshot Controls;
    }
    public DZNetwork.JitterBuffer<PlayerSnapshot> Histogram = null;
    private void UpdateReconcilliation()
    {
        Histogram.Add(new PlayerSnapshot()
        {
            Snapshot = (Data)GetSnapshot(),
            Controls = Controller.GetSnapshot()
        });
    }

    private void LerpReconcilleError()
    {
        const float Amount = 4f;
        float Error = (ReconcilledSelf.BodyChunk0.Position - BodyChunks[0].Position).SqrMagnitude();
        if (Error < 1)
        {
            BodyChunks[0].Position = Vector3.Lerp(BodyChunks[0].Position, ReconcilledSelf.BodyChunk0.Position, Amount * Time.fixedDeltaTime);
            BodyChunks[1].Position = Vector3.Lerp(BodyChunks[1].Position, ReconcilledSelf.BodyChunk1.Position, Amount * Time.fixedDeltaTime);
        }
        else
        {
            BodyChunks[0].Position = ReconcilledSelf.BodyChunk0.Position;
            BodyChunks[1].Position = ReconcilledSelf.BodyChunk1.Position;
        }
    }

    private PlayerSnapshot Current = null;
    public Data CurrentSelf;
    private bool ValidPredictPass;
    private bool FinishedPredict;
    public void StartClientPrediction(Game.ServerSnapshot FromData)
    {
        ValidPredictPass = FromData.Data.ContainsKey(ID);
        CurrentSelf = (Data)GetSnapshot();
        Reconcille = true;
        if (!ValidPredictPass) return;
        Data ClientPredictBaseline = (Data)FromData.Data[ID].Data;
        if (LastReconcilled >= ClientPredictBaseline.InputID)
        {
            ValidPredictPass = false;
            return;
        }
        LastReconcilled = ClientPredictBaseline.InputID;
        Histogram.Iterate(S =>
        {
            if (S.Value.Controls.InputID >= ClientPredictBaseline.InputID)
            {
                Current = S.Value;
            }
        }, S => S.Value.Controls.InputID >= ClientPredictBaseline.InputID);
        if (Current != null)
        {
            Histogram.Dequeue(Current);
            ParseSnapshot(ClientPredictBaseline);
            FinishedPredict = false;
            CurrentKey = Histogram.FirstKey;
        }
        else
        {
            Histogram.Clear();
            ValidPredictPass = false;
        }
    }
    private DZNetwork.JitterBuffer<PlayerSnapshot>.Key CurrentKey;
    public void ClientPrediction()
    {
        if (!ValidPredictPass) return;
        if (CurrentKey != null)
        {
            Controller.ParseSnapshot(CurrentKey.Value.Controls);
            if (!FinishedPredict && CurrentKey.Next == null)
            {
                FinishedPredict = true;
                ReconcilledSelf = (Data)GetSnapshot();
            }
            CurrentKey = CurrentKey.Next;
        }
    }
    public void EndClientPrediction()
    {
        ParseSnapshot(CurrentSelf);
        Reconcille = false;
    }

    private Data ReconcilledSelf;
    private bool Reconcille = false;
    private ulong LastReconcilled = 0;
    public override void Interpolate(object FromData, object ToData, float Time)
    {
        if (Controller.Owner != null && DZSettings.ClientSidePrediction)
            return;

        Data From = (Data)FromData;
        Data To = (Data)ToData;
        BodyChunks[0].Interpolate(From.BodyChunk0, To.BodyChunk0, Time);
        BodyChunks[1].Interpolate(From.BodyChunk1, To.BodyChunk1, Time);
        BodyChunkConnections[0].Interpolate(From.BodyChunkConnections0, To.BodyChunkConnections0, Time);
    }
}
