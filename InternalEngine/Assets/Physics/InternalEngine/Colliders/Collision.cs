using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using InternalEngine.Entity;

namespace InternalEngine.Colliders
{
    public class Int_Collision
    {
        public static Func<EntityObject, EntityObject, int ,int, Manifold>[][] CollisionSolveMatrix = new Func<EntityObject, EntityObject, int, int, Manifold>[][]
        {
            new Func<EntityObject, EntityObject, int, int, Manifold>[] {CircleCircle, CircleBox},
            new Func<EntityObject, EntityObject, int, int, Manifold>[] {CircleBox, CircleCircle}
        };

        public static void CalculateManifolds(List<Manifold> Manifolds, List<EntityObject> Entities)
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                for (int j = i + 1; j < Entities.Count; j++)
                {
                    Manifold M = CollisionSolveMatrix[(int)Entities[i].Collider.Type][(int)Entities[j].Collider.Type](Entities[i], Entities[j], i, j);

                    int ExistingManifold = Manifolds.FindIndex((m) => { return (m.A == M.A && m.B == M.B);  });
                    if (M.ContactPoints != null && M.ContactPoints.Length > 0)
                    {
                        if (ExistingManifold == -1)
                            Manifolds.Add(M);
                        else
                        {
                            Manifolds[ExistingManifold].Update(M.ContactPoints);
                        }
                    }
                    else if (ExistingManifold != -1)
                    {
                        Manifolds.RemoveAt(ExistingManifold);
                    }
                }
            }
        }

        public static Manifold CircleCircle(EntityObject _A, EntityObject _B, int i, int j)
        {
            Manifold M = new Manifold(ManifoldType.CircleCircle, _A, _B, i, j);

            EntityObject A = M.A;
            EntityObject B = M.B;

            Int_CircleCollider ACollider = (Int_CircleCollider)A.Collider;
            Int_CircleCollider BCollider = (Int_CircleCollider)B.Collider;

            Vector2 Normal = B.Position - A.Position;

            float TotalRadius = ACollider.Radius + BCollider.Radius;

            if (Normal.sqrMagnitude >= TotalRadius * TotalRadius)
                return M; //no collision

            float Distance = Normal.magnitude;
            M.ContactPoints = new Int_ContactPoint[1];
            M.ContactPoints[0] = new Int_ContactPoint();
            if (Distance == 0)
            {
                M.ContactPoints[0].Seperation = -ACollider.Radius;
                M.ContactPoints[0].Normal = new Vector2(0, 1);
                M.ContactPoints[0].Position = A.Position;
            }
            else
            {
                M.ContactPoints[0].Seperation = Distance - TotalRadius;
                M.ContactPoints[0].Normal = Normal / Distance;
                M.ContactPoints[0].Position = M.ContactPoints[0].Normal * ACollider.Radius + A.Position;
            }

            return M;
        }

        public static Manifold CircleBox(EntityObject _A, EntityObject _B, int i, int j)
        {
            Manifold M = new Manifold(ManifoldType.CircleBox, _A, _B, i, j);

            EntityObject A = M.A;
            EntityObject B = M.B;

            Int_CircleCollider ACollider = A.Collider.Type == ColliderType.Circle ? (Int_CircleCollider)A.Collider : (Int_CircleCollider)B.Collider;
            //Inverse of above here

            return M;
        }
    }
}
