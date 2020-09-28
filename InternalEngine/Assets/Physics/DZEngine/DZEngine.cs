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
            UpdatableDeletableObjects.Add(new BodyChunk());
            ((BodyChunk)UpdatableDeletableObjects[0]).InvMass = 0;
            ((BodyChunk)UpdatableDeletableObjects[0]).InvInertia = 0;

            List<DistanceJoint> D = new List<DistanceJoint>();
            for (int i = 0; i < 10; i++)
            {
                BodyChunk Obj = new BodyChunk();
                UpdatableDeletableObjects.Add(Obj);
                Obj.Position = new Vector2(0, 2 * (i + 1));

                DistanceJoint Joint = new DistanceJoint();
                Joint.Set((BodyChunk)UpdatableDeletableObjects[i], (BodyChunk)UpdatableDeletableObjects[i + 1], 2, new Vector2(0, 0));
                D.Add(Joint);
            }
            UpdatableDeletableObjects.AddRange(D);

            UpdatableDeletableObjects.Add(new BodyChunk());
        }

        public static List<AbstractWorldEntity> WorldEntities = new List<AbstractWorldEntity>(); //TODO:: this needs implementing
        private static List<IUpdatableAndDeletable> LastUpdatableDeletableObjects = new List<IUpdatableAndDeletable>();
        public static List<IUpdatableAndDeletable> UpdatableDeletableObjects = new List<IUpdatableAndDeletable>();
        public static void FixedUpdate()
        {
            InvDeltaTime = 1 / Time.deltaTime;

            LastUpdatableDeletableObjects.Clear();
            LastUpdatableDeletableObjects.AddRange(UpdatableDeletableObjects);
            UpdatableDeletableObjects.Clear();
            for (int i = 0; i < LastUpdatableDeletableObjects.Count; i++)
            {
                LastUpdatableDeletableObjects[i].Update();

                if (!LastUpdatableDeletableObjects[i].FlaggedToDelete)
                    UpdatableDeletableObjects.Add(LastUpdatableDeletableObjects[i]);
                else
                {
                    LastUpdatableDeletableObjects[i].Delete(); //Destroy Interface
                    ((AbstractWorldEntity)LastUpdatableDeletableObjects[i]).Destroy(); //Destroy Entity
                }
            }

            for (int i = 0; i < UpdatableDeletableObjects.Count; i++)
            {
                UpdatableDeletableObjects[i].PreUpdate();
            }
            
            for (int j = 0; j < NumPhysicsIterations; j++)
            {
                for (int i = 0; i < UpdatableDeletableObjects.Count; i++)
                {
                    UpdatableDeletableObjects[i].IteratedUpdate();
                }
            }
        }
    }
}
