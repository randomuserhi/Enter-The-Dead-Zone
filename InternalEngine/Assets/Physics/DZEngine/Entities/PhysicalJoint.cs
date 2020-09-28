using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadZoneEngine.Entities
{
    public abstract class PhysicalJoint : AbstractWorldEntity, IUpdatableAndDeletable
    {
        public PhysicalJoint()
        {
            SetEntityType();
        }

        public PhysicalJoint(ulong ID) : base(ID)
        {
            SetEntityType();
        }

        public bool _FlaggedToDelete;
        bool IUpdatableAndDeletable.FlaggedToDelete { get { return _FlaggedToDelete; } set { _FlaggedToDelete = value; } }
        void IUpdatableAndDeletable.PreUpdate()
        {
            PreStep();
        }

        void IUpdatableAndDeletable.Update() { }
        void IUpdatableAndDeletable.IteratedUpdate()
        {
            ApplyImpulse();
        }
        void IUpdatableAndDeletable.Delete() { }

        protected abstract void SetEntityType();

        public abstract void PreStep();
        public abstract void ApplyImpulse();
    }
}
