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

        private bool _FlaggedToDelete;
        public bool FlaggedToDelete { get { return _FlaggedToDelete; } set { _FlaggedToDelete = value; } }
        public void Instantiate()
        {
            DZEngine.UpdatableDeletableObjects.Add(this);
        }
        public virtual void PreUpdate() { }
        public virtual void Update() { }
        public virtual void IteratedUpdate() { }
        public virtual void Delete() { FlaggedToDelete = true; }
    }
}
