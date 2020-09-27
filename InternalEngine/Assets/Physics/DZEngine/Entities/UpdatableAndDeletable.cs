using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadZoneEngine.Entities
{
    public class UpdatableAndDeletable
    {
        public bool FlaggedForDeletion = false;
        public AbstractWorldEntity Owner;

        public virtual void PreUpdate()
        {

        }

        public virtual void Update()
        {

        }

        public virtual void IteratedUpdate()
        {

        }

        public void Destroy()
        {
            FlaggedForDeletion = true;
        }

        public virtual void Delete()
        {

        }
    }
}
