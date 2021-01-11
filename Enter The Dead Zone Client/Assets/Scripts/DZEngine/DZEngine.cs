using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;

using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

namespace DeadZoneEngine
{
    public static class DZEngine
    {
        public static float InvDeltaTime = 0;

        public static void Initialize()
        {

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

            if (DZSettings.ActiveRenderers)
            {
                IRenderer Renderer = Entity as IRenderer;
                Renderer?.InitializeRenderer();
            }

            _AbstractWorldEntities.AddIfContainsInterface(Entity);
            _UpdatableObjects.AddIfContainsInterface(Entity);
            _PhysicsUpdatableObjects.AddIfContainsInterface(Entity);
            _IteratableUpdatableObjects.AddIfContainsInterface(Entity);
            _RenderableObjects.AddIfContainsInterface(Entity);
            _ServerSendableObjects.AddIfContainsInterface(Entity);
        }

        #region DZEngine.ManagedList

        private static HashSet<Type> ManagedListTypes = new HashSet<Type>();
        private static Dictionary<(Type, Type), Delegate> GetInvokeCache = new Dictionary<(Type, Type), Delegate>();
        private static Dictionary<Type, (Delegate, Delegate)> GetManagedInvokeCache = new Dictionary<Type, (Delegate, Delegate)>();
        private static void InvokeIfContainsInterface<SearchType, ListType>(Delegate Method, List<ListType> List) where SearchType : class
        {
            for (int i = 0; i < List.Count; i++)
            {
                SearchType Item = List[i] as SearchType;
                if (Item != null)
                {
                    ((Action<SearchType>)Method)(Item);
                }
            }
        }
        private static Action<Delegate, List<InvokeType>> GetInvokeFromType<InvokeType>(Type T)
        {
            var Label = (T, typeof(InvokeType));
            if (GetInvokeCache.ContainsKey(Label))
                return (Action<Delegate, List<InvokeType>>)GetInvokeCache[Label];
            MethodInfo Method = typeof(DZEngine).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(I => I.Name == nameof(DZEngine.InvokeIfContainsInterface));
            Action<Delegate, List<InvokeType>> DelegateAction = (Action<Delegate, List<InvokeType>>)Delegate.CreateDelegate(typeof(Action<Delegate, List<InvokeType>>), Method.MakeGenericMethod(T, typeof(InvokeType)));
            GetInvokeCache.Add(Label, DelegateAction);
            return DelegateAction;
        }
        private static void UpdateManagedLists()
        {
            foreach (Type T in ManagedListTypes)
            {
                Delegate UpdateListMethod = null;
                Delegate ClearListMethod = null;
                if (GetManagedInvokeCache.ContainsKey(T))
                {
                    UpdateListMethod = GetManagedInvokeCache[T].Item1;
                    ClearListMethod = GetManagedInvokeCache[T].Item2;
                }
                else
                {
                    Type ActionGeneric = typeof(Action<>).MakeGenericType(T);
                    Type Generic = typeof(ManagedList<>).MakeGenericType(T);
                    MethodInfo UpdateListMethodInfo = Generic.GetMethod(nameof(ManagedList<object>.UpdateExistingLists));
                    MethodInfo ClearListMethodInfo = Generic.GetMethod(nameof(ManagedList<object>.ClearExistingLists));
                    UpdateListMethod = Delegate.CreateDelegate(ActionGeneric, UpdateListMethodInfo);
                    ClearListMethod = Delegate.CreateDelegate(typeof(Action), ClearListMethodInfo);
                    GetManagedInvokeCache.Add(T, (UpdateListMethod, ClearListMethod));
                }

                ((Action)ClearListMethod)();
                GetInvokeFromType<AbstractWorldEntity>(T)(UpdateListMethod, _AbstractWorldEntities);
                GetInvokeFromType<IPhysicsUpdatable>(T)(UpdateListMethod, _PhysicsUpdatableObjects);
                GetInvokeFromType<IUpdatable>(T)(UpdateListMethod, _UpdatableObjects);
                GetInvokeFromType<IIteratableUpdatable>(T)(UpdateListMethod, _IteratableUpdatableObjects);
                GetInvokeFromType<IRenderer>(T)(UpdateListMethod, _RenderableObjects);
                GetInvokeFromType<IServerSendable>(T)(UpdateListMethod, _ServerSendableObjects);
            }
        }
        public class ManagedList<T> : HashSet<T> where T : class
        {
            public static List<WeakReference> ExistingLists = new List<WeakReference>();

            public ManagedList()
            {
                ExistingLists.Add(new WeakReference(this));
                ManagedListTypes.Add(typeof(T));
            }

            public static void ClearExistingLists()
            {
                ExistingLists.RemoveAll(I =>
                {
                    if (I.IsAlive)
                    {
                        ManagedList<T> L = (ManagedList<T>)I.Target;
                        L.Clear();
                        return false;
                    }
                    return true;
                });
            }

            public static void UpdateExistingLists(T Item)
            {
                for (int i = 0; i < ExistingLists.Count; i++)
                {
                    ManagedList<T> L = (ManagedList<T>)ExistingLists[i].Target;
                    L.Add(Item);
                }
            }
        }

        #endregion

