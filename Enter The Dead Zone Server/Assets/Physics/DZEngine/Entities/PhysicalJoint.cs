using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadZoneEngine.Entities
{
    public abstract class PhysicalJoint : AbstractWorldEntity, IIteratableUpdatable
    {
        public PhysicalJoint()
        {
            SetEntityType();
        }

        public PhysicalJoint(ulong ID) : base(ID)
        {
            SetEntityType();
        }

        protected override void _Instantiate()
        {
            DZEngine.IteratableUpdatableObjects.Add(this);
        }
        public virtual void PreUpdate() { }
        public virtual void IteratedUpdate() { }
    }
}
