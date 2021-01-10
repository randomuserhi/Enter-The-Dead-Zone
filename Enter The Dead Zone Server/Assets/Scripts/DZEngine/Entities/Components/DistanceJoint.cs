using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace DeadZoneEngine.Entities.Components
{
    public struct DistanceJointData
    {
        public PhysicalObject A;
        public PhysicalObject B;
        public float Distance;
        public Vector2 Anchor;

        public DistanceJointData(PhysicalObject A, PhysicalObject B, float Distance, Vector2 Anchor)
        {
            this.A = A;
            this.B = B;
            this.Distance = Distance;
            this.Anchor = Anchor;
        }
    }

    public class DistanceJoint : PhysicalJoint
    {
        PhysicalObject A;
        PhysicalObject B;

        Mat22 M; //Rotation Matrix
        Vector2 LocalAnchorA; //Anchor Points
        Vector2 LocalAnchorB;
        Vector2 RA; //Relative Anchor position on Body A
        Vector2 RB; //Relative Anchor position on Body B
        Vector2 Bias; //Bias in Impulse equation
        Vector2 AccumulatedImpulse; //stores accumulated Impulse

        public float Relaxation = 1f;

        //Strength of pull on object A and B
        public float ARatio = 1;
        public float BRatio = 1;

        float Distance;
        Vector2 Anchor;

        public DistanceJoint()
        {
            
        }
        public DistanceJoint(ulong ID) : base(ID)
        {

        }

        public void Set(object Data)
        {
            DistanceJointData DistanceJointWrapper = (DistanceJointData)Data;
            Distance = DistanceJointWrapper.Distance;
            Anchor = DistanceJointWrapper.Anchor;
            A = DistanceJointWrapper.A;
            B = DistanceJointWrapper.B;

            //Compute Anchor information (rotation matrices)
            Mat22 RotA = new Mat22(0);
            Mat22 RotB = new Mat22(0);
            Mat22 RotAT = RotA.Transpose();
            Mat22 RotBT = RotB.Transpose();

            LocalAnchorA = RotAT * (Anchor);
            LocalAnchorB = RotBT * (Anchor - new Vector2(Distance, 0));

            Relaxation = 1.0f;
        }

        public void SetDistance(float Distance)
        {
            this.Distance = Distance;
            Mat22 RotA = new Mat22(0);
            Mat22 RotB = new Mat22(0);
            Mat22 RotAT = RotA.Transpose();
            Mat22 RotBT = RotB.Transpose();

            LocalAnchorA = RotAT * (Anchor);
            LocalAnchorB = RotBT * (Anchor - new Vector2(Distance, 0));
        }

        public override void PreUpdate()
        {
            //Pre-compute anchors, mass matrix, and bias => http://twvideo01.ubm-us.net/o1/vault/gdc09/slides/04-GDC09_Catto_Erin_Solver.pdf
            if (A.Position == B.Position)
                A.Position += new Vector2(0.01f, 0);

            //Same as using atan2(A.Position - B.Position) however faster as skips atan2 math => this is just getting the current angle between the two objects A and B
            Mat22 RotA = new Mat22((A.Position - B.Position).normalized);
            Mat22 RotB = new Mat22((B.Position - A.Position).normalized);
            /*Mat22 RotA = new Mat22(A.Rotation); // This is for conserving rotation of connected blocks
            Mat22 RotB = new Mat22(B.Rotation);*/

            float AInvMass = A.InvMass * ARatio;
            float BInvMass = B.InvMass * BRatio;
            float AInvInertia = A.InvInertia * ARatio;
            float BInvInertia = B.InvInertia * BRatio;

            RA = RotA * LocalAnchorA;
            RB = RotB * LocalAnchorB;

            Mat22 K1;
            K1.Col1.x = AInvMass + BInvMass; K1.Col2.x = 0.0f;
            K1.Col1.y = 0.0f; K1.Col2.y = AInvMass + BInvMass;

            Mat22 K2;
            K2.Col1.x = AInvInertia * RA.y * RA.y; K2.Col2.x = -AInvInertia * RA.x * RA.y;
            K2.Col1.y = -AInvInertia * RA.x * RA.y; K2.Col2.y = AInvInertia * RA.x * RA.x;

            Mat22 K3;
            K3.Col1.x = BInvInertia * RB.y * RB.y; K3.Col2.x = -BInvInertia * RB.x * RB.y;
            K3.Col1.y = -BInvInertia * RB.x * RB.y; K3.Col2.y = BInvInertia * RB.x * RB.x;

            Mat22 K = K1 + K2 + K3;
            M = K.Invert();

            Vector2 p1 = A.Position + RA;
            Vector2 p2 = B.Position + RB;
            Vector2 dp = p2 - p1;
            Bias = -0.1f * DZEngine.InvDeltaTime * dp;

            //Apply accumulated impulse
            AccumulatedImpulse *= Relaxation;

            A.Velocity -= AInvMass * AccumulatedImpulse;
            A.AngularVelocity -= AInvInertia * Math2D.Cross(RA, AccumulatedImpulse);

            B.Velocity += BInvMass * AccumulatedImpulse;
            B.AngularVelocity += BInvInertia * Math2D.Cross(RB, AccumulatedImpulse);
        }

        public override void IteratedUpdate()
        {
            Vector2 RelativeDeltaVelocity = B.Velocity + Math2D.Cross(B.AngularVelocity, RB) - A.Velocity - Math2D.Cross(A.AngularVelocity, RA);
            Vector2 Impulse = M * (-RelativeDeltaVelocity + Bias);

            A.Velocity -= A.InvMass * ARatio * Impulse;
            A.AngularVelocity -= A.InvInertia * ARatio * Math2D.Cross(RA, Impulse);

            B.Velocity += B.InvMass * BRatio * Impulse;
            B.AngularVelocity += B.InvInertia * BRatio * Math2D.Cross(RB, Impulse);

            AccumulatedImpulse += Impulse;
        }

        public override byte[] GetBytes()
        {
            List<byte> Data = new List<byte>();
            Data.AddRange(BitConverter.GetBytes(Distance));
            Data.AddRange(BitConverter.GetBytes(Anchor.x));
            Data.AddRange(BitConverter.GetBytes(Anchor.y));
            Data.AddRange(BitConverter.GetBytes(ARatio));
            Data.AddRange(BitConverter.GetBytes(BRatio));
            return Data.ToArray();
        }

        public override void ParseBytes(Network.Packet Data, ulong ServerTick)
        {

        }
    }
}
