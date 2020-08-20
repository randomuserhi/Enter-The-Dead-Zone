using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using InternalEngine.Colliders;

namespace InternalEngine.Entity
{
    public abstract class EntityObject
    {
        public Vector2 Position;
        public float Rotation;
        public Vector2 Velocity;
        public float AngularVelocity;

        public float InvMass;
        public float InvInertia;

        public Int_Collider Collider;

        public void ApplyImpulse(Vector2 Impulse, Vector2 ContactVector)
        {
            Velocity += Impulse * InvMass;
            AngularVelocity += InvInertia * Math2D.Cross(ContactVector, Impulse);
        }

        public virtual void PhysicsUpdate()
        {
            Position += Velocity * IntEngine.DeltaTime; //remove delta time later
        }
    }
}
