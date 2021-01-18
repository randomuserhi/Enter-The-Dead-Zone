using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadZoneEngine.Entities
{
    /// <summary>
    /// Describes an object that can be instantiated and deleted from DZEngine
    /// </summary>
    public interface _IInstantiatableDeletable
    {
        bool Active { get; set; } //Is the object active
        bool FlaggedToDelete { get; set; } //Is the object to be deleted next frame
        bool Disposed { get; set; } //Has the object already been disposed
        void Delete(); //Delete the object
        void Instantiate(); //Instantiate the object (called before constructor)
        object Create(); //Create the object (manually called after constructor)
    }

    /// <summary>
    /// Describes an object that is sent between server and client
    /// </summary>
    public interface IServerSendable : _IInstantiatableDeletable
    {
        EntityID ID { get; set; } //ID of entity
        int ServerObjectType { get; set; } //Type of entity
        bool RecentlyUpdated { get; set; } //Has this entity recently been acknowledged by server
        byte[] GetBytes(); //Convert the entity into byte data
        void ParseBytes(DZNetwork.Packet Data, ulong ServerTick); //Parse byte data into this entity
    }

    /// <summary>
    /// Describes an object that can be rendered
    /// </summary>
    public interface IRenderer : _IInstantiatableDeletable
    {
        int SortingLayer { get; set; } //Determines the order of render (if this value is less than another renders value, its rendered behind that render)

        void InitializeRenderer(); //Initialize renders

        void Render(); //Called once per frame for rendering
    }

    /// <summary>
    /// Describes an object that has a render component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRenderer<T> : _IInstantiatableDeletable, IRenderer where T : class
    {
        T RenderObject { get; set; } //Render component
    }

    /// <summary>
    /// Describes an object that uses the standard update loop
    /// </summary>
    public interface IUpdatable : _IInstantiatableDeletable
    {
        void Update(); //Called once per frame
        void BodyPhysicsUpdate(); //Called once per frame during isolated physics loop 
    }

    /// <summary>
    /// Describes an object that uses the seperate (isolated) physics update loop
    /// </summary>
    public interface IPhysicsUpdatable : _IInstantiatableDeletable
    {
        void IsolateVelocity();
        void RestoreVelocity();
    }

    /// <summary>
    /// Describes an object that uses the Impulse Engine update loop
    /// </summary>
    public interface IIteratableUpdatable : _IInstantiatableDeletable
    {
        void PreUpdate(); //Called once per frame prior iterated update
        void IteratedUpdate(); //Called multiple times per frame
    }
}
