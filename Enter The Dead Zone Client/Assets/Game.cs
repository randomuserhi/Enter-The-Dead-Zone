using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;
using Network;

public class Game
{
    public static void FixedUpdate()
    {
    }

    public static void UnWrapSnapshot(PacketWrapper Packet) //TODO:: place specific entity unwraps into their respective classes / abstract methods
    {
        Packet P = Packet.Packet;
        int NumEntities = P.ReadInt();
        for (int i = 0; i < NumEntities; i ++)
        {
            AbstractWorldEntity.EntityType ET = (AbstractWorldEntity.EntityType)P.ReadInt();
            switch (ET)
            {
                case AbstractWorldEntity.EntityType.BodyChunk:
                    {
                        ulong ID = P.ReadULong();
                        BodyChunk E;
                        if (EntityID.Exists(ID))
                            E = (BodyChunk)EntityID.IDToObject[ID].Child;
                        else
                        {
                            E = new BodyChunk(new AbstractWorldEntity(ID));
                            DZEngine.UpdatableDeletableObjects.Add(E);
                        }
                        E.Position = new Vector3(P.ReadFloat(), P.ReadFloat());
                        E.Rotation = P.ReadFloat();
                        E.Velocity = new Vector3(P.ReadFloat(), P.ReadFloat());
                        E.AngularVelocity = P.ReadFloat();
                        E.InvMass = P.ReadFloat();
                        E.InvInertia = P.ReadFloat();
                        P.ReadFloat(); //radius => but didnt implement
                    }
                    break;

                case AbstractWorldEntity.EntityType.DistanceJoint:
                    {
                        ulong ID = P.ReadULong();
                        DistanceJoint E;
                        if (EntityID.Exists(ID))
                        {
                            E = (DistanceJoint)EntityID.IDToObject[ID].Child;
                        }
                        else
                        {
                            E = new DistanceJoint(new AbstractWorldEntity(ID));
                            DZEngine.UpdatableDeletableObjects.Add(E);
                        }
                        E.Set((BodyChunk)EntityID.IDToObject[P.ReadULong()].Child, (BodyChunk)EntityID.IDToObject[P.ReadULong()].Child, P.ReadFloat(), new Vector2(P.ReadFloat(), P.ReadFloat()));
                        //TODO:: somehow need to handle when points used in the joint where not sent with snapshot
                    }
                    break;

                default:
                    Debug.LogError("EntityType : " + ET + " does not exist!");
                    break;
            }
        }
    }
}

