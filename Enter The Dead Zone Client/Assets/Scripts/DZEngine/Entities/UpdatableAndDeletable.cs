﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadZoneEngine.Entities
{
    public interface _IInstantiatableDeletable
    {
        bool Active { get; set; }
        bool FlaggedToDelete { get; set; }
        bool Disposed { get; set; }
        void Delete();
        void Instantiate();
        object Create();
    }

    public interface IServerSendable : _IInstantiatableDeletable
    {
        EntityID ID { get; set; }
        int ServerObjectType { get; set; }
        bool RecentlyUpdated { get; set; }
        byte[] GetBytes();
        void ParseBytes(DZNetwork.Packet Data, ulong ServerTick);
    }

    public interface IRenderer : _IInstantiatableDeletable
    {
        int SortingLayer { get; set; }

        void InitializeRenderer();

        void Render();
    }

    public interface IRenderer<T> : _IInstantiatableDeletable, IRenderer where T : class
    {
        T RenderObject { get; set; }
    }

    public interface IUpdatable : _IInstantiatableDeletable
    {
        void Update();
        void BodyPhysicsUpdate();
    }

    public interface IPhysicsUpdatable : _IInstantiatableDeletable
    {
        void IsolateVelocity();
        void RestoreVelocity();
    }

    public interface IIteratableUpdatable : _IInstantiatableDeletable
    {
        void PreUpdate();
        void IteratedUpdate();
    }
}