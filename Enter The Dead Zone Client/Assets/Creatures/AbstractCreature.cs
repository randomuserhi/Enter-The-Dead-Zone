using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

public abstract class AbstractCreature : AbstractWorldEntity, IUpdatableAndDeletable
{
    public BodyChunk[] BodyChunks;
    public DistanceJoint[] BodyChunkConnections;

    public AbstractCreature() : base() { SetEntityType(); }
    public AbstractCreature(ulong ID) : base(ID) { SetEntityType(); }

    private bool _Active = true;
    public bool Active { get { return _Active; } set { _Active = value; } }
    private bool _FlaggedToDelete;
    public bool FlaggedToDelete { get { return _FlaggedToDelete; } set { _FlaggedToDelete = value; } }
    public virtual void Instantiate()
    {
        DZEngine.UpdatableDeletableObjects.Add(this);

        if (BodyChunks != null)
            for (int i = 0; i < BodyChunks.Length; i++)
                BodyChunks[i].Instantiate();
        if (BodyChunkConnections != null)
            for (int i = 0; i < BodyChunkConnections.Length; i++)
                BodyChunkConnections[i].Instantiate();
    }
    public virtual void PreUpdate() { }
    public virtual void Update() { }
    public virtual void BodyPhysicsUpdate() { }
    public virtual void IteratedUpdate() { }
    public virtual void Delete()
    {
        FlaggedToDelete = true;

        if (BodyChunks != null)
            for (int i = 0; i < BodyChunks.Length; i++)
                BodyChunks[i].Delete();
        if (BodyChunkConnections != null)
            for (int i = 0; i < BodyChunkConnections.Length; i++)
                BodyChunkConnections[i].Delete();
    }
}
