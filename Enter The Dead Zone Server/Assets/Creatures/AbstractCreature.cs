using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

public abstract class AbstractCreature : AbstractWorldEntity, IUpdatable
{
    public BodyChunk[] BodyChunks;
    public DistanceJoint[] BodyChunkConnections;

    public AbstractCreature() { }
    public AbstractCreature(ulong ID) : base(ID) { }

    protected override void _Instantiate()
    {
        DZEngine.UpdatableObjects.Add(this);

        if (BodyChunks != null)
            for (int i = 0; i < BodyChunks.Length; i++)
                BodyChunks[i].Instantiate();
        if (BodyChunkConnections != null)
            for (int i = 0; i < BodyChunkConnections.Length; i++)
                BodyChunkConnections[i].Instantiate();
    }
    public virtual void Update() { }
    public virtual void BodyPhysicsUpdate() { }
    protected override void _Delete()
    {
        if (BodyChunks != null)
            for (int i = 0; i < BodyChunks.Length; i++)
                BodyChunks[i].Delete();
        if (BodyChunkConnections != null)
            for (int i = 0; i < BodyChunkConnections.Length; i++)
                BodyChunkConnections[i].Delete();
    }
}
