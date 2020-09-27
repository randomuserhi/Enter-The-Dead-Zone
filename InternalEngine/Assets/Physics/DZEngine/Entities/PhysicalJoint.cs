using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadZoneEngine.Entities
{
    public abstract class PhysicalJoint : UpdatableAndDeletable
    {
        public PhysicalJoint(AbstractWorldEntity Owner)
        {
            this.Owner = Owner;
            SetEntityType();
        }

        protected abstract void SetEntityType();

        public abstract void PreStep();
        public abstract void ApplyImpulse();
        public abstract byte[] GetBytes();
    }
}
