using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using InternalEngine.Entity;

namespace InternalEngine.Colliders
{
    public class Int_CircleCollider : Int_Collider
    {
        public float Radius;
     
        public Int_CircleCollider(EntityObject Entity) : base(ColliderType.Circle, Entity)
        {

        }
    }
}
