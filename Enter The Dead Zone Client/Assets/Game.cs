using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using InternalEngine;
using InternalEngine.Entity;
using InternalEngine.Entity.Interactions;
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
            EntityBehaviourType ET = (EntityBehaviourType)P.ReadInt();
            switch (ET)
            {
                case EntityBehaviourType.PointEntity:
                    {
                        ulong EntityID = P.ReadULong();
                        PointEntity E;
                        if (EntityBehaviour.EntityIDMap.ContainsKey(EntityID))
                            E = (PointEntity)EntityBehaviour.EntityIDMap[EntityID];
                        else
                            E = new PointEntity(EntityID);
                        E.Position = new Vector3(P.ReadFloat(), P.ReadFloat());
                        E.RB.velocity = new Vector3(P.ReadFloat(), P.ReadFloat());
                        E.RB.angularVelocity = P.ReadFloat();
                        E.InvMass = P.ReadFloat();
                        E.InvInertia = P.ReadFloat();
                        P.ReadFloat(); //radius => but didnt implement
                        IntEngine.Entities.Add(E);
                    }
                    break;

                case EntityBehaviourType.DistanceJoint:
                    {
                        ulong EntityID = P.ReadULong();
                        DistanceJoint E;
                        if (EntityBehaviour.EntityIDMap.ContainsKey(EntityID))
                            E = (DistanceJoint)EntityBehaviour.EntityIDMap[EntityID];
                        else
                            E = new DistanceJoint(EntityID);
                        E.Set((PointEntity)EntityBehaviour.EntityIDMap[P.ReadULong()], (PointEntity)EntityBehaviour.EntityIDMap[P.ReadULong()], P.ReadFloat(), new Vector2(P.ReadFloat(), P.ReadFloat()));
                        IntEngine.EntityJoints.Add(E);

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

