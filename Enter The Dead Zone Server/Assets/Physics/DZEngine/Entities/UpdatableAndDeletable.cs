using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadZoneEngine.Entities
{
    public interface IUpdatableAndDeletable
    {
        bool FlaggedToDelete { get; set; }

        void PreUpdate();

        void Update();

        void IteratedUpdate();

        void Destroy();

        void Delete();
    }
}
