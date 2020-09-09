using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace InternalEngine.Entity.Interactions
{
    public class DistanceJoint : EntityJoint
    {
        Mat22 M; //Rotation Matrix
        Vector2 LocalAnchorA; //Anchor Points
        Vector2 LocalAnchorB;
        Vector2 RA; //Relative Anchor position on Body A
        Vector2 RB; //Relative Anchor position on Body B
        Vector2 Bias; //Bias in Impulse equation
        Vector2 AccumulatedImpulse; //stores accumulated Impulse

        float Relaxation = 1; //Effectively the damping of the joint
        float Distance;
        Vector2 Anchor;

        public void Set(EntityObject A, EntityObject B, float Distance, Vector2 Anchor)
        {
            this.Distance = Distance;
            this.Anchor = Anchor;
            this.A = A;
            this.B = B;

            //Compute Anchor information (rotation matrices)
            Mat22 RotA = new Mat22(0);
            Mat22 RotB = new Mat22(0);
            Mat22 RotAT = RotA.Transpose();
            Mat22 RotBT = RotB.Transpose();

            LocalAnchorA = RotAT * (Anchor);
            LocalAnchorB = RotBT * (Anchor - new Vector2(0, Distance));

            Relaxation = 1.0f;
        }

        public override void PreStep()
        {
            //Pre-compute anchors, mass matrix, and bias => http://twvideo01.ubm-us.net/o1/vault/gdc09/slides/04-GDC09_Catto_Erin_Solver.pdf
            Mat22 RotA = new Mat22(A.Rotation);
            Mat22 RotB = new Mat22(B.Rotation);

            RA = RotA * LocalAnchorA;
            RB = RotB * LocalAnchorB;

            Mat22 K1;
            K1.Col1.x = A.InvMass + B.InvMass; K1.Col2.x = 0.0f;
            K1.Col1.y = 0.0f; K1.Col2.y = A.InvMass + B.InvMass;

            Mat22 K2;
            K2.Col1.x = A.InvInertia * RA.y * RA.y; K2.Col2.x = -A.InvInertia * RA.x * RA.y;
            K2.Col1.y = -A.InvInertia * RA.x * RA.y; K2.Col2.y = A.InvInertia * RA.x * RA.x;

            Mat22 K3;
            K3.Col1.x = B.InvInertia * RB.y * RB.y; K3.Col2.x = -B.InvInertia * RB.x * RB.y;
            K3.Col1.y = -B.InvInertia * RB.x * RB.y; K3.Col2.y = B.InvInertia * RB.x * RB.x;

            Mat22 K = K1 + K2 + K3;
            M = K.Invert();

            Vector2 p1 = A.Position + RA;
            Vector2 p2 = B.Position + RB;
            Vector2 dp = p2 - p1;
            Bias = -0.1f * IntEngine.InvDeltaTime * dp;


            //Apply accumulated impulse
            AccumulatedImpulse *= Relaxation;

            A.Velocity -= A.InvMass * AccumulatedImpulse;
            A.AngularVelocity -= A.InvInertia * Math2D.Cross(RA, AccumulatedImpulse);

            B.Velocity += B.InvMass * AccumulatedImpulse;
            B.AngularVelocity += B.InvInertia * Math2D.Cross(RB, AccumulatedImpulse);
        }

        public override void ApplyImpulse()
        {
            Vector2 RelativeDeltaVelocity = B.Velocity + Math2D.Cross(B.AngularVelocity, RB) - A.Velocity - Math2D.Cross(A.AngularVelocity, RA);
            Vector2 Impulse = M * (-RelativeDeltaVelocity + Bias);

            A.Velocity -= A.InvMass * Impulse;
            A.AngularVelocity -= A.InvInertia * Math2D.Cross(RA, Impulse);

            B.Velocity += B.InvMass * Impulse;
            B.AngularVelocity += B.InvInertia * Math2D.Cross(RB, Impulse);

            AccumulatedImpulse += Impulse;
        }

        public override byte[] GetPacketBytes()
        {
            //EntityType + ulong EntityID + ulong EntityID + float Distance, Vector2 Anchor
            //TODO:: might be worth optimizing this to use arrays or something if performance is hit hard
            List<byte> Data = new List<byte>();
            Data.AddRange(BitConverter.GetBytes((int)EntityBehaviourType.DistanceJoint));
            Data.AddRange(BitConverter.GetBytes(EntityIDMapReversed[A]));
            Data.AddRange(BitConverter.GetBytes(EntityIDMapReversed[B]));
            Data.AddRange(BitConverter.GetBytes(Distance));
            Data.AddRange(BitConverter.GetBytes(Anchor.x));
            Data.AddRange(BitConverter.GetBytes(Anchor.y));
            return Data.ToArray();
        }

        /*public void DebugPrint()
        {
            Mat22 R1 = new Mat22(A.Rotation);
            Mat22 R2 = new Mat22(B.Rotation);

            Vector2 x1 = A.Position;
            Vector2 p1 = x1 + R1 * LocalAnchorA;

            Vector2 x2 = B.Position;
            Vector2 p2 = x2 + R2 * LocalAnchorB;

            Debug.Log(x1);
            Debug.Log(x2);
            Debug.Log(p1);
            Debug.Log(p2);
            Debug.Log("----------");
        }*/
    }
}
