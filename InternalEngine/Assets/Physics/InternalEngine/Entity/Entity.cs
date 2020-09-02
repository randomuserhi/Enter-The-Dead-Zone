using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using InternalEngine.Physics;

namespace InternalEngine.Entity
{
    public abstract class EntityObject
    {
        public EntityObject()
        {
            Self = new GameObject();
            RB = Self.AddComponent<Rigidbody2D>();

            //SpriteRenderer for debugging
            Self.AddComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Circle");
        }

        public float InvMass { get { return _InvMass; }  set { _InvMass = value; if (value != 0) { RB.mass = 1 / value; RB.isKinematic = false; } else { RB.isKinematic = true; } } }
        private float _InvMass;
        public float InvInertia { get { return _InvInertia; } set { _InvInertia = value; if (value != 0) { RB.inertia = 1 / value; RB.freezeRotation = false; } else { RB.freezeRotation = true; } } }
        private float _InvInertia;

        public GameObject Self;
        public Rigidbody2D RB;

        public Vector2 Position { get { return Self.transform.position; } set { Self.transform.position = value; } }
        public float Rotation { get { return Self.transform.rotation.eulerAngles.z * Mathf.Deg2Rad; } set { Quaternion A = new Quaternion(); A.eulerAngles = new Vector3(0, 0, value * Mathf.Rad2Deg); Self.transform.rotation = A; } }
        public Vector2 Velocity { get { return RB.velocity; } set { RB.velocity = value; } }
        public float AngularVelocity { get { return RB.angularVelocity * Mathf.Deg2Rad; } set { RB.angularVelocity = value * Mathf.Rad2Deg; } }

        public Action<EntityObject> PhysicsUpdate;
    }
}
