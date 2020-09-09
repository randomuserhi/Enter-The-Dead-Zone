using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using InternalEngine.Entity;

namespace InternalEngine.Entity.Interactions
{
    public abstract class EntityJoint : EntityBehaviour
    {
        protected EntityObject A;
        protected EntityObject B;

        public EntityJoint() : base()
        {
        }

        public EntityJoint(uint EntityID) : base(EntityID)
        {
        }

        public abstract void PreStep();
        public abstract void ApplyImpulse();
    }
}
