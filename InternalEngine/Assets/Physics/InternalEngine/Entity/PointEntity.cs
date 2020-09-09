using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using InternalEngine.Physics;

namespace InternalEngine.Entity
{
    //Represents a point entity => acts with a circle collider
    public class PointEntity : EntityObject
    {
        CircleCollider2D Collider;

        public PointEntity() : base()
        {
            Initialize();
        }

        public PointEntity(uint EntityID) : base(EntityID)
        {
            Initialize();
        }

        public void Initialize()
        {
            InvMass = 1;
            InvInertia = 1;

            Collider = Self.AddComponent<CircleCollider2D>();
            Collider.radius = 0.5f;
        }
    }
}
