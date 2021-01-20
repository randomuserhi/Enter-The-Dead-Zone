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
        //private static object IDDictionaryLock = new object();
        public static Dictionary<ulong, _IInstantiatableDeletable> IDToObject = new Dictionary<ulong, _IInstantiatableDeletable>();

        public static ulong StaticID = 0;

        public AbstractWorldEntity Self;
        public ulong Value;
        public EntityID(AbstractWorldEntity Self)
        {
            this.Self = Self;
            Value = StaticID++; //TODO:: add case where StaticID == ulong.MaxValue
            IDToObject.Add(Value, Self);
        }
        public EntityID(AbstractWorldEntity Self, ulong ID)
        {
            this.Self = Self;
            if (IDToObject.ContainsKey(ID))
            {
                Debug.LogError("EntityID(ulong ID) => ID " + ID + " already exists!");
                return;
            }
            Value = ID;
            IDToObject.Add(Value, Self);
        }

        public void ChangeID()
        {
            Remove(this);
            Value = StaticID++;
            IDToObject.Add(Value, Self);
        }

        public void ChangeID(ulong New, bool Replace = false)
        {
            if (IDToObject.ContainsKey(New))
            {
                if (Replace && IDToObject[New] != Self)
                {
                    DZEngine.Destroy(IDToObject[New]);
                    IDToObject[New] = Self;
                }
                else
                {
                    Debug.LogError("Could not change ID as an object at that ID already exists...");
                }
            }
            else
            {
                IDToObject.Add(New, IDToObject[Value]);
                Remove(this);
            }
        }

        public static void Remove(EntityID ID)
        {
            if (IDToObject.ContainsKey(ID))
                IDToObject.Remove(ID.Value);
            else
                Debug.LogError("EntityID.Remove(EntityID ID) => ID " + ID + " does not exist!");
        }

        public static _IInstantiatableDeletable GetObject(ulong ID)
        {
            if (IDToObject.ContainsKey(ID))
                return IDToObject[ID];
            return null;
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
            if (ReferenceEquals(A, null) && ReferenceEquals(B, null))
                return true;
            else if (ReferenceEquals(A, null) || ReferenceEquals(B, null))
                return false;
            return A.Value == B.Value;
        }
        public static bool operator !=(EntityID A, EntityID B)
        {
            if (ReferenceEquals(A, null) && ReferenceEquals(B, null))
                return false;
            else if (ReferenceEquals(A, null) || ReferenceEquals(B, null))
                return true;
            return A.Value != B.Value;
        }
    }

    public abstract class AbstractWorldEntity : _IInstantiatableDeletable
    {
        public EntityID ID { get; set; } = null;
        public AbstractWorldEntity()
        {
            if (this is IServerSendable)
                ID = new EntityID(this);
            DZEngine.Instantiate(this);
        }
        public AbstractWorldEntity(ulong ID)
        {
            if (this is IServerSendable)
                this.ID = new EntityID(this, ID);
            DZEngine.Instantiate(this);
        }

        public bool Active { get; set; } = true;
        public bool FlaggedToDelete { get; set; } = false;
        public bool Disposed { get; set; } = false;

        public virtual void Instantiate() { }
        public object Create()
        {
            OnCreate();
            return this;
        }
        protected virtual void OnCreate() { }

        public void Delete()
        {
            FlaggedToDelete = true;
            if (ID != null)
                EntityID.Remove(ID);
            OnDelete();
        }
        protected virtual void OnDelete() { }

        public abstract byte[] GetBytes();
        public abstract void ParseBytes(DZNetwork.Packet Data, ulong ServerTick);
    }
}
