using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

public class PlayerCreature : AbstractCreature
{
    public PlayerCreature()
    {
        BodyChunks = new BodyChunk[2];
        BodyChunks[0] = new BodyChunk(this);
        BodyChunks[1] = new BodyChunk(this);
        BodyChunkConnections = new DistanceJoint[1];
        BodyChunkConnections[0] = new DistanceJoint();
        BodyChunkConnections[0].Set(BodyChunks[0], BodyChunks[1], 1.5f, Vector2.zero);
    }
    
    protected override void SetEntityType()
    {
        Type = EntityType.PlayerCreature;
    }

    public override void Instantiate()
    {
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
