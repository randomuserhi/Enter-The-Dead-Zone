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
        public AbstractWorldEntity Parent;

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

        private bool _FlaggedToDelete;
        public bool FlaggedToDelete { get { return _FlaggedToDelete; } set { _FlaggedToDelete = value; } }
        public void Instantiate()
        {
            DZEngine.UpdatableDeletableObjects.Add(this);
        }
        public virtual void PreUpdate() { }
        public virtual void Update() { }
        public virtual void IteratedUpdate() { }
        public void Delete()
        {
            FlaggedToDelete = true;
            GameObject.Destroy(Self);
        }

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
