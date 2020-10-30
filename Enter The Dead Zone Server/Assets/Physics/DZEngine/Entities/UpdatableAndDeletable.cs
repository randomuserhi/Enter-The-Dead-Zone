using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadZoneEngine.Entities
{
    public interface IUpdatableAndDeletable
    {
        bool Active { get; set; }
        bool FlaggedToDelete { get; set; }

        void Instantiate();

        void PreUpdate();

        void Update();
        void BodyPhysicsUpdate();

        void IteratedUpdate();

        void Destroy();

        void Delete();
    }
}
