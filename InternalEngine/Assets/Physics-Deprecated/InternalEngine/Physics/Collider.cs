using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using InternalEngine.Entity;
using InternalEngine.Entity.Interactions;

namespace InternalEngine.Physics
{
    public class Resolver
    {
        public static void PreStep(List<EntityJoint> Joints)
        {
            for (int i = 0; i < Joints.Count; i++)
            {
                Joints[i].PreStep();
            }
        }

        public static void ApplyImpulses(List<EntityJoint> Joints, int NumIterations)
        {
            for (int j = 0; j < NumIterations; j++)
            {
                for (int i = 0; i < Joints.Count; i++)
                {
                    Joints[i].ApplyImpulse();
                }
            }
        }
    }
}
