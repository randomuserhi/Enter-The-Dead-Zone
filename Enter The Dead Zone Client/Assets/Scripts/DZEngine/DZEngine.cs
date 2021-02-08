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
        public static float InvDeltaTime = 0; // 1 / DeltaTime of frame

        /// <summary>
        /// Called on startup
        /// </summary>
        public static void Initialize()
        {

        }

        /// <summary>
        /// Adds a given entity to a DZEngine.ManagedList if its of the correct type
        /// </summary>
        /// <typeparam name="T">ManagedList Type</typeparam>
        /// <param name="List">List to append to</param>
        /// <param name="Entity">Entity to append</param>
        private static void AddIfContainsInterface<T>(this List<T> List, object Entity) where T : class
        {
            T Interface = Entity as T;
            if (Interface != null)
                List.Add(Interface);
        }

        /// <summary>
        /// Calls relevant initialization functions and adds entity to DZEngine
        /// </summary>
        /// <param name="Entity"></param>
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

        private static HashSet<Type> ManagedListTypes = new HashSet<Type>(); //Contains all created managed lists
        private static Dictionary<(Type, Type), Delegate> GetInvokeCache = new Dictionary<(Type, Type), Delegate>(); //Caching delegate functions used for calling relevant add functions
        private static Dictionary<Type, (Delegate, Delegate)> GetManagedInvokeCache = new Dictionary<Type, (Delegate, Delegate)>(); //Caching delegate functions used for clearing and updating managed lists

        /// <summary>
        /// Invokes a given delegate on every item that contains the provided interface
        /// </summary>
        /// <typeparam name="SearchType">Interface the item should contain</typeparam>
        /// <typeparam name="ListType">Type of list being checked</typeparam>
        /// <param name="Method">Delegate to call</param>
        /// <param name="List">List being checked</param>
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

        /// <summary>
        /// Returns the given delegate to add an item to a managed list
        /// </summary>
        /// <typeparam name="InvokeType">Type of list being invoked onto</typeparam>
        /// <param name="T">Type of item</param>
        /// <returns></returns>
        private static Action<Delegate, List<InvokeType>> GetInvokeFromType<InvokeType>(Type T)
        {
            var Label = (T, typeof(InvokeType)); //Key for cache dictionary
            if (GetInvokeCache.ContainsKey(Label)) //Check if it does not already exist
                return (Action<Delegate, List<InvokeType>>)GetInvokeCache[Label]; //If so return cached delegate
            //Find the right method to generate delegate from
            MethodInfo Method = typeof(DZEngine).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(I => I.Name == nameof(DZEngine.InvokeIfContainsInterface));
            //Generate delegate from method
            Action<Delegate, List<InvokeType>> DelegateAction = (Action<Delegate, List<InvokeType>>)Delegate.CreateDelegate(typeof(Action<Delegate, List<InvokeType>>), Method.MakeGenericMethod(T, typeof(InvokeType)));
            //Add delegate to cache
            GetInvokeCache.Add(Label, DelegateAction);
            return DelegateAction;
        }
        private static void UpdateManagedLists()
        {
            foreach (Type T in ManagedListTypes) //Loop through all existing managed lists
            {
                //Find the approapriate update and clear delegates for handling the lists
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

                ((Action)ClearListMethod)(); //Clear the managed list of items
                //Update the managed list with new items
                GetInvokeFromType<AbstractWorldEntity>(T)(UpdateListMethod, _AbstractWorldEntities);
                GetInvokeFromType<IPhysicsUpdatable>(T)(UpdateListMethod, _PhysicsUpdatableObjects);
                GetInvokeFromType<IUpdatable>(T)(UpdateListMethod, _UpdatableObjects);
                GetInvokeFromType<IIteratableUpdatable>(T)(UpdateListMethod, _IteratableUpdatableObjects);
                GetInvokeFromType<IRenderer>(T)(UpdateListMethod, _RenderableObjects);
                GetInvokeFromType<IServerSendable>(T)(UpdateListMethod, _ServerSendableObjects);
            }
        }

        /// <summary>
        /// Defines a list that is automatically updated to contain all entities of type T assigned to DZEngine
        /// This is useful for simply getting a list of specific IRender<>
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        public class ManagedList<T> : HashSet<T> where T : class
        {
            public static List<WeakReference> ExistingLists = new List<WeakReference>();

            public ManagedList()
            {
                //Keep track of all existing managed lists with weak references to allow Garbage Collection (GC)
                ExistingLists.Add(new WeakReference(this));
                //Add Type that is being managed to type list
                ManagedListTypes.Add(typeof(T));
            }

            /// <summary>
            /// Removes lists that have been cleaned up by GC
            /// </summary>
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

            /// <summary>
            /// Adds a new item to all lists of type T
            /// </summary>
            /// <param name="Item"></param>
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

        public static void Instantiate(object Item)
        {
            EntitiesToPush.Add(Item);
        }
        private static List<object> EntitiesToPush = new List<object>(); //List of entities to push to DZEngine

        //Lists of entity interfaces that define DZEngine
        private static List<IServerSendable> _ServerSendableObjects = new List<IServerSendable>(); //All entities that are sendable across the server and client
        private static List<AbstractWorldEntity> _AbstractWorldEntities = new List<AbstractWorldEntity>(); //All abstract world entities
        private static List<IPhysicsUpdatable> _PhysicsUpdatableObjects = new List<IPhysicsUpdatable>(); //All objects that use the seperate (isolated) physics loop
        private static List<IUpdatable> _UpdatableObjects = new List<IUpdatable>(); //All objects that use the standard update loop
        private static List<IIteratableUpdatable> _IteratableUpdatableObjects = new List<IIteratableUpdatable>(); //All objects that use the impulse engine
        private static List<IRenderer> _RenderableObjects = new List<IRenderer>(); //All objects that have a renderer

        /// <summary>
        /// Destroys an entity
        /// </summary>
        /// <param name="Item"></param>
        public static void Destroy(_IInstantiatableDeletable Item)
        {
            Item.FlaggedToDelete = true;
        }

        /// <summary>
        /// Adds an unmanaged entity (not of type AbstractWorldEntity), these are called components
        /// </summary>
        /// <param name="Component"></param>
        public static void AddComponent(_IInstantiatableDeletable Component)
        {
            EntitiesToPush.Add(Component);
        }

        //public getters for lists
        public static ReadOnlyCollection<IServerSendable> ServerSendableObjects { get { return _ServerSendableObjects.AsReadOnly(); } }
        public static ReadOnlyCollection<AbstractWorldEntity> AbstractWorldEntities { get { return _AbstractWorldEntities.AsReadOnly(); } }
        public static ReadOnlyCollection<IPhysicsUpdatable> PhysicsUpdatableObjects { get { return _PhysicsUpdatableObjects.AsReadOnly(); } }
        public static ReadOnlyCollection<IUpdatable> UpdatableObjects { get { return _UpdatableObjects.AsReadOnly(); } }
        public static ReadOnlyCollection<IIteratableUpdatable> IteratableUpdatableObjects { get { return _IteratableUpdatableObjects.AsReadOnly(); } }
        public static ReadOnlyCollection<IRenderer> RenderableObjects { get { return _RenderableObjects.AsReadOnly(); } }

        /// <summary>
        /// Releases and disposes of all entities and their managed resources
        /// </summary>
        public static void ReleaseResources()
        {
            EntitiesToPush.RemoveAll(I =>
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

        /// <summary>
        /// Checks the deletion of an object
        /// </summary>
        /// <param name="DeletableObject">Object to delete</param>
        /// <param name="ForceDelete">Force delete the object regardless</param>
        /// <returns>true if object was disposed, otherwise false</returns>
        private static bool DeleteHandle(_IInstantiatableDeletable DeletableObject, bool ForceDelete = false)
        {
            if (DeletableObject.FlaggedToDelete || ForceDelete) //Check if the object is flagged to delete or is forced to delete
            {
                if (!DeletableObject.Disposed) //If the object has not already been disposed the perform delete
                {
                    DeletableObject.Disposed = true;
                    DeletableObject.Delete();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the bytes of a given entity
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        public static byte[] GetBytes(IServerSendable Item)
        {
            //Header Contents => EntityID, FlaggedToDelete, EntityType
            List<byte> Data = new List<byte>();
            Data.AddRange(BitConverter.GetBytes(Item.ID));
            Data.AddRange(BitConverter.GetBytes(Item.FlaggedToDelete));
            //Provide actual data if the item is not about to be deleted
            //otherwise this data is redundant
            if (!Item.FlaggedToDelete)
            {
                Data.AddRange(BitConverter.GetBytes(Item.ServerObjectType));
                Data.AddRange(Item.GetBytes());
            }
            return Data.ToArray();
        }

        public static void NonPhysicsUpdate()
        {
            UpdateManagedLists(); //Update DZEngine.ManagedLists

            //Push entites into DZEngine
            EntitiesToPush.RemoveAll(I =>
            {
                InstantiateAndAddEntity(I);
                return true;
            });

            //Remove deleted AbstractWorldEntities
            _AbstractWorldEntities.RemoveAll(I =>
            {
                return DeleteHandle(I);
            });

            //Remove deleted server entites
            _ServerSendableObjects.RemoveAll(I =>
            {
                I.RecentlyUpdated = false;
                I.ServerUpdate();
                return DeleteHandle(I);
            });

            //Isolate the general physics updates from creature body physics -> this is specific for maintaining physic objects inside of creature bodies
            //(the creature body is updated relative to itself without the need to worry about countering general physics (its isolated from general physics))
            _PhysicsUpdatableObjects.RemoveAll(I =>
            {
                return DeleteHandle(I);
            });

            _UpdatableObjects.RemoveAll(I =>
            {
                return DeleteHandle(I);
            });

            //Check and resolve physics constraints from impulse engine
            _IteratableUpdatableObjects.RemoveAll(I =>
            {
                return DeleteHandle(I);
            });

            //Render renderable entities
            _RenderableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    if (DZSettings.ActiveRenderers)
                        I.Render();
                }
                return DeleteHandle(I);
            });
        }

        public static void PhysicsUpdate()
        {
            InvDeltaTime = Game.ClientTickRate;

            //Isolate the general physics updates from creature body physics -> this is specific for maintaining physic objects inside of creature bodies
            //(the creature body is updated relative to itself without the need to worry about countering general physics (its isolated from general physics))
            for (int i = 0; i < _PhysicsUpdatableObjects.Count; i++)
            {
                if (_PhysicsUpdatableObjects[i].Active && _PhysicsUpdatableObjects[i].PhysicallyActive)
                    _PhysicsUpdatableObjects[i].IsolateVelocity();
            }

            _UpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    I.BodyPhysicsUpdate(); //This is specific to entites mainly to update self-righting bodies or other body animation specific physics
                                           //its seperated and run in a seperate physics operation to prevent self-righting body physics from being counteracted from normal physics (such as gravity).
                                           //In other words this simply isolates the body physics from the standard physics
                }
                return DeleteHandle(I);
            });

            //Check and resolve physics constraints from impulse engine
            _IteratableUpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active && I.PhysicallyActive)
                {
                    I.PreUpdate();
                }
                return DeleteHandle(I);
            });
            for (int j = 0; j < DZSettings.NumPhysicsIterations; j++)
            {
                for (int i = 0; i < _IteratableUpdatableObjects.Count; i++)
                {
                    if (_IteratableUpdatableObjects[i].Active && _IteratableUpdatableObjects[i].PhysicallyActive)
                        _IteratableUpdatableObjects[i].IteratedUpdate();
                }
            }

            Physics2D.Simulate(Time.fixedDeltaTime / 2);

            //Restore the velocities back to normal, we are no longer considering the entity in an isolated system
            for (int i = 0; i < _PhysicsUpdatableObjects.Count; i++)
            {
                if (_PhysicsUpdatableObjects[i].Active && _PhysicsUpdatableObjects[i].PhysicallyActive)
                    _PhysicsUpdatableObjects[i].RestoreVelocity();
            }

            //Update updatable entities
            for (int i = 0; i < _UpdatableObjects.Count; i++)
            {
                if (_UpdatableObjects[i].Active)
                    _UpdatableObjects[i].Update();
            }

            //Check and resolve physics constraints from impulse engine
            for (int i = 0; i < _IteratableUpdatableObjects.Count; i++)
            {
                if (_IteratableUpdatableObjects[i].Active && _IteratableUpdatableObjects[i].PhysicallyActive)
                    _IteratableUpdatableObjects[i].PreUpdate();
            }
            for (int j = 0; j < DZSettings.NumPhysicsIterations; j++)
            {
                for (int i = 0; i < _IteratableUpdatableObjects.Count; i++)
                {
                    if (_IteratableUpdatableObjects[i].Active && _IteratableUpdatableObjects[i].PhysicallyActive)
                        _IteratableUpdatableObjects[i].IteratedUpdate();
                }
            }

            Physics2D.Simulate(Time.fixedDeltaTime / 2);
        }

        /// <summary>
        /// Called once per frame
        /// </summary>
        public static void FixedUpdate()
        {
            InvDeltaTime = Game.ClientTickRate;

            UpdateManagedLists(); //Update DZEngine.ManagedLists

            //Push entites into DZEngine
            EntitiesToPush.RemoveAll(I =>
            {
                InstantiateAndAddEntity(I);
                return true;
            });

            //Remove deleted AbstractWorldEntities
            _AbstractWorldEntities.RemoveAll(I =>
            {
                return DeleteHandle(I);
            });

            //Remove deleted server entites
            _ServerSendableObjects.RemoveAll(I =>
            {
                I.RecentlyUpdated = false;
                I.ServerUpdate();
                return DeleteHandle(I);
            });

            //Isolate the general physics updates from creature body physics -> this is specific for maintaining physic objects inside of creature bodies
            //(the creature body is updated relative to itself without the need to worry about countering general physics (its isolated from general physics))
            _PhysicsUpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active && I.PhysicallyActive)
                {
                    I.IsolateVelocity();
                }
                return DeleteHandle(I);
            });

            _UpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    I.BodyPhysicsUpdate(); //This is specific to entites mainly to update self-righting bodies or other body animation specific physics
                                           //its seperated and run in a seperate physics operation to prevent self-righting body physics from being counteracted from normal physics (such as gravity).
                                           //In other words this simply isolates the body physics from the standard physics
                }
                return DeleteHandle(I);
            });

            //Check and resolve physics constraints from impulse engine
            _IteratableUpdatableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active && I.PhysicallyActive)
                {
                    I.PreUpdate();
                }
                return DeleteHandle(I);
            });
            for (int j = 0; j < DZSettings.NumPhysicsIterations; j++)
            {
                for (int i = 0; i < _IteratableUpdatableObjects.Count; i++)
                {
                    if (_IteratableUpdatableObjects[i].Active && _IteratableUpdatableObjects[i].PhysicallyActive)
                        _IteratableUpdatableObjects[i].IteratedUpdate();
                }
            }

            Physics2D.Simulate(Time.fixedDeltaTime / 2f);

            //Restore the velocities back to normal, we are no longer considering the entity in an isolated system
            for (int i = 0; i < _PhysicsUpdatableObjects.Count; i++)
            {
                if (_PhysicsUpdatableObjects[i].Active && _PhysicsUpdatableObjects[i].PhysicallyActive)
                    _PhysicsUpdatableObjects[i].RestoreVelocity();
            }

            //Update updatable entities
            for (int i = 0; i < _UpdatableObjects.Count; i++)
            {
                if (_UpdatableObjects[i].Active)
                    _UpdatableObjects[i].Update();
            }

            //Check and resolve physics constraints from impulse engine
            for (int i = 0; i < _IteratableUpdatableObjects.Count; i++)
            {
                if (_IteratableUpdatableObjects[i].Active && _IteratableUpdatableObjects[i].PhysicallyActive)
                    _IteratableUpdatableObjects[i].PreUpdate();
            }
            for (int j = 0; j < DZSettings.NumPhysicsIterations; j++)
            {
                for (int i = 0; i < _IteratableUpdatableObjects.Count; i++)
                {
                    if (_IteratableUpdatableObjects[i].Active && _IteratableUpdatableObjects[i].PhysicallyActive)
                        _IteratableUpdatableObjects[i].IteratedUpdate();
                }
            }

            Physics2D.Simulate(Time.fixedDeltaTime / 2f);

            //Render renderable entities
            _RenderableObjects.RemoveAll(I =>
            {
                if (!I.FlaggedToDelete && I.Active)
                {
                    if (DZSettings.ActiveRenderers)
                        I.Render();
                }
                return DeleteHandle(I);
            });
        }
    }
}
