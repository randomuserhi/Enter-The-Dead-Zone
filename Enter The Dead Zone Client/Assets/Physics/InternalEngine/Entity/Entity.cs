using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using InternalEngine.Physics;

namespace InternalEngine.Entity
{
    public abstract class EntityObject
    {
        /*public static string ByteDebug(uint num)
        {
            string ByteString = "";

            byte[] bytes = BitConverter.GetBytes(num);

            int bitPos = 0;
            while (bitPos < 8 * bytes.Length)
            {
                int byteIndex = bitPos / 8;
                int offset = bitPos % 8;
                bool isSet = (bytes[byteIndex] & (1 << offset)) != 0;

                if (isSet)
                    ByteString += "1";
                else
                    ByteString += "0";

                bitPos++;
            }

            return ByteString;
        }*/

        //TODO:: change ulong or uint based on 32 bit or 64 bit system => uint = 32 bit, ulong = 64 bit
        private static List<uint> AssignedID = new List<uint>();
        private static uint SizeOfUIntInBits = Convert.ToUInt32(sizeof(uint) * 8);
        public static uint GetID()
        {
            //Look for free ID
            int Index = AssignedID.FindIndex((Segment) => Segment != uint.MaxValue);
            if (Index == -1)
            {
                AssignedID.Add(1);
                Index = AssignedID.Count - 1;
                return (uint)Index * SizeOfUIntInBits;
            }

            uint Block = AssignedID[Index];
            uint Offset = 0;
            for (int i = 0; Offset < SizeOfUIntInBits; i++, Offset++)
            {
                if ((0u | (1u << i) & Block) == 0u)
                {
                    AssignedID[Index] |= 1u << i;
                    break;
                }
            }
            return (uint)Index * SizeOfUIntInBits + Offset;
        }
        public static void RemoveID(uint EntityID)
        {
            int Index = Convert.ToInt32(EntityID / SizeOfUIntInBits);
            int SubIndex = Convert.ToInt32(EntityID % SizeOfUIntInBits);
            AssignedID[Index] &= ~(1u << SubIndex);
        }
        //TODO:: change to be more memory efficient on client side (if an entity of ID 1000 is added, a long mostly empty array is generated)
        //TODO:: cleanup this code it looks shite
        public static bool CheckEntityID(uint EntityID)
        {
            int Index = Convert.ToInt32(EntityID / SizeOfUIntInBits);
            int SubIndex = Convert.ToInt32(EntityID % SizeOfUIntInBits);
            if (Index >= AssignedID.Count)
            {
                //Expand ID tracking to include missing index
                while (AssignedID.Count <= Index)
                    AssignedID.Add(0u);
                //Set the ID in the list
                AssignedID[Index] |= 1u << SubIndex;
                return false;
            }
            uint Segment = AssignedID[Index];
            bool Exists = ((1u << SubIndex) & Segment) != 0u;
            if (!Exists) //Add the ID if it doesnt exist
                AssignedID[Index] |= 1u << SubIndex;
            return Exists; //returns true if the index exists
        }

        public EntityObject()
        {
            EntityID = GetID();
            Debug.Log(EntityID);

            Initialize();

            //SpriteRenderer for debugging
            Self.AddComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Circle");
        }

        public EntityObject(uint EntityID)
        {
            if (CheckEntityID(EntityID))
                Debug.LogError("EntityID: " + EntityID + " already Exists. This should not ever happen! Too bad!");
            this.EntityID = EntityID;

            Initialize();

            //SpriteRenderer for debugging
            Self.AddComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Circle");
        }

        /*~EntityObject()
        {
            Debug.Log(EntityID + " has been destroyed");
            Destroy();
        }*/

        private void Initialize()
        {
            Self = new GameObject();
            RB = Self.AddComponent<Rigidbody2D>();
        }

        public void Destroy()
        {
            GameObject.Destroy(Self);
            RemoveID(EntityID);
        }

        public readonly uint EntityID;

        public float InvMass { get { return _InvMass; }  set { _InvMass = value; if (value != 0) { RB.mass = 1 / value; RB.isKinematic = false; } else { RB.isKinematic = true; } } }
        private float _InvMass;
        public float InvInertia { get { return _InvInertia; } set { _InvInertia = value; if (value != 0) { RB.inertia = 1 / value; RB.freezeRotation = false; } else { RB.freezeRotation = true; } } }
        private float _InvInertia;

        public GameObject Self;
        public Rigidbody2D RB;

        public Vector2 Position { get { return Self.transform.position; } set { Self.transform.position = value; } }
        public float Rotation { get { return Self.transform.rotation.eulerAngles.z * Mathf.Deg2Rad; } set { Quaternion A = new Quaternion(); A.eulerAngles = new Vector3(0, 0, value * Mathf.Rad2Deg); Self.transform.rotation = A; } }
        public Vector2 Velocity { get { return RB.velocity; } set { RB.velocity = value; } }
        public float AngularVelocity { get { return RB.angularVelocity * Mathf.Deg2Rad; } set { RB.angularVelocity = value * Mathf.Rad2Deg; } }

        public Action<EntityObject> PhysicsUpdate;
    }
}