        public static List<object> EntitesToPush = new List<object>();
        private static List<IServerSendable> _ServerSendableObjects = new List<IServerSendable>();
        private static List<AbstractWorldEntity> _AbstractWorldEntities = new List<AbstractWorldEntity>();
        private static List<IPhysicsUpdatable> _PhysicsUpdatableObjects = new List<IPhysicsUpdatable>();
        private static List<IUpdatable> _UpdatableObjects = new List<IUpdatable>();
        private static List<IIteratableUpdatable> _IteratableUpdatableObjects = new List<IIteratableUpdatable>();
        private static List<IRenderer> _RenderableObjects = new List<IRenderer>();

        public static void Destroy(_IInstantiatableDeletable Item)
        {
            Item.FlaggedToDelete = true;
        }
        public static void AddComponent(_IInstantiatableDeletable Component)
        {
            EntitesToPush.Add(Component);
        }

        public static ReadOnlyCollection<IServerSendable> ServerSendableObjects { get { return _ServerSendableObjects.AsReadOnly(); } }
        public static ReadOnlyCollection<AbstractWorldEntity> AbstractWorldEntities { get { return _AbstractWorldEntities.AsReadOnly(); } }
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

            _ServerSendableObjects.RemoveAll(I =>
            {
                return DeleteHandle(I, true);
            });

            _AbstractWorldEntities.RemoveAll(I =>
            {
                return DeleteHandle(I, true);
            });

            _PhysicsUpdatableObjects.RemoveAll(I =>
            {
                return DeleteHandle(I, true);
            });

            _UpdatableObjects.RemoveAll(I =>
            {
                return DeleteHandle(I, true);
            });

            _IteratableUpdatableObjects.RemoveAll(I =>
            {
                return DeleteHandle(I, true);
            });

            _RenderableObjects.RemoveAll(I =>
            {
                return DeleteHandle(I, true);
            });
        }

        private static bool DeleteHandle(_IInstantiatableDeletable DeletableObject, bool ForceDelete = false)
        {
            if (DeletableObject.FlaggedToDelete || ForceDelete)
            {
                if (!DeletableObject.Disposed)
                {
                    DeletableObject.Disposed = true;
                    DeletableObject.Delete();
                }
                return true;
            }
            return false;
        }

        public static byte[] GetBytes(IServerSendable Item)
        {
            //Header Contents => EntityID, FlaggedToDelete, EntityType
            List<byte> Data = new List<byte>();
            Data.AddRange(BitConverter.GetBytes(Item.ID));
            Data.AddRange(BitConverter.GetBytes(Item.FlaggedToDelete));
            Data.AddRange(BitConverter.GetBytes(Item.ServerObjectType));
            //Provide actual data if the item is not about to be deleted
            //otherwise this data is redundant
            if (!Item.FlaggedToDelete)
                Data.AddRange(Item.GetBytes());
            return Data.ToArray();
        }

        public static void FixedUpdate()
        {
            /*Debug.Log("---- PreEntityCounts ----");
            Debug.Log(InstantiatableDeletable.Count);
            Debug.Log(PhysicsUpdatableObjects.Count);
            Debug.Log(UpdatableObjects.Count);
            Debug.Log(IteratableUpdatableObjects.Count);*/

            InvDeltaTime = 1f / Time.deltaTime;

            UpdateManagedLists();

            EntitesToPush.RemoveAll(I =>
            {
                InstantiateAndAddEntity(I);
                return true;
            });

            _AbstractWorldEntities.RemoveAll(I =>
            {
                return DeleteHandle(I);
            });

            _ServerSendableObjects.RemoveAll(I =>
            {
                I.RecentlyUpdated = false;
                return DeleteHandle(I);
            });

            //Isolate the general physics updates from creature body physics -> this is specific for maintaining physic objects inside of creature bodies
            //(the creature body is updated relative to itself without the need to worry about countering general physics (its isolated from general physics))
            _PhysicsUpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    I.IsolateVelocity();
                }
                return DeleteHandle(I);
            });

            _UpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    I.BodyPhysicsUpdate(); //This is specific to creatures mainly to update self-righting bodies or other body animation specific physics
                                           //its seperated and run in a seperate physics operation to prevent self-righting body physics from being counteracted from normal physics (such as gravity).
                                           //In other words this simply isolates the body physics from the standard physics
                }
                return DeleteHandle(I);
            });

            //Check and resolve physics constraints (Joints etc) => Essentially update the isolated physics of just creature bodies
            _IteratableUpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    I.PreUpdate();
                }
                return DeleteHandle(I);
            });
            for (int j = 0; j < DZSettings.NumPhysicsIterations; j++)
            {
                for (int i = 0; i < _IteratableUpdatableObjects.Count; i++)
                {
                    if (_IteratableUpdatableObjects[i].Active)
                        _IteratableUpdatableObjects[i].IteratedUpdate();
                }

                Physics2D.Simulate(1f / 60f / DZSettings.NumPhysicsIterations);
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
            for (int j = 0; j < DZSettings.NumPhysicsIterations; j++)
            {
                for (int i = 0; i < _IteratableUpdatableObjects.Count; i++)
                {
                    if (_IteratableUpdatableObjects[i].Active)
                        _IteratableUpdatableObjects[i].IteratedUpdate();
                }

                Physics2D.Simulate(1f / 60f / DZSettings.NumPhysicsIterations);
            }

            _RenderableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    if (DZSettings.ActiveRenderers)
                        I.Render();
                }
                return DeleteHandle(I);
            });

            /*Debug.Log("---- PostEntityCounts ----");
            Debug.Log(InstantiatableDeletable.Count);
            Debug.Log(PhysicsUpdatableObjects.Count);
            Debug.Log(UpdatableObjects.Count);
            Debug.Log(IteratableUpdatableObjects.Count);*/
        }
    }
}
