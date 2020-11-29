using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace DeadZoneEngine.Entities
{
    public class EntityID
    {
        public static Dictionary<ulong, AbstractWorldEntity> IDToObject = new Dictionary<ulong, AbstractWorldEntity>();

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

        public static void Remove(EntityID ID)
        {
            if (IDToObject.ContainsKey(ID))
                IDToObject.Remove(ID.Value);
            else
                Debug.LogError("EntityID.Remove(EntityID ID) => ID " + ID + " does not exist!");
        }

        public static bool Exists(ulong ID)
        {
            return IDToObject.ContainsKey(ID);
        }

        public static implicit operator ulong(EntityID ID)
        {
            return ID.Value;
        }

        public override bool Equals(object Obj)
        {
            return Obj is EntityID && this == (EntityID)Obj;
        }
        public static bool operator ==(EntityID A, EntityID B)
        {
            return A.Value == B.Value;
        }
        public static bool operator !=(EntityID A, EntityID B)
        {
            return A.Value != B.Value;
        }
    }

    public abstract class AbstractWorldEntity : IInstantiatableAndDeletable
    {
        public EntityID ID;
        public EntityType Type;
        public AbstractWorldEntity()
        {
            ID = new EntityID(this);
            SetEntityType();
        }
        public AbstractWorldEntity(ulong ID)
        {
            this.ID = new EntityID(this, ID);
            SetEntityType();
        }

        public virtual void Set(object Data) { } //Used by some entities for initializing with given data, its used in cases where the client receives data and needs to set the data for the given object
                                                        // #TODO:: maybe implement a giant struct which contains 
                                                        //data for every object much like whats described in Randy's code video for revamping ECS

        protected abstract void SetEntityType();

        public abstract byte[] GetBytes();

        public void Destroy()
        {
            if (FlaggedToDelete)
                EntityID.Remove(ID);
            else
                Debug.LogWarning("Call Delete() rather than Destroy()!");
            FlaggedToDelete = true;
        }

        private bool _Active = true;
        public bool Active { get { return _Active; } set { _Active = value; } }
        private bool _FlaggedToDelete;
        public bool FlaggedToDelete { get { return _FlaggedToDelete; } set { _FlaggedToDelete = value; } }

        public void Instantiate()
        {
            DZEngine.InstantiatableDeletable.Add(this);
            _Instantiate();
        }
        protected virtual void _Instantiate() { }

        public void Delete()
        {
            FlaggedToDelete = true;
            _Delete();
        }
        protected virtual void _Delete() { }

        public enum EntityType
        {
            DistanceJoint,
            BodyChunk,
            PlayerCreature,
            Tilemap
        }
    }
}
