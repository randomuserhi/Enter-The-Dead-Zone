using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DeadZoneEngine.Entities.Components
{
    public class BodyChunk : PhysicalObject, IRenderer<SpriteRenderer>
    {
        private AbstractWorld Info;
        public object Context
        {
            get
            {
                if (Info != null)
                    return Info.Context;
                else
                    return null;
            }
            set
            {
                Info.Context = value;
            }
        }
        public DZSettings.EntityType ContextType
        {
            get
            {
                if (Info != null)
                    return Info.Type;
                else
                    return DZSettings.EntityType.Null;
            }
            set
            {
                Info.Type = value;
            }
        }

        public int SortingLayer { get; set; }
        public SpriteRenderer RenderObject { get; set; }
        private GameObject RenderObj;
        public Vector3 SpriteOffset = Vector3.zero;
        public Color RenderColor = new Color(1, 1, 1);
        public virtual void InitializeRenderer()
        {
            RenderObj = new GameObject();
            RenderObj.transform.parent = Self.transform;
            RenderObj.transform.localPosition = Vector3.zero;
            RenderObject = RenderObj.AddComponent<SpriteRenderer>();
            RenderObject.sprite = Resources.Load<Sprite>("Sprites/Circle");
        }
        public virtual void Render()
        {
            RenderObj.transform.localPosition = SpriteOffset;
            RenderObject.color = RenderColor;
        }

        public CircleCollider2D Collider { get; private set; }
        public ContactPoint2D[] Contacts = new ContactPoint2D[10];
        public float Height;

        public BodyChunk()
        {
            Init();
        }
        public BodyChunk(AbstractWorldEntity Parent)
        {
            this.Parent = Parent;
            Init();
        }

        public BodyChunk(ushort ID) : base(ID)
        {
            Init();
        }

        private void Init()
        {
            Info = Self.AddComponent<AbstractWorld>();
            Info.Self = this;
            Collider = Self.AddComponent<CircleCollider2D>();
            Collider.radius = 0.5f;

            InvInertia = 1;
            InvMass = 1;
            Gravity = 0;
        }

        /// <summary>
        /// Update Contacts Array => If no contacts are found, the array will not be updated (past values will persist)
        /// </summary>
        /// <returns>Number of contacts recieved</returns>
        public int GetContacts()
        {
            return Collider.GetContacts(Contacts);
        }

        public override byte[] GetBytes()
        {
            List<byte> Data = new List<byte>();
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

        public override void ParseBytes(DZNetwork.Packet Data)
        {
            Data D = (Data)ParseBytesToSnapshot(Data);
            ParseSnapshot(D);
        }

        public struct Data
        {
            public Vector2 Position;
            public float Rotation;
            public Vector2 Velocity;
            public float AngularVelocity;
            public float InvMass;
            public float InvInertia;
            public float ColliderRadius;
        }

        public override object GetSnapshot()
        {
            Data D = new Data()
            {
                Position = new Vector2(Position.x, Position.y),
                Rotation = Rotation,
                Velocity = new Vector2(Velocity.x, Velocity.y),
                AngularVelocity = AngularVelocity,
                InvMass = InvMass,
                InvInertia = InvInertia,
                ColliderRadius = Collider.radius
            };
            return D;
        }

        public static object ParseBytesToSnapshot(DZNetwork.Packet Data)
        {
            return new Data()
            {
                Position = new Vector2(Data.ReadFloat(), Data.ReadFloat()),
                Rotation = Data.ReadFloat(),
                Velocity = new Vector2(Data.ReadFloat(), Data.ReadFloat()),
                AngularVelocity = Data.ReadFloat(),
                InvMass = Data.ReadFloat(),
                InvInertia = Data.ReadFloat(),
                ColliderRadius = Data.ReadFloat()
            };
        }

        public override void ParseSnapshot(object ObjectData)
        {
            Data Data = (Data)ObjectData;
            Position = Data.Position;
            Rotation = Data.Rotation;
            Velocity = Data.Velocity;
            AngularVelocity = Data.AngularVelocity;
            InvMass = Data.InvMass;
            InvInertia = Data.InvInertia;
            Collider.radius = Data.ColliderRadius;
        }

        public override void Interpolate(object FromData, object ToData, float Time)
        {
            Data From = (Data)FromData;
            Data To = (Data)ToData;
            Position = From.Position + (To.Position - From.Position) * Time;
            Rotation = From.Rotation + (To.Rotation - From.Rotation) * Time;
            Velocity = From.Velocity + (To.Velocity - From.Velocity) * Time;
            AngularVelocity = From.AngularVelocity + (To.AngularVelocity - From.AngularVelocity) * Time;
        }

        public override void Extrapolate(object FromData, float Time)
        {
            Data From = (Data)FromData;
            Position = From.Position + From.Velocity * Time;
            Rotation = From.Rotation + From.AngularVelocity * Time;
        }
    }
}
