using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

public class PlayerCreature : AbstractCreature
{
    private BodyChunk MainBodyChunk;
    private Vector3 LockedFeetPos;

    public PlayerCreature()
    {
        BodyChunks = new BodyChunk[2];
        BodyChunks[0] = new BodyChunk(this);
        BodyChunks[1] = new BodyChunk(this);
        BodyChunkConnections = new DistanceJoint[1];
        BodyChunkConnections[0] = new DistanceJoint();
        BodyChunkConnections[0].Set(BodyChunks[0], BodyChunks[1], 1.5f, Vector2.zero);

        MainBodyChunk = BodyChunks[0]; //body part that will be locked to the ground (feet)
    }
    
    protected override void SetEntityType()
    {
        Type = EntityType.PlayerCreature;
    }

    public override void Update()
    {
        UpdateBodyPosture();
    }

    private void UpdateBodyPosture()
    {
        //0.375 and 1.125 is for Lifting legs over ledges, otherwise use 1 and 1
        BodyChunks[1].Velocity += new Vector2(0, 1f * BodyChunks[0].Gravity);
        BodyChunks[0].Velocity -= new Vector2(0, 1f * BodyChunks[1].Gravity);
        BodyChunks[1].Velocity = new Vector2(BodyChunks[1].Velocity.x * 0.8f, BodyChunks[1].Velocity.y);
        BodyChunks[0].Velocity = new Vector2(BodyChunks[0].Velocity.x * 0.8f, BodyChunks[0].Velocity.y);

        //Due to friction feet will automatically lage behind the head creating the leaning forward into movement we want => if not accelerate the head faster than the feet or something
        // => change this later to lock feet to the ground etc
        BodyChunks[0].Velocity += new Vector2(0.5f * Input.GetAxis("Horizontal"), 0);
        BodyChunks[1].Velocity += new Vector2(0.5f * Input.GetAxis("Horizontal"), 0);
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
