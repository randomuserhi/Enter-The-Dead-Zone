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

public class Parser
{
    public static void Parse(ref BodyChunk Chunk, Packet P)
    {
        ulong ID = P.ReadULong();
        if (EntityID.Exists(ID))
            Chunk = (BodyChunk)EntityID.IDToObject[ID];
        else
            Chunk = new BodyChunk(ID);
        Chunk.Active = P.ReadBool();
        Chunk.Position = new Vector3(P.ReadFloat(), P.ReadFloat());
        Chunk.Rotation = P.ReadFloat();
        Chunk.Velocity = new Vector3(P.ReadFloat(), P.ReadFloat());
        Chunk.AngularVelocity = P.ReadFloat();
        Chunk.InvMass = P.ReadFloat();
        Chunk.InvInertia = P.ReadFloat();
        P.ReadFloat(); //radius => but didnt implement
        Chunk.Instantiate();
    }

    public static void Parse(ref DistanceJoint Joint, Packet P)
    {
        ulong ID = P.ReadULong();
        if (EntityID.Exists(ID))
            Joint = (DistanceJoint)EntityID.IDToObject[ID];
        else
            Joint = new DistanceJoint(ID);
        Joint.Active = P.ReadBool();
        Joint.Set((PhysicalObject)EntityID.IDToObject[P.ReadULong()], (PhysicalObject)EntityID.IDToObject[P.ReadULong()], P.ReadFloat(), new Vector2(P.ReadFloat(), P.ReadFloat()));
        Joint.ARatio = P.ReadFloat();
        Joint.BRatio = P.ReadFloat();
        Joint.Instantiate();
        //TODO:: somehow need to handle when points used in the joint where not sent with snapshot
    }

    public static void Parse(ref PlayerCreature Creature, Packet P)
    {
        ulong ID = P.ReadULong();
        if (EntityID.Exists(ID))
            Creature = (PlayerCreature)EntityID.IDToObject[ID];
        else
            Creature = new PlayerCreature(ID);
        Creature.Active = P.ReadBool();

        BodyChunk A = null;
        P.ReadInt();
        Parse(ref A, P);

        BodyChunk B = null;
        P.ReadInt();
        Parse(ref B, P);

        DistanceJoint J = null;
        P.ReadInt();
        Parse(ref J, P);

        Creature.Init(A, B, J);
        //TODO:: somehow need to handle when points used in the joint where not sent with snapshot
    }
}

public class Game
{
    public static void FixedUpdate()
    {
    }

    public static void UnWrapSnapshot(PacketWrapper Packet) //TODO:: place specific entity unwraps into their respective classes / abstract methods
    {
        try
        {
            Packet P = Packet.Packet;
            int NumEntities = P.ReadInt();
            for (int i = 0; i < NumEntities; i++)
            {
                AbstractWorldEntity.EntityType ET = (AbstractWorldEntity.EntityType)P.ReadInt();
                switch (ET)
                {
                    case AbstractWorldEntity.EntityType.BodyChunk:
                        {
                            BodyChunk E = null;
                            Parser.Parse(ref E, P);
                        }
                        break;

                    case AbstractWorldEntity.EntityType.DistanceJoint:
                        {
                            DistanceJoint E = null;
                            Parser.Parse(ref E, P);
                        }
                        break;

                    case AbstractWorldEntity.EntityType.PlayerCreature:
                        {
                            PlayerCreature E = null;
                            Parser.Parse(ref E, P);
                        }
                        break;

                    default:
                        Debug.LogError("EntityType : " + ET + " does not exist!");
                        break;
                }
            }
        }
        catch (Exception)
        {

        }
    }
}

