using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using InternalEngine.Entity;
using InternalEngine.Entity.Interactions;
using InternalEngine.Physics;

using UnityEngine;

namespace InternalEngine
{
    public class IntEngine
    {
        public static float InvDeltaTime = 90f;
        public const int NumIterations = 10;

        public static void Initialise()
        {
            Entities.Add(new PointEntity());
            Entities[0].InvMass = 0;
            Entities[0].InvInertia = 0;

            for (int i = 0; i < 10; i++)
            {
                PointEntity Obj = new PointEntity();
                Entities.Add(Obj);
                Obj.Position = new Vector2(0, 2 * (i + 1));

                DistanceJoint Joint = new DistanceJoint();
                Joint.Set(Entities[i], Entities[i + 1], 2, new Vector2(0, 0));
                EntityJoints.Add(Joint);
            }

            Entities.Add(new PointEntity(20000));
        }

        public static List<EntityObject> Entities = new List<EntityObject>();
        public static List<EntityJoint> EntityJoints = new List<EntityJoint>();
        public static void PerformTimeStep()
        {
            InvDeltaTime = 1 / Time.deltaTime; //Update InvDeltaTime

            //Force updates / velocity updates should be performed before here
            for (int i = 1; i < Entities.Count; i++)
            {
                Entities[i].PhysicsUpdate?.Invoke(Entities[i]);
            }

            Resolver.PreStep(EntityJoints);
            Resolver.ApplyImpulses(EntityJoints, NumIterations);
        }
    }
}
