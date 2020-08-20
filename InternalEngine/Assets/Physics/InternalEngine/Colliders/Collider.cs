using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using InternalEngine.Entity;
using InternalEngine.Entity.Interactions;

namespace InternalEngine.Colliders
{
    public enum ColliderType
    {
        Circle,
        Box
    }

    public abstract class Int_Collider
    {
        public ColliderType Type;
        public int Layer;
        public EntityObject Entity;

        public Int_Collider(ColliderType Type, EntityObject Entity)
        {
            this.Type = Type;
            this.Entity = Entity;
        }
    }

    public enum ManifoldType
    {
        CircleCircle,
        CircleBox,
        BoxBox
    }

    public class Int_ContactPoint
    {
        public Vector2 Position;
        public Vector2 Normal;
        public float Seperation;
        public float AccumulatedNormalImpulse;
        public float AccumulatedTangentImpulse;
        public float MassNormal;
        public float MassTangent;
        public float Bias;
    }

    public class Manifold
    {
        public ManifoldType Type;

        public EntityObject A;
        public EntityObject B;

        public Int_ContactPoint[] ContactPoints;

        public Manifold(ManifoldType Type, EntityObject A, EntityObject B, int i, int j)
        {
            this.Type = Type;
            this.A = i > j ? A : B;
            this.B = i > j ? B : A;
            ContactPoints = null;
        }

        public void Update(Int_ContactPoint[] NewContactPoints)
        {

        }
    }

    public class CollisionResolver
    {
        public static void PreStep(List<Manifold> Manifolds, List<EntityJoint> Joints)
        {
            for (int j = 0; j < Manifolds.Count; j++)
            {
                Manifold M = Manifolds[j];
                for (int i = 0; i < M.ContactPoints.Length; i++)
                {
                    Int_ContactPoint Contact = M.ContactPoints[i];
                    Vector2 RA = Contact.Position - M.A.Position;
                    Vector2 RB = Contact.Position - M.B.Position;

                    //Precompute normal mass, tangent mass and bias
                    float RnA = Vector2.Dot(RA, Contact.Normal);
                    float RnB = Vector2.Dot(RB, Contact.Normal);
                    float KNormal = M.A.InvMass + M.B.InvMass;
                    KNormal += M.A.InvInertia * (Vector2.Dot(RA, RA) - RnA * RnA) +
                               M.B.InvInertia * (Vector2.Dot(RB, RB) - RnB * RnB);
                    Contact.MassNormal = 1f / KNormal;

                    Vector2 Tangent = Math2D.Cross(Contact.Normal, 1f);
                    float RtA = Vector2.Dot(RA, Tangent);
                    float RtB = Vector2.Dot(RB, Tangent);
                    float KTangent = M.A.InvMass + M.B.InvMass;
                    KTangent += M.A.InvInertia * (Vector2.Dot(RA, RA) - RtA * RtA) +
                               M.B.InvInertia * (Vector2.Dot(RB, RB) - RtB * RtB);
                    Contact.MassTangent = 1f / KTangent;

                    Contact.Bias = -0.1f * IntEngine.InvDeltaTime * Mathf.Min(0f, Contact.Seperation + IntEngine.AllowedPenetration);
                }
            }

            for (int i = 0; i < Joints.Count; i++)
            {
                Joints[i].PreStep();
            }
        }

        public static void ApplyImpulses(List<Manifold> Manifolds, List<EntityJoint> Joints, int NumIterations)
        {
            for (int j = 0; j < NumIterations; j++)
            {
                for (int i = 0; i < Manifolds.Count; i++)
                {
                    Manifold M = Manifolds[i];
                    EntityObject A = M.A;
                    EntityObject B = M.B;

                    for (int k = 0; k < M.ContactPoints.Length; k++)
                    {
                        Int_ContactPoint Contact = M.ContactPoints[k];

                        Vector2 RA = Contact.Position - A.Position;
                        Vector2 RB = Contact.Position - B.Position;

                        Vector2 RelativeVel = B.Velocity + Math2D.Cross(B.AngularVelocity, RB) - A.Velocity - Math2D.Cross(A.AngularVelocity, RA);

                        float Normal = Vector2.Dot(RelativeVel, Contact.Normal);
                        float NormalImpulse = Contact.MassNormal * (-Normal + Contact.Bias);

                        //Clamping of accumulated Impulse
                        float OldNormalImpulse = Contact.AccumulatedNormalImpulse;
                        Contact.AccumulatedNormalImpulse = Mathf.Max(OldNormalImpulse + NormalImpulse, 0f);
                        NormalImpulse = Contact.AccumulatedNormalImpulse - OldNormalImpulse;

                        Vector2 Impulse = NormalImpulse * Contact.Normal;

                        A.Velocity -= A.InvMass * Impulse;
                        A.AngularVelocity -= A.InvInertia * Math2D.Cross(RA, Impulse);

                        B.Velocity += B.InvMass * Impulse;
                        B.AngularVelocity += B.InvInertia * Math2D.Cross(RB, Impulse);

                        //Friction Implementation goes here
                    }
                }

                for (int i = 0; i < Joints.Count; i++)
                {
                    Joints[i].ApplyImpulse();
                }
            }
        }
    }
}
