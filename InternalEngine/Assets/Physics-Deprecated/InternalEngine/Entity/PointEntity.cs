using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using InternalEngine.Physics;

namespace InternalEngine.Entity
{
    //Represents a point entity => acts with a circle collider
    public class PointEntity : EntityObject
    {
        CircleCollider2D Collider;

        public PointEntity() : base()
        {
            Initialize();
        }

        public PointEntity(ulong EntityID) : base(EntityID)
        {
            Initialize();
        }

        public void Initialize()
        {
            InvMass = 1;
            InvInertia = 1;

            Collider = Self.AddComponent<CircleCollider2D>();
            Collider.radius = 0.5f;
        }

        public override byte[] GetPacketBytes()
        {
            //EntityType + Vector3 Position + float InvMass + float InvInertia + float Radius
            //TODO:: might be worth optimizing this to use arrays or something if performance is hit hard
            List<byte> Data = new List<byte>();
            Data.AddRange(BitConverter.GetBytes((int)EntityBehaviourType.PointEntity));
            Data.AddRange(BitConverter.GetBytes(EntityID));
            Data.AddRange(BitConverter.GetBytes(Self.transform.position.x));
            Data.AddRange(BitConverter.GetBytes(Self.transform.position.y));
            Data.AddRange(BitConverter.GetBytes(Rotation));
            Data.AddRange(BitConverter.GetBytes(RB.velocity.x));
            Data.AddRange(BitConverter.GetBytes(RB.velocity.y));
            Data.AddRange(BitConverter.GetBytes(AngularVelocity));
            Data.AddRange(BitConverter.GetBytes(InvMass));
            Data.AddRange(BitConverter.GetBytes(InvInertia));
            Data.AddRange(BitConverter.GetBytes(0.5f));
            return Data.ToArray();
        }
    }
}
