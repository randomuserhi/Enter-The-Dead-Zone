using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadZoneEngine.Entities
{
    public interface IRenderer
    {
    }

    public interface IInstantiatableAndDeletable
    {
        bool FlaggedToDelete { get; set; }

        void Instantiate();

        void Destroy();

        void Delete();
    }

    public interface IUpdatable
    {
        bool Active { get; set; }
        bool FlaggedToDelete { get; set; }
        void Update();
        void BodyPhysicsUpdate();
    }

    public interface IPhysicsUpdatable
    {
        bool Active { get; set; }
        bool FlaggedToDelete { get; set; }
        void IsolateVelocity();
        void RestoreVelocity();
    }

    public interface IIteratableUpdatable
    {
        bool Active { get; set; }
        bool FlaggedToDelete { get; set; }
        void PreUpdate();
        void IteratedUpdate();
    }
}
