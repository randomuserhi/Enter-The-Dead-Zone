using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace DeadZoneEngine.Entities
{
    public abstract class PhysicalObject : AbstractWorldEntity, IUpdatableAndDeletable
    {
        protected GameObject Self;
        protected Rigidbody2D RB;

        public PhysicalObject()
        {
            Self = new GameObject();
            RB = Self.AddComponent<Rigidbody2D>();

            SetEntityType();
        }

        public PhysicalObject(ulong ID) : base(ID)
        {
            Self = new GameObject();
            RB = Self.AddComponent<Rigidbody2D>();

            SetEntityType();
        }

        public bool _FlaggedToDelete;
        bool IUpdatableAndDeletable.FlaggedToDelete { get { return _FlaggedToDelete; } set { _FlaggedToDelete = value; } }
        void IUpdatableAndDeletable.PreUpdate() { }
        void IUpdatableAndDeletable.Update() { }
        void IUpdatableAndDeletable.IteratedUpdate() { }
        void IUpdatableAndDeletable.Delete()
        {
            GameObject.Destroy(Self);
        }

        protected abstract void SetEntityType();

        public Vector2 Position { get { return RB.position; } set { RB.position = value; } }
        public Vector2 Velocity { get { return RB.velocity; } set { RB.velocity = value; } }
        public float Rotation { get { return RB.rotation * Mathf.Deg2Rad; } set { RB.rotation = value * Mathf.Rad2Deg; } }
        public float AngularVelocity { get { return RB.angularVelocity * Mathf.Deg2Rad; } set { RB.angularVelocity = value * Mathf.Rad2Deg; } }

        public float _InvMass = 0;
        public float InvMass { get { return _InvMass; } set { _InvMass = value; if (_InvMass == 0) RB.constraints |= RigidbodyConstraints2D.FreezePosition; else { RB.mass = 1 / _InvMass; RB.constraints &= ~RigidbodyConstraints2D.FreezePosition; } } }

        public float _InvInertia = 0;
        public float InvInertia { get { return _InvInertia; } set { _InvInertia = value; if (_InvInertia == 0) RB.constraints |= RigidbodyConstraints2D.FreezeRotation; else { RB.inertia = 1 / _InvInertia; RB.constraints &= ~RigidbodyConstraints2D.FreezeRotation; } } }
    }
}
