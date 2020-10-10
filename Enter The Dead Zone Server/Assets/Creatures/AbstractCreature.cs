using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

public abstract class AbstractCreature : AbstractWorldEntity, IUpdatableAndDeletable
{
    public BodyChunk[] BodyChunks;
    public DistanceJoint[] BodyChunkConnections;

    public AbstractCreature() : base() { SetEntityType(); }
    public AbstractCreature(ulong ID) : base(ID) { SetEntityType(); }

    private bool _FlaggedToDelete;
    public bool FlaggedToDelete { get { return _FlaggedToDelete; } set { _FlaggedToDelete = value; } }
    public abstract void Instantiate();
    public virtual void PreUpdate() { }
    public virtual void Update() { }
    public virtual void IteratedUpdate() { }
    public virtual void Delete() { FlaggedToDelete = true; }
}
