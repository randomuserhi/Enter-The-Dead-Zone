using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace DeadZoneEngine.Entities.Components
{
    public class BodyChunk : PhysicalObject
    {
        private CircleCollider2D Collider;

        public BodyChunk()
        {
            Init();
        }

        public BodyChunk(ulong ID) : base(ID)
        {
            Init();
        }

        private void Init()
        {
            Collider = Self.AddComponent<CircleCollider2D>();
            Collider.radius = 0.5f;

            InvInertia = 1;
            InvMass = 1;

            //For debugging
            Self.AddComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Circle");
        }

        protected override void SetEntityType()
        {
            Type = AbstractWorldEntity.EntityType.BodyChunk;
        }

        public override byte[] GetBytes()
        {
            //EntityType + Vector3 Position + float InvMass + float InvInertia + float Radius
            //TODO:: might be worth optimizing this to use arrays or something if performance is hit hard
            List<byte> Data = new List<byte>();
            Data.AddRange(BitConverter.GetBytes((int)Type));
            Data.AddRange(BitConverter.GetBytes(ID));
            Data.AddRange(BitConverter.GetBytes(Position.x));
            Data.AddRange(BitConverter.GetBytes(Position.y));
            Data.AddRange(BitConverter.GetBytes(Rotation));
            Data.AddRange(BitConverter.GetBytes(Velocity.x));
            Data.AddRange(BitConverter.GetBytes(Velocity.y));
            Data.AddRange(BitConverter.GetBytes(AngularVelocity));
            Data.AddRange(BitConverter.GetBytes(InvMass));
            Data.AddRange(BitConverter.GetBytes(InvInertia));
            Data.AddRange(BitConverter.GetBytes(0.5f));
            return Data.ToArray();
        }
    }
}
