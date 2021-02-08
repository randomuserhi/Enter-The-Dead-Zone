using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace DeadZoneEngine.Entities
{
    public abstract class PhysicalObject : AbstractWorldEntity, IPhysicsUpdatable
    {
        public GameObject Self;
        public AbstractWorldEntity Parent;
        protected Rigidbody2D RB;

        public PhysicalObject()
        {
            Self = new GameObject();
            RB = Self.AddComponent<Rigidbody2D>();
            RB.angularDrag = 0;
            RB.drag = 0;
            RB.gravityScale = 0;
            RB.sharedMaterial = Resources.Load<PhysicsMaterial2D>("PhysicsMaterial/Zero");
            RB.interpolation = RigidbodyInterpolation2D.None;
            RB.isKinematic = DZSettings.Client;
        }

        public PhysicalObject(ushort ID) : base(ID)
        {
            Self = new GameObject();
            RB = Self.AddComponent<Rigidbody2D>();
            RB.angularDrag = 0;
            RB.drag = 0;
            RB.gravityScale = 0;
            RB.sharedMaterial = Resources.Load<PhysicsMaterial2D>("PhysicsMaterial/Zero");
            RB.interpolation = RigidbodyInterpolation2D.None;
            RB.isKinematic = DZSettings.Client;
        }

        protected override void OnDelete()
        {
            GameObject.Destroy(Self);
        }

        public void PhysicsUpdate(float DeltaTime)
        {
            Self.transform.position += (Vector3)Velocity * DeltaTime;
            Self.transform.eulerAngles += new Vector3(0, 0, AngularVelocity) * DeltaTime;
        }

        public virtual void FixedUpdate() { }
        public virtual void Update() { }
        public virtual void BodyPhysicsUpdate() { }


        private Vector2 PreVelocity;
        public void IsolateVelocity()
        {
            Vector2 Temp = PreVelocity;
            PreVelocity = Velocity;
            Velocity = Temp;
        }

        public void RestoreVelocity()
        {
            Vector2 Temp = Velocity;
            Velocity = PreVelocity;
            PreVelocity = Temp;
        }

        public bool Kinematic { get { return RB.isKinematic; } set { RB.isKinematic = value; } }
        public int CollisionLayer { get { return Self.layer; } set { Self.layer = value; } }
        public Vector2 Position { get { return Self.transform.position; } set { Self.transform.position = value; } }
        public Vector2 Velocity { get { return RB.velocity; } set { RB.velocity = value; } }
        public float Rotation { get { return Self.transform.eulerAngles.z * Mathf.Deg2Rad; } set { Self.transform.eulerAngles = new Vector3(0, value * Mathf.Rad2Deg, 0); } }
        public float AngularVelocity { get { return RB.angularVelocity * Mathf.Deg2Rad; } set { RB.angularVelocity = value * Mathf.Rad2Deg; } }

        public float _InvMass = 0;
        public float InvMass { get { return _InvMass; } set { _InvMass = value; if (_InvMass == 0) RB.constraints |= RigidbodyConstraints2D.FreezePosition; else { RB.mass = 1 / _InvMass; RB.constraints &= ~RigidbodyConstraints2D.FreezePosition; } } }

        public float _InvInertia = 0;
        public float InvInertia { get { return _InvInertia; } set { _InvInertia = value; if (_InvInertia == 0) RB.constraints |= RigidbodyConstraints2D.FreezeRotation; else { RB.inertia = 1 / _InvInertia; RB.constraints &= ~RigidbodyConstraints2D.FreezeRotation; } } }

        public float Gravity = 1;
    }
}
