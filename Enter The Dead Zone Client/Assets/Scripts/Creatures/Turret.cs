﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using DeadZoneEngine.Entities;
using DeadZoneEngine.Entities.Components;

public class Turret : AbstractCreature, IServerSendable
{
    public int ServerObjectType { get; set; } = (int)DZSettings.EntityType.Turret;
    public bool RecentlyUpdated { get; set; } = false;
    public bool ProtectedDeletion { get; set; } = false;

    public int Timer;
    public int FireRate;

    public Turret(ushort ID) : base(ID)
    {
        Initialize();
    }
    public Turret()
    {
        Initialize();
    }

    public override void Render()
    {
        BodyChunks[0].RenderObject.gameObject.transform.localScale = new Vector2(0.7f, 0.7f);
    }

    Color BodyColor;
    private void Initialize()
    {
        BodyChunks = new BodyChunk[1];
        BodyChunks[0] = new BodyChunk(this);
        BodyChunks[0].Collider.radius = 0.35f;
        BodyChunks[0].Kinematic = true;
        BodyChunks[0].Context = this;
        BodyChunks[0].ContextType = DZSettings.EntityType.Turret;
        SetGravity(0f);

        BodyColor = new Color(0.56f, 0.56f, 0.56f);
        BodyChunks[0].RenderColor = BodyColor;
    }

    public void SetGravity(float Gravity)
    {
        BodyChunks[0].Gravity = Gravity;
    }

    public Vector2 Position
    {
        get
        {
            if (BodyChunks[0] != null)
                return BodyChunks[0].Position;
            return Vector2.zero;
        }
        set
        {
            if (BodyChunks[0] != null)
                BodyChunks[0].Position = value;
            if (BodyChunks[1] != null)
                BodyChunks[1].Position = value;
        }
    }

    public void ServerUpdate()
    {
    }

    public override void Update()
    {
    }

    private void UpdateBodyState()
    {

    }
    protected override void OnDelete()
    {
        BodyChunks[0].Delete();
    }

    public override byte[] GetBytes()
    {
        List<byte> Data = new List<byte>();
        Data.AddRange(BodyChunks[0].GetBytes());
        return Data.ToArray();
    }

    public override void ParseBytes(DZNetwork.Packet Data)
    {
        ParseSnapshot((Data)ParseBytesToSnapshot(Data));
    }

    public struct Data
    {
        public BodyChunk.Data BodyChunk0;
    }

    public static object ParseBytesToSnapshot(DZNetwork.Packet Data)
    {
        return new Data()
        {
            BodyChunk0 = (BodyChunk.Data)BodyChunk.ParseBytesToSnapshot(Data)
        };
    }
    public override void ParseSnapshot(object ObjectData)
    {
        Data Data = (Data)ObjectData;
        BodyChunks[0].ParseSnapshot(Data.BodyChunk0);
    }

    public override object GetSnapshot()
    {
        return new Data()
        {
            BodyChunk0 = (BodyChunk.Data)BodyChunks[0].GetSnapshot()
        };
    }

    public override void Interpolate(object FromData, object ToData, float Time)
    {
        Data From = (Data)FromData;
        Data To = (Data)ToData;
        BodyChunks[0].Interpolate(From.BodyChunk0, To.BodyChunk0, Time);
    }

    public override void Extrapolate(object FromData, float Time)
    {
        Data From = (Data)FromData;
        BodyChunks[0].Extrapolate(From.BodyChunk0, Time);
    }
}
