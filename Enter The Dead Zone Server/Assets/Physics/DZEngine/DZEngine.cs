using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

namespace DeadZoneEngine
{
    public class DZEngine
    {
        public static float InvDeltaTime = 0;
        public static int NumPhysicsIterations = 10;

        public static void Initialize()
        {
            /*new BodyChunk().Instantiate();
            ((BodyChunk)UpdatableDeletableObjects[0]).InvMass = 0;
            ((BodyChunk)UpdatableDeletableObjects[0]).InvInertia = 0;

            List<DistanceJoint> D = new List<DistanceJoint>();
            for (int i = 0; i < 10; i++)
            {
                BodyChunk Obj = new BodyChunk();
                Obj.Instantiate();
                Obj.Position = new Vector2(0, 2 * (i + 1));

                DistanceJoint Joint = new DistanceJoint();
                Joint.Set((BodyChunk)UpdatableDeletableObjects[i], (BodyChunk)UpdatableDeletableObjects[i + 1], 2, new Vector2(0, 0));
                D.Add(Joint);
            }
            UpdatableDeletableObjects.AddRange(D);

            new BodyChunk(2000).Instantiate();*/
            new PlayerCreature().Instantiate();
        }

        public static List<IUpdatableAndDeletable> SnapShotObjects = new List<IUpdatableAndDeletable>(); 

        public static List<AbstractWorldEntity> WorldEntities = new List<AbstractWorldEntity>(); //TODO:: this needs implementing
        private static List<PhysicalObject> PhysicalObjects = new List<PhysicalObject>();
        private static List<IUpdatableAndDeletable> LastUpdatableDeletableObjects = new List<IUpdatableAndDeletable>();
        public static List<IUpdatableAndDeletable> UpdatableDeletableObjects = new List<IUpdatableAndDeletable>();
        public static void FixedUpdate()
        {
            InvDeltaTime = 1f / Time.deltaTime;

            SnapShotObjects.Clear();
            PhysicalObjects.Clear();
            LastUpdatableDeletableObjects.Clear();
            LastUpdatableDeletableObjects.AddRange(UpdatableDeletableObjects);
            UpdatableDeletableObjects.Clear();
            for (int i = 0; i < LastUpdatableDeletableObjects.Count; i++)
            {
                if (LastUpdatableDeletableObjects[i].Active)
                    LastUpdatableDeletableObjects[i].Update();
                if (!LastUpdatableDeletableObjects[i].FlaggedToDelete)
                {
                    UpdatableDeletableObjects.Add(LastUpdatableDeletableObjects[i]);
                    PhysicalObject Object = LastUpdatableDeletableObjects[i] as PhysicalObject;
                    if (Object != null && LastUpdatableDeletableObjects[i].Active)
                        PhysicalObjects.Add(Object);

                    AbstractWorldEntity E = LastUpdatableDeletableObjects[i] as AbstractWorldEntity;
                    if (E != null && E.Type != AbstractWorldEntity.EntityType.BodyChunk || E.Type != AbstractWorldEntity.EntityType.DistanceJoint)
                        SnapShotObjects.Add(LastUpdatableDeletableObjects[i]);
                }
                else
                {
                    LastUpdatableDeletableObjects[i].Delete(); //Destroy Interface
                    ((AbstractWorldEntity)LastUpdatableDeletableObjects[i]).Destroy(); //Destroy Entity
                }
            }

            //Isolate the general physics updates from creature body physics -> this is specific for maintaining physic objects inside of creature bodies
            //(the creature body is updated relative to itself without the need to worry about countering general physics (its isolated from general physics))
            for (int i = 0; i < PhysicalObjects.Count; i++)
            {
                PhysicalObjects[i].IsolateVelocity();
            }

            for (int i = 0; i < UpdatableDeletableObjects.Count; i++)
            {
                UpdatableDeletableObjects[i].BodyPhysicsUpdate(); //This is specific to creatures mainly to update self-righting bodies or other body animation specific physics
            }

            //Check and resolve physics constraints (Joints etc) => Essentially update the isolated physics of just creature bodies
            for (int i = 0; i < UpdatableDeletableObjects.Count; i++)
            {
                if (UpdatableDeletableObjects[i].Active)
                    UpdatableDeletableObjects[i].PreUpdate();
            }

            for (int j = 0; j < NumPhysicsIterations; j++)
            {
                for (int i = 0; i < UpdatableDeletableObjects.Count; i++)
                {
                    if (UpdatableDeletableObjects[i].Active)
                        UpdatableDeletableObjects[i].IteratedUpdate();
                }

                Physics2D.Simulate(1f / 60f / NumPhysicsIterations);
            }

            //Restore the velocities back to normal, we are no longer considering the creature body in an isolated system
            for (int i = 0; i < PhysicalObjects.Count; i++)
            {
                PhysicalObjects[i].RestoreVelocity();
            }

            //Check and resolve physics constraints (Joints etc) => Essentially update the general physics of all bodies
            for (int i = 0; i < UpdatableDeletableObjects.Count; i++)
            {
                if (UpdatableDeletableObjects[i].Active)
                    UpdatableDeletableObjects[i].PreUpdate();
            }

            for (int j = 0; j < NumPhysicsIterations; j++)
            {
                for (int i = 0; i < UpdatableDeletableObjects.Count; i++)
                {
                    if (UpdatableDeletableObjects[i].Active)
                        UpdatableDeletableObjects[i].IteratedUpdate();
                }

                Physics2D.Simulate(1f / 60f / NumPhysicsIterations);
            }
        }
    }
}
