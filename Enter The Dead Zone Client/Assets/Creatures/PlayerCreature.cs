using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

public class PlayerController
{
    public Vector2 Direction;
}

public class PlayerCreature : AbstractCreature
{
    public struct PlayerStats
    {
        public float RunSpeed;
    }

    private enum BodyState
    {
        Limp,
        Standing,
        Crawling
    }

    private BodyState State;
    private PlayerController Controller;
    public PlayerStats Stats;

    private float[] DynamicRunSpeed; //Controls Speed of each bodychunk

    public PlayerCreature(ulong ID) : base(ID) { }

    public PlayerCreature()
    {
        Init(new BodyChunk(this), new BodyChunk(this), new DistanceJoint());
    }

    //TODO implement INIT into abstractCreature template
    public void Init(BodyChunk A, BodyChunk B, DistanceJoint Joint)
    {
        Controller = new PlayerController();
        State = BodyState.Standing;
        Stats.RunSpeed = 1f;

        BodyChunks = new BodyChunk[2];
        BodyChunks[0] = A;
        BodyChunks[1] = B;
        SetGravity(0f);

        BodyChunkConnections = new DistanceJoint[1];
        BodyChunkConnections[0] = Joint;
        BodyChunkConnections[0].Set(BodyChunks[0], BodyChunks[1], 1.5f, Vector2.zero);
        BodyChunkConnections[0].Active = false;

        Physics2D.IgnoreCollision(BodyChunks[0].Collider, BodyChunks[1].Collider, true); //Ignore collisions between body parts

        DynamicRunSpeed = new float[2];
    }

    protected override void SetEntityType()
    {
        Type = EntityType.PlayerCreature;
    }

    public override void Update()
    {
        //Test movement
        Controller.Direction.x = Input.GetAxis("Horizontal");
        Controller.Direction.y = Input.GetAxis("Vertical");

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
            case BodyState.Standing:
                {
                    //TODO:: implement friction of different surfaces (harder to accelerate on ice, feet cant gain traction (similar to ice in minecraft))
                    DynamicRunSpeed[0] = 1f;
                    DynamicRunSpeed[1] = 3f;
                    BodyChunks[0].Velocity += new Vector2(Stats.RunSpeed * DynamicRunSpeed[0] * Controller.Direction.x, 0);
                    BodyChunks[0].Velocity += new Vector2(0, Stats.RunSpeed * DynamicRunSpeed[0] * Controller.Direction.y);
                    BodyChunks[1].Velocity += new Vector2(Stats.RunSpeed * DynamicRunSpeed[1] * Controller.Direction.x, 0);
                    BodyChunks[1].Velocity += new Vector2(0, Stats.RunSpeed * DynamicRunSpeed[1] * Controller.Direction.y);

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
                    BodyChunkConnections[0].Active = true;
                }
                break;
            case BodyState.Standing:
                {
                    BodyChunkConnections[0].Active = false;

                    float Dist = Vector2.Distance(BodyChunks[0].Position, BodyChunks[1].Position);
                    Vector2 Dir = (BodyChunks[1].Position - BodyChunks[0].Position).normalized;
                    BodyChunks[1].Position += -Dist * Dir * 0.8f;
                    BodyChunks[1].Velocity += -Dist * Dir * 0.8f;
                }
                break;
        }
    }

    public void SetGravity(float Gravity)
    {
        BodyChunks[0].Gravity = Gravity;
        BodyChunks[1].Gravity = Gravity;
    }

    public override byte[] GetBytes()
    {
        //EntityType + ulong EntityID + ulong EntityID + float Distance, Vector2 Anchor
        //TODO:: might be worth optimizing this to use arrays or something if performance is hit hard
        List<byte> Data = new List<byte>();
        Data.AddRange(BitConverter.GetBytes((int)Type));
        Data.AddRange(BitConverter.GetBytes(ID));
        Data.AddRange(BitConverter.GetBytes(Active));
        Data.AddRange(BodyChunks[0].GetBytes());
        Data.AddRange(BodyChunks[1].GetBytes());
        Data.AddRange(BodyChunkConnections[0].GetBytes());
        return Data.ToArray();
    }
}
