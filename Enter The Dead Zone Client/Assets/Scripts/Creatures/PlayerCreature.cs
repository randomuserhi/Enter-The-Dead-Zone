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
    public int RecentlyUpdated { get; set; }

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
        Data.AddRange(BodyChunks[0].GetBytes());
        Data.AddRange(BodyChunks[1].GetBytes());
        Data.AddRange(BodyChunkConnections[0].GetBytes());
        return Data.ToArray();
    }

    public override void ParseBytes(DZNetwork.Packet Data, ulong ServerTick)
    {
        BodyChunks[0].ParseBytes(Data, ServerTick);
        BodyChunks[1].ParseBytes(Data, ServerTick);
        BodyChunkConnections[0].ParseBytes(Data, ServerTick);
    }
}
