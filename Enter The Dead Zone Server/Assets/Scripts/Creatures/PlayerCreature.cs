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

    public struct PlayerStats
    {
        public float RunSpeed;
    }

    private enum BodyState
    {
        UprightTest,
        Limp,
        Standing
    }

    public class Control
    {
        public PlayerController Owner;
        public Vector2 MovementDirection;

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

    private BodyState State; //Ragdoll state
    public PlayerStats Stats; //General player stats

    private float[] DynamicRunSpeed; //Controls Speed of each bodychunk

    public PlayerCreature(ushort ID) : base(ID)
    {
        Initialize();
    }
    public PlayerCreature()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (DZSettings.ClientSidePrediction)
            Histogram = new DZNetwork.JitterBuffer<PlayerSnapshot>();

        Controller = new Control();
        State = BodyState.Standing;
        Stats.RunSpeed = 2f;

        BodyChunks = new BodyChunk[2];
        BodyChunks[0] = new BodyChunk(this);
        BodyChunks[1] = new BodyChunk(this);
        SetGravity(0f);

        BodyChunkConnections = new DistanceJoint[1];
        BodyChunkConnections[0] = new DistanceJoint();
        BodyChunkConnections[0].Set(new DistanceJointData(BodyChunks[0], BodyChunks[1], 1.5f, Vector2.zero));
        BodyChunkConnections[0].Active = false;

        Physics2D.IgnoreCollision(BodyChunks[0].Collider, BodyChunks[1].Collider, true); //Ignore collisions between body parts

        DynamicRunSpeed = new float[2];
    }

    public void ServerUpdate()
    {
        if (Controller.Owner == null || DZSettings.ClientSidePrediction == false) return;

        UpdateReconcilliation();

        BodyChunks[0].PhysicallyActive = true;
        BodyChunks[1].PhysicallyActive = true;
        BodyChunkConnections[0].PhysicallyActive = true;
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
        switch (State)
        {
            case BodyState.UprightTest:
                {
                    BodyChunks[0].Velocity -= new Vector2(0, BodyChunks[0].Gravity);
                    BodyChunks[1].Velocity -= new Vector2(0, BodyChunks[1].Gravity);

                    int A = BodyChunks[0].GetContacts();
                    BodyChunkConnections[0].ARatio = 1;
                    for (int i = 0; i < A; i++)
                    {
                        if ((BodyChunks[0].Contacts[i].point - BodyChunks[0].Position).y < 0)
                            BodyChunkConnections[0].ARatio = 0;
                    }

                    if (Controller != null)
                    {
                        DynamicRunSpeed[0] = 1f;
                        DynamicRunSpeed[1] = 3f;
                        BodyChunks[0].Velocity += new Vector2(Stats.RunSpeed * DynamicRunSpeed[0] * Controller.MovementDirection.x, 0);
                        BodyChunks[1].Velocity += new Vector2(Stats.RunSpeed * DynamicRunSpeed[1] * Controller.MovementDirection.x, 0);
                    }

                    BodyChunks[0].Velocity *= new Vector2(0.8f, 1);
                    BodyChunks[1].Velocity *= new Vector2(0.8f, 1);
                }
                break;
            case BodyState.Limp:
                {

                }
                break;
            case BodyState.Standing:
                {
                    DynamicRunSpeed[0] = 1f;
                    DynamicRunSpeed[1] = 3f;
                    if (Controller != null)
                    {
                        BodyChunks[0].Velocity += new Vector2(Stats.RunSpeed * DynamicRunSpeed[0] * Controller.MovementDirection.x, 0);
                        BodyChunks[0].Velocity += new Vector2(0, Stats.RunSpeed * DynamicRunSpeed[0] * Controller.MovementDirection.y);
                        BodyChunks[1].Velocity += new Vector2(Stats.RunSpeed * DynamicRunSpeed[1] * Controller.MovementDirection.x, 0);
                        BodyChunks[1].Velocity += new Vector2(0, Stats.RunSpeed * DynamicRunSpeed[1] * Controller.MovementDirection.y);
                    }

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
            case BodyState.UprightTest:
                {
                    SetGravity(0.3f);
                    BodyChunkConnections[0].Active = true;

                    BodyChunks[0].Velocity -= new Vector2(0, BodyChunks[0].Gravity);
                    BodyChunks[1].Velocity += new Vector2(0, BodyChunks[1].Gravity);

                    BodyChunks[0].Velocity *= new Vector2(0.8f, 1);
                    BodyChunks[1].Velocity *= new Vector2(0.8f, 1);
                }
                break;
            case BodyState.Limp:
                {
                    SetGravity(0f);
                    BodyChunkConnections[0].Active = true;
                }
                break;
            case BodyState.Standing:
                {
                    SetGravity(0f);
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
        Data.AddRange(BitConverter.GetBytes(Controller.InputID));
        Data.AddRange(BodyChunks[0].GetBytes());
        Data.AddRange(BodyChunks[1].GetBytes());
        Data.AddRange(BodyChunkConnections[0].GetBytes());
        return Data.ToArray();
    }

    public override void ParseBytes(DZNetwork.Packet Data)
    {
        ParseSnapshot((Data)ParseBytesToData(Data));
    }

    public struct Data
    {
        public ulong InputID;
        public BodyChunk.Data BodyChunk0;
        public BodyChunk.Data BodyChunk1;
        public DistanceJoint.Data BodyChunkConnections0;
    }

    public static object ParseBytesToData(DZNetwork.Packet Data)
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
        {
            return;
        }
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

    private class PlayerSnapshot
    {
        public Data Snapshot;
        public Control.Snapshot Controls;
    }
    DZNetwork.JitterBuffer<PlayerSnapshot> Histogram = null;
    private void UpdateReconcilliation()
    {
        Histogram.Add(new PlayerSnapshot()
        {
            Snapshot = (Data)GetSnapshot(),
            Controls = Controller.GetSnapshot()
        });
    }

    private bool Reconcille = false;
    private object LastReconcilled = null;
    public override void Interpolate(object FromData, object ToData, float Time)
    {
        if (Controller.Owner != null && DZSettings.ClientSidePrediction && LastReconcilled != FromData)
        {
            LastReconcilled = FromData;
            Data ReconcilleSnapshot = (Data)FromData;
            PlayerSnapshot Current = null;
            Histogram.Iterate(S =>
            {
                if (S.Value.Controls.InputID >= ReconcilleSnapshot.InputID)
                {
                    Current = S.Value;
                }
            }, S => S.Value.Controls.InputID >= ReconcilleSnapshot.InputID);
            Debug.Log("> " + Histogram.Count);
            if (Current != null)
            {
                Histogram.Dequeue(Current);
                Debug.Log(BodyChunks[0].Position);
                Reconcille = true;
                ParseSnapshot(ReconcilleSnapshot);
                Reconcille = false;
                Histogram.Iterate(S =>
                {
                    Controller.ParseSnapshot(S.Value.Controls);
                    DZEngine.PhysicsUpdate();
                });
                Debug.Log(BodyChunks[0].Position);
            }
            else
            {
                Histogram.Dequeue(Histogram.Last);
            }
            return;
        }
        Data From = (Data)FromData;
        Data To = (Data)ToData;
        BodyChunks[0].Interpolate(From.BodyChunk0, To.BodyChunk0, Time);
        BodyChunks[1].Interpolate(From.BodyChunk1, To.BodyChunk1, Time);
        BodyChunkConnections[0].Interpolate(From.BodyChunkConnections0, To.BodyChunkConnections0, Time);
    }
}
