using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

public abstract class AbstractCreature : AbstractWorldEntity, IUpdatable, IRenderer
{
    public int SortingLayer { get; set; }

    public BodyChunk[] BodyChunks;
    public DistanceJoint[] BodyChunkConnections;

    public AbstractCreature() { }
    public AbstractCreature(ulong ID) : base(ID) { }

    public virtual void Update() { }
    public virtual void BodyPhysicsUpdate() { }
    protected override void OnDelete() { }

    public virtual void InitializeRenderer() { }
    public virtual void Render() { }
}
