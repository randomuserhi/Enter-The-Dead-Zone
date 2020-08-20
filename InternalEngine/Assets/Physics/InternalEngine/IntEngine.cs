using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using InternalEngine.Entity;
using InternalEngine.Entity.Interactions;
using InternalEngine.Colliders;

using UnityEngine;

namespace InternalEngine
{
    public class IntEngine
    {
        public const float DeltaTime = 1f / 30f;
        public const float InvDeltaTime = 30f;

        public const float AllowedPenetration = 0.01f;

        public static void Initialise()
        {
            Entities.Add(new PointEntity());
            Entities[0].InvMass = 0;
            Entities[0].InvInertia = 0;

            for (int i = 0; i < 100; i++)
            {
                PointEntity Obj = new PointEntity();
                Entities.Add(Obj);
                Obj.Position = new Vector2(1, 2 * (i + 1));

                EntityJoint Joint = new EntityJoint();
                Joint.Set(Entities[i], Entities[i + 1], 2, new Vector2(0, 0));
                EntityJoints.Add(Joint);
            }
        }

        private static List<Manifold> Manifolds = new List<Manifold>();
        public static List<EntityObject> Entities = new List<EntityObject>();
        public static List<EntityJoint> EntityJoints = new List<EntityJoint>();
        public static void PerformTimeStep()
        {
            Manifolds.Clear(); //Remove for warm starting
            Int_Collision.CalculateManifolds(Manifolds, Entities);

            //Force updates / velocity updates should be performed before here
            for (int i = 1; i < Entities.Count; i++)
            {
                Entities[i].Velocity -= new Vector2(0, 1);
            }

            CollisionResolver.PreStep(Manifolds, EntityJoints);
            CollisionResolver.ApplyImpulses(Manifolds, EntityJoints, 10);

            for (int i = 0; i < Entities.Count; i++)
            {
                if (Entities[i].InvMass == 0) continue;

                Entities[i].Position += Entities[i].Velocity * DeltaTime;
                Entities[i].Rotation += Entities[i].AngularVelocity * DeltaTime;
            }
        }
    }
}
