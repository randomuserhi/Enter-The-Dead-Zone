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
        public static Dictionary<ushort, _IInstantiatableDeletable> IDToObject = new Dictionary<ushort, _IInstantiatableDeletable>();

        public static ushort StaticID = 0;

        public AbstractWorldEntity Self { get; private set; }
        public ushort Value { get; private set; }
        public EntityID(AbstractWorldEntity Self)
        {
            this.Self = Self;
            AssignNewID();
        }
        public EntityID(AbstractWorldEntity Self, ushort ID)
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

        private void AssignNewID()
        {
            ushort Next = StaticID++;
            if (IDToObject.Count >= ushort.MaxValue - 100)
            {
                Debug.LogError("No more IDs to give!");
                return;
            }
            while (IDToObject.ContainsKey(Next))
            {
                Next = StaticID++;
            }
            Value = Next;
            IDToObject.Add(Value, Self);
        }

        public void ChangeID()
        {
            Remove(this);
            AssignNewID();
        }

        public void ChangeID(ushort New, bool Replace = false)
        {
            if (IDToObject.ContainsKey(New))
            {
                if (IDToObject[New] != Self)
                {
                    if (Replace)
                    {
                        DZEngine.Destroy(IDToObject[New]);
                        IDToObject[New] = Self;
                    }
                    else
                    {
                        Debug.LogError("Could not change ID as an object at that ID already exists...");
                    }
                }
            }
            else
            {
                IDToObject.Add(New, IDToObject[Value]);
                Remove(this);
            }
            Value = New;
        }

        public static void Remove(EntityID ID)
        {
            if (IDToObject.ContainsKey(ID))
            {
                IDToObject.Remove(ID);
            }
            else
                Debug.LogError("EntityID.Remove(EntityID ID) => ID " + ID + " does not exist!");
        }

        public static _IInstantiatableDeletable GetObject(ushort ID)
        {
            if (IDToObject.ContainsKey(ID))
                return IDToObject[ID];
            return null;
        }

        public static bool Exists(ushort ID)
        {
            return IDToObject.ContainsKey(ID);
        }

        public static implicit operator ushort(EntityID ID)
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
        public AbstractWorldEntity(ushort ID)
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
