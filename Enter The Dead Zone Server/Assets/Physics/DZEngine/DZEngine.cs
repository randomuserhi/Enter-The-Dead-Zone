using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

namespace DeadZoneEngine
{
    public static class DZEngine
    {
        public static float InvDeltaTime = 0;
        public static int NumPhysicsIterations = 10;
        public static bool ActiveRenderers = false;

        //Test
        static PlayerCreature P;

        public static void Initialize()
        {
            P = new PlayerCreature();

            Tilemap T1 = new Tilemap(32, new Vector2Int(4, 2));
            Tilemap T2 = new Tilemap(32, new Vector2Int(4, 2));
        }

        private static void AddIfContainsInterface<T>(this List<T> List, object Entity) where T : class
        {
            T Interface = Entity as T;
            if (Interface != null)
                List.Add(Interface);
        }
        private static void InstantiateAndAddEntity(object Entity)
        {
            _IInstantiatableDeletable Instantiatable = Entity as _IInstantiatableDeletable;
            Instantiatable?.Instantiate();

            if (ActiveRenderers)
            {
                IRenderer Renderer = Entity as IRenderer;
                Renderer?.InitializeRenderer();
            }

            _AbstractWorldEntities.AddIfContainsInterface(Entity);
            _UpdatableObjects.AddIfContainsInterface(Entity);
            _PhysicsUpdatableObjects.AddIfContainsInterface(Entity);
            _IteratableUpdatableObjects.AddIfContainsInterface(Entity);
            _RenderableObjects.AddIfContainsInterface(Entity);
        }

        public static List<object> EntitesToPush = new List<object>();
        private static List<AbstractWorldEntity> _AbstractWorldEntities = new List<AbstractWorldEntity>();
        private static List<IPhysicsUpdatable> _PhysicsUpdatableObjects = new List<IPhysicsUpdatable>();
        private static List<IUpdatable> _UpdatableObjects = new List<IUpdatable>();
        private static List<IIteratableUpdatable> _IteratableUpdatableObjects = new List<IIteratableUpdatable>();
        private static List<IRenderer> _RenderableObjects = new List<IRenderer>();

        public static ReadOnlyCollection<AbstractWorldEntity> AbstractWorldEntites { get { return _AbstractWorldEntities.AsReadOnly(); } }
        public static ReadOnlyCollection<IPhysicsUpdatable> PhysicsUpdatableObjects { get { return _PhysicsUpdatableObjects.AsReadOnly(); } }
        public static ReadOnlyCollection<IUpdatable> UpdatableObjects { get { return _UpdatableObjects.AsReadOnly(); } }
        public static ReadOnlyCollection<IIteratableUpdatable> IteratableUpdatableObjects { get { return _IteratableUpdatableObjects.AsReadOnly(); } }
        public static ReadOnlyCollection<IRenderer> RenderableObjects { get { return _RenderableObjects.AsReadOnly(); } }

        public static void ReleaseResources()
        {
            EntitesToPush.RemoveAll(I =>
            {
                InstantiateAndAddEntity(I);
                return true;
            });

            _AbstractWorldEntities.RemoveAll(I =>
            {
                I.Delete();
                return true;
            });

            _PhysicsUpdatableObjects.Clear();
            _UpdatableObjects.Clear();
            _IteratableUpdatableObjects.Clear();
            _RenderableObjects.Clear();
        }

        public static void FixedUpdate()
        {
            /*Debug.Log("---- PreEntityCounts ----");
            Debug.Log(InstantiatableDeletable.Count);
            Debug.Log(PhysicsUpdatableObjects.Count);
            Debug.Log(UpdatableObjects.Count);
            Debug.Log(IteratableUpdatableObjects.Count);*/

            InvDeltaTime = 1f / Time.deltaTime;

            EntitesToPush.RemoveAll(I =>
            {
                InstantiateAndAddEntity(I);
                return true;
            });

            _AbstractWorldEntities.RemoveAll(I => 
            {
                if (I.FlaggedToDelete)
                {
                    I.Delete();
                }
                return I.FlaggedToDelete;
            });

            //Isolate the general physics updates from creature body physics -> this is specific for maintaining physic objects inside of creature bodies
            //(the creature body is updated relative to itself without the need to worry about countering general physics (its isolated from general physics))
            _PhysicsUpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    I.IsolateVelocity();
                }
                return I.FlaggedToDelete;
            });

            _UpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    I.BodyPhysicsUpdate(); //This is specific to creatures mainly to update self-righting bodies or other body animation specific physics
                                           //its seperated and run in a seperate physics operation to prevent self-righting body physics from being counteracted from normal physics (such as gravity).
                                           //In other words this simply isolates the body physics from the standard physics
                }
                return I.FlaggedToDelete;
            });

            //Check and resolve physics constraints (Joints etc) => Essentially update the isolated physics of just creature bodies
            _IteratableUpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    I.PreUpdate();
                }
                return I.FlaggedToDelete;
            });
            for (int j = 0; j < NumPhysicsIterations; j++)
            {
                for (int i = 0; i < _IteratableUpdatableObjects.Count; i++)
                {
                    if (_IteratableUpdatableObjects[i].Active)
                        _IteratableUpdatableObjects[i].IteratedUpdate();
                }

                Physics2D.Simulate(1f / 60f / NumPhysicsIterations);
            }

            //Restore the velocities back to normal, we are no longer considering the creature body in an isolated system
            for (int i = 0; i < _PhysicsUpdatableObjects.Count; i++)
            {
                if (_PhysicsUpdatableObjects[i].Active)
                    _PhysicsUpdatableObjects[i].RestoreVelocity();
            }

            for (int i = 0; i < _UpdatableObjects.Count; i++)
            {
                if (_UpdatableObjects[i].Active)
                    _UpdatableObjects[i].Update(); 
            }

            //Check and resolve physics constraints (Joints etc) => Essentially update the general physics of all bodies
            for (int i = 0; i < _IteratableUpdatableObjects.Count; i++)
            {
                if (_IteratableUpdatableObjects[i].Active)
                    _IteratableUpdatableObjects[i].PreUpdate();
            }
            for (int j = 0; j < NumPhysicsIterations; j++)
            {
                for (int i = 0; i < _IteratableUpdatableObjects.Count; i++)
                {
                    if (_IteratableUpdatableObjects[i].Active)
                        _IteratableUpdatableObjects[i].IteratedUpdate();
                }

                Physics2D.Simulate(1f / 60f / NumPhysicsIterations);
            }

            _RenderableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    if (ActiveRenderers)
                        I.Render();
                }
                return I.FlaggedToDelete;
            });

            /*Debug.Log("---- PostEntityCounts ----");
            Debug.Log(InstantiatableDeletable.Count);
            Debug.Log(PhysicsUpdatableObjects.Count);
            Debug.Log(UpdatableObjects.Count);
            Debug.Log(IteratableUpdatableObjects.Count);*/
        }
    }
}
