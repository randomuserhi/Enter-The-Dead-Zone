using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadZoneEngine.Entities
{
    /// <summary>
    /// Describes a joint interaction between objects
    /// </summary>
    public abstract class PhysicalJoint : AbstractWorldEntity, IIteratableUpdatable
    {
        public PhysicalJoint() { }

        public PhysicalJoint(ushort ID) : base(ID) { }

        public virtual void PreUpdate() { }
        public virtual void IteratedUpdate() { }
    }
}
