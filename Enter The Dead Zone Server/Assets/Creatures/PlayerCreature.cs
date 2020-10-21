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
    private BodyChunk LowerBodyChunk;
    private PlayerController Controller;

    public PlayerCreature()
    {
        Controller = new PlayerController();

        BodyChunks = new BodyChunk[2];
        BodyChunks[0] = new BodyChunk(this);
        BodyChunks[1] = new BodyChunk(this);
        BodyChunkConnections = new DistanceJoint[1];
        BodyChunkConnections[0] = new DistanceJoint();
        BodyChunkConnections[0].Set(BodyChunks[0], BodyChunks[1], 1.5f, Vector2.zero);

        LowerBodyChunk = BodyChunks[0]; //body part that will be locked to the ground (feet)
    }
    
    protected override void SetEntityType()
    {
        Type = EntityType.PlayerCreature;
    }

    public override void Update()
    {
        UpdatePhysics();
        UpdateBodyPosture();
    }

    private void UpdatePhysics()
    {
        int NumContactPoints = LowerBodyChunk.GetContacts();
        Vector2 ContactPoint = NumContactPoints != 0 ? LowerBodyChunk.Contacts[0].point - LowerBodyChunk.Position : Vector2.zero;
        if (ContactPoint.y < 0)
        {
            //is grounded, apply friction, lock feet etc..
            LowerBodyChunk.Velocity = new Vector2(BodyChunks[0].Velocity.x * 0.9f, BodyChunks[0].Velocity.y);
            BodyChunkConnections[0].ARatio = 0; //Lock the feet
        }
        else
        {
            BodyChunkConnections[0].ARatio = 1; //Unlock feet
        }
    }

    private void UpdateBodyPosture()
    {
        //0.375 and 1.125 is for Lifting legs over ledges, otherwise use 1 and 1, 0.8f indicates the dampening
        BodyChunks[1].Velocity += new Vector2(0, 1f * BodyChunks[0].Gravity);
        BodyChunks[0].Velocity -= new Vector2(0, 1f * BodyChunks[1].Gravity);
        BodyChunks[1].Velocity = new Vector2(BodyChunks[1].Velocity.x * 0.8f, BodyChunks[1].Velocity.y);
        BodyChunks[0].Velocity = new Vector2(BodyChunks[0].Velocity.x * 0.8f, BodyChunks[0].Velocity.y);

        //Due to friction feet will automatically lage behind the head creating the leaning forward into movement we want (Friction will need to be hard coded if its not in a physics material) => if not accelerate the head faster than the feet or something
        // => change this later to lock feet to the ground etc
        Vector2 Horizontal = new Vector2(0.5f * Controller.Direction.x, 0);
        BodyChunks[0].Velocity += Horizontal;
        BodyChunks[1].Velocity += Horizontal;
    }

    public override void Instantiate()
    {
        base.Instantiate();
        for (int i = 0; i < BodyChunks.Length; i++)
        {
            BodyChunks[i].Instantiate();
        }
        for (int i = 0; i < BodyChunkConnections.Length; i++)
        {
            BodyChunkConnections[i].Instantiate();
        }
    }

    public override void Delete()
    {
        base.Delete();
        for (int i = 0; i < BodyChunks.Length; i++)
        {
            BodyChunks[i].Delete();
        }
        for (int i = 0; i < BodyChunkConnections.Length; i++)
        {
            BodyChunkConnections[i].Delete();
        }
    }

    public override byte[] GetBytes()
    {
        throw new NotImplementedException();
    }
}
