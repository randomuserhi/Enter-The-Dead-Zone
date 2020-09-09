using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using InternalEngine.Entity;

namespace InternalEngine.Entity.Interactions
{
    public abstract class EntityJoint
    {
        protected EntityObject A;
        protected EntityObject B;

        public abstract void PreStep();
        public abstract void ApplyImpulse();
    }
}
