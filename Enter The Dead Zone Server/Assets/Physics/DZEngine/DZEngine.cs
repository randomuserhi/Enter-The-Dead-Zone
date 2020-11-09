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

        //Test
        static PlayerCreature P;

        public static void Initialize()
        {
            P = new PlayerCreature();
            P.Instantiate();
        }

        public static List<AbstractWorldEntity> InstantiatableDeletable = new List<AbstractWorldEntity>();
        public static List<IPhysicsUpdatable> PhysicsUpdatableObjects = new List<IPhysicsUpdatable>();
        public static List<IUpdatable> UpdatableObjects = new List<IUpdatable>();
        public static List<IIteratableUpdatable> IteratableUpdatableObjects = new List<IIteratableUpdatable>();

        public static void FixedUpdate()
        {
            InvDeltaTime = 1f / Time.deltaTime;

            InstantiatableDeletable.RemoveAll(I => 
            {
                if (I.FlaggedToDelete)
                {
                    I.Delete();
                    I.Destroy();
                }
                return I.FlaggedToDelete;
            });

            //Isolate the general physics updates from creature body physics -> this is specific for maintaining physic objects inside of creature bodies
            //(the creature body is updated relative to itself without the need to worry about countering general physics (its isolated from general physics))
            PhysicsUpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    I.IsolateVelocity();
                }
                return I.FlaggedToDelete;
            });

            UpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    I.BodyPhysicsUpdate(); //This is specific to creatures mainly to update self-righting bodies or other body animation specific physics
                                           //its seperated and run in a seperate physics operation to prevent self-righting body physics from being counteracted from normal physics (such as gravity).
                                           //In other words this simply isolates the body physics from the standard physics
                }
                return I.FlaggedToDelete;
            });

            //Check and resolve physics constraints (Joints etc) => Essentially update the isolated physics of just creature bodies
            IteratableUpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    I.PreUpdate();
                }
                return I.FlaggedToDelete;
            });
            for (int j = 0; j < NumPhysicsIterations; j++)
            {
                for (int i = 0; i < IteratableUpdatableObjects.Count; i++)
                {
                    if (IteratableUpdatableObjects[i].Active)
                        IteratableUpdatableObjects[i].IteratedUpdate();
                }

                Physics2D.Simulate(1f / 60f / NumPhysicsIterations);
            }

            //Restore the velocities back to normal, we are no longer considering the creature body in an isolated system
            for (int i = 0; i < PhysicsUpdatableObjects.Count; i++)
            {
                if (PhysicsUpdatableObjects[i].Active)
                    PhysicsUpdatableObjects[i].RestoreVelocity();
            }

            for (int i = 0; i < UpdatableObjects.Count; i++)
            {
                if (UpdatableObjects[i].Active)
                    UpdatableObjects[i].Update(); 
            }

            //Check and resolve physics constraints (Joints etc) => Essentially update the general physics of all bodies
            for (int i = 0; i < IteratableUpdatableObjects.Count; i++)
            {
                if (IteratableUpdatableObjects[i].Active)
                    IteratableUpdatableObjects[i].PreUpdate();
            }
            for (int j = 0; j < NumPhysicsIterations; j++)
            {
                for (int i = 0; i < IteratableUpdatableObjects.Count; i++)
                {
                    if (IteratableUpdatableObjects[i].Active)
                        IteratableUpdatableObjects[i].IteratedUpdate();
                }

                Physics2D.Simulate(1f / 60f / NumPhysicsIterations);
            }
        }
    }
}
