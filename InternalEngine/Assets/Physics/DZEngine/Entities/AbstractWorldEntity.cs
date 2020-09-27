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

    public class AbstractWorldEntity
    {
        public EntityID ID;
        public EntityType Type;
        public AbstractWorldEntity()
        {
            ID = new EntityID(this);
        }
        public AbstractWorldEntity(ulong ID)
        {
            this.ID = new EntityID(this, ID);
        }

        public void Destroy()
        {
            EntityID.Remove(ID);
        }

        public enum EntityType
        {
            DistanceJoint,
            BodyChunk
        }
    }
}
