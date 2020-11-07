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
            P = new PlayerCreature();
            P.Instantiate();
        }

        private static List<AbstractWorldEntity> LastInstantiatableDeletable = new List<AbstractWorldEntity>();
        public static List<AbstractWorldEntity> InstantiatableDeletable = new List<AbstractWorldEntity>();

        private static List<IPhysicsUpdatable> LastPhysicsUpdatableObjects = new List<IPhysicsUpdatable>();
        public static List<IPhysicsUpdatable> PhysicsUpdatableObjects = new List<IPhysicsUpdatable>();

        private static List<IUpdatable> LastUpdatableObjects = new List<IUpdatable>();
        public static List<IUpdatable> UpdatableObjects = new List<IUpdatable>();

        private static List<IIteratableUpdatable> LastIteratableUpdatableObjects = new List<IIteratableUpdatable>();
        public static List<IIteratableUpdatable> IteratableUpdatableObjects = new List<IIteratableUpdatable>();

        public static void FixedUpdate()
        {
            InvDeltaTime = 1f / Time.deltaTime;

            LastInstantiatableDeletable.Clear();
            LastInstantiatableDeletable.AddRange(InstantiatableDeletable);
            InstantiatableDeletable.Clear();
            for (int i = 0; i < LastInstantiatableDeletable.Count; i++)
            {
                if (!LastInstantiatableDeletable[i].FlaggedToDelete)
                {
                    InstantiatableDeletable.Add(LastInstantiatableDeletable[i]);
                }
                else
                {
                    LastInstantiatableDeletable[i].Delete(); //Destroy Interface
                    LastInstantiatableDeletable[i].Destroy(); //Destroy Entity
                }
            }

            //Isolate the general physics updates from creature body physics -> this is specific for maintaining physic objects inside of creature bodies
            //(the creature body is updated relative to itself without the need to worry about countering general physics (its isolated from general physics))
            LastPhysicsUpdatableObjects.Clear();
            LastPhysicsUpdatableObjects.AddRange(PhysicsUpdatableObjects);
            PhysicsUpdatableObjects.Clear();
            for (int i = 0; i < LastPhysicsUpdatableObjects.Count; i++)
            {
                if (!LastPhysicsUpdatableObjects[i].FlaggedToDelete)
                {
                    PhysicsUpdatableObjects.Add(LastPhysicsUpdatableObjects[i]);
                    if (LastPhysicsUpdatableObjects[i].Active)
                        LastPhysicsUpdatableObjects[i].IsolateVelocity();
                }
            }

            LastUpdatableObjects.Clear();
            LastUpdatableObjects.AddRange(UpdatableObjects);
            UpdatableObjects.Clear();
            for (int i = 0; i < LastUpdatableObjects.Count; i++)
            {
                if (!LastUpdatableObjects[i].FlaggedToDelete)
                {
                    UpdatableObjects.Add(LastUpdatableObjects[i]);
                    if (LastUpdatableObjects[i].Active)
                        LastUpdatableObjects[i].BodyPhysicsUpdate(); //This is specific to creatures mainly to update self-righting bodies or other body animation specific physics
                                                                     //its seperated and run in a seperate physics operation to prevent self-righting body physics from being counteracted from normal physics (such as gravity).
                                                                     //In other words this simply isolates the body physics from the standard physics
                }
            }

            //Check and resolve physics constraints (Joints etc) => Essentially update the isolated physics of just creature bodies
            LastIteratableUpdatableObjects.Clear();
            LastIteratableUpdatableObjects.AddRange(IteratableUpdatableObjects);
            IteratableUpdatableObjects.Clear();
            for (int i = 0; i < LastIteratableUpdatableObjects.Count; i++)
            {
                if (!LastIteratableUpdatableObjects[i].FlaggedToDelete)
                {
                    IteratableUpdatableObjects.Add(LastIteratableUpdatableObjects[i]);
                    if (LastIteratableUpdatableObjects[i].Active)
                        LastIteratableUpdatableObjects[i].PreUpdate();
                }
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
