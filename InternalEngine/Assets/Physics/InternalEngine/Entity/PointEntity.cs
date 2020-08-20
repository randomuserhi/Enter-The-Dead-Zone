using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using InternalEngine.Colliders;

namespace InternalEngine.Entity
{
    //Represents a point entity => acts with a circle collider
    public class PointEntity : EntityObject
    {
        public PointEntity()
        {
            Collider = new Int_CircleCollider(this);
            ((Int_CircleCollider)Collider).Radius = 0.5f;

            InvMass = 1;
            InvInertia = 1;
        }
    }
}
