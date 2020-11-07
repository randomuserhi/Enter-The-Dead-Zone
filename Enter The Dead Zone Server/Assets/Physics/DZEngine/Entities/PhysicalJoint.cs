using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadZoneEngine.Entities
{
    public abstract class PhysicalJoint : AbstractWorldEntity, IIteratableUpdatable
    {
        public PhysicalJoint() { }

        public PhysicalJoint(ulong ID) : base(ID) { }

        protected override void _Instantiate()
        {
            DZEngine.IteratableUpdatableObjects.Add(this);
        }
        public virtual void PreUpdate() { }
        public virtual void IteratedUpdate() { }
    }
}
