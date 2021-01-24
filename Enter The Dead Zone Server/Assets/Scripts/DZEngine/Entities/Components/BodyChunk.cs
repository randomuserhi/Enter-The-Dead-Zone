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
        public int SortingLayer { get; set; }
        public SpriteRenderer RenderObject { get; set; }
        public virtual void InitializeRenderer()
        {
            RenderObject = Self.AddComponent<SpriteRenderer>();
            RenderObject.sprite = Resources.Load<Sprite>("Sprites/Circle");
        }
        public virtual void Render() { }

        public CircleCollider2D Collider { get; private set; }
        public ContactPoint2D[] Contacts = new ContactPoint2D[10]; //Contact buffer for getting collision contacts
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
            Collider = Self.AddComponent<CircleCollider2D>();
            Collider.radius = 0.5f;

            InvInertia = 1;
            InvMass = 1;
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

        public override void ParseBytes(DZNetwork.Packet Data, ulong ServerTick)
        {
            throw new NotImplementedException();
        }
    }
}
