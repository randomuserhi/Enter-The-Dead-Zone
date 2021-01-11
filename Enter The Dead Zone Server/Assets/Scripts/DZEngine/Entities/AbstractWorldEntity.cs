using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace DeadZoneEngine.Entities
{
    /// <summary>
    /// Handles providing unique IDs for all server-bound entities
    /// </summary>
    public class EntityID
    {
        public static Dictionary<ulong, _IInstantiatableDeletable> IDToObject = new Dictionary<ulong, _IInstantiatableDeletable>();

        public static ulong StaticID = 0;

        public ulong Value;
        public EntityID(AbstractWorldEntity Self)
        {
            Value = StaticID++; //TODO:: add case where StaticID == ulong.MaxValue
            IDToObject.Add(Value, Self);
        }
        public EntityID(AbstractWorldEntity Self, ulong ID)
        {
            if (IDToObject.ContainsKey(ID))
            {
                Debug.LogError("EntityID(ulong ID) => ID " + ID + " already exists!");
                return;
            }
            Value = ID;
            IDToObject.Add(Value, Self);
        }

        /// <summary>
        /// Removes an entity from the ID list
        /// </summary>
        /// <param name="ID">ID to remove</param>
        public static void Remove(EntityID ID)
        {
            if (IDToObject.ContainsKey(ID))
                IDToObject.Remove(ID.Value);
            else
                Debug.LogError("EntityID.Remove(EntityID ID) => ID " + ID + " does not exist!");
        }

        /// <summary>
        /// Returns an entity from ID, if no entity is found returns null
        /// </summary>
        public static _IInstantiatableDeletable GetObject(ulong ID)
        {
            if (IDToObject.ContainsKey(ID))
                return IDToObject[ID];
            return null;
        }

        /// <summary>
        /// Checks if an entity of the given ID exists
        /// </summary>
        public static bool Exists(ulong ID)
        {
            return IDToObject.ContainsKey(ID);
        }

        public static implicit operator ulong(EntityID ID)
        {
            return ID.Value;
        }

        //Override equality operations
        public override bool Equals(object Obj)
        {
            return Obj is EntityID && this == (EntityID)Obj;
        }
        public static bool operator ==(EntityID A, EntityID B)
        {
            if (ReferenceEquals(A, null) || ReferenceEquals(B, null))
                return false;
            return A.Value == B.Value;
        }
        public static bool operator !=(EntityID A, EntityID B)
        {
            if (ReferenceEquals(A, null) || ReferenceEquals(B, null))
                return false;
            return A.Value != B.Value;
        }
    }

    /// <summary>
    /// Abstract entity that describes all entities
    /// </summary>
    public abstract class AbstractWorldEntity : _IInstantiatableDeletable
    {
        public EntityID ID { get; set; } = null; //If an entity is not server-bound no ID needs to be provided (null value)
        public AbstractWorldEntity()
        {
            if (this is IServerSendable) //Check if an entity is server-bound and generate an ID for it
                ID = new EntityID(this);
            DZEngine.EntitesToPush.Add(this); //Add entity to DZEngine
        }
        public AbstractWorldEntity(ulong ID) //Creates an entity with the given ID
        {
            if (this is IServerSendable) //Check if an entity is server-bound and generate an ID for it
                this.ID = new EntityID(this, ID);
            DZEngine.EntitesToPush.Add(this); //Add entity to DZEngine
        }

        public bool Active { get; set; } = true;
        public bool FlaggedToDelete { get; set; } = false;
        public bool Disposed { get; set; } = false;

        /// <summary>
        /// Called before constructor to initialize base values
        /// </summary>
        public virtual void Instantiate() { }

        /// <summary>
        /// Is used when values need to be initialized after constructor
        /// </summary>
        /// <returns>Self</returns>
        public object Create() 
        {
            OnCreate();
            return this;
        }
        /// <summary>
        /// Called on Create()
        /// </summary>
        protected virtual void OnCreate() { }

        /// <summary>
        /// Primes object for deletion
        /// </summary>
        public void Delete()
        {
            FlaggedToDelete = true;
            if (ID != null)
                EntityID.Remove(ID);
            OnDelete();
        }
        /// <summary>
        /// Is called on Delete()
        /// </summary>
        protected virtual void OnDelete() { }

        /// <summary>
        /// Returns the bytes representing an entity
        /// </summary>
        /// <returns></returns>
        public abstract byte[] GetBytes();

        /// <summary>
        /// Parse bytes from a packet into this entity
        /// </summary>
        /// <param name="Data">Packet Data</param>
        /// <param name="ServerTick">Tick timing for when packet was sent from server</param>
        public abstract void ParseBytes(Network.Packet Data, ulong ServerTick);
    }
}
