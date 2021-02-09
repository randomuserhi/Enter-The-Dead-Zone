using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using DeadZoneEngine;
using DZNetwork;
using DeadZoneEngine.Entities;

public class CrystalEntity : AbstractWorldEntity, IUpdatable, IRenderer, IServerSendable
{

    public int SortingLayer { get; set; }
    public int ServerObjectType { get; set; } = (int)DZSettings.EntityType.CrystalEntity;
    public bool RecentlyUpdated { get; set; } = false;
    public bool ProtectedDeletion { get; set; } = false;

    public int Value = 1;
    public GameObject[] Crystals;
    public SpriteRenderer[] CrystalRenders;
    private Sprite ActiveSprite;
    private Sprite InActiveSprite;
    public Vector2 Position = Vector3.zero;

    public CrystalEntity(ushort ID) : base(ID)
    {
        Init();
    }

    public CrystalEntity() : base()
    {
        Init();
    }

    public void Init()
    {
        Crystals = new GameObject[10];
        for (int i = 0; i < Crystals.Length; i++)
        {
            Crystals[i] = new GameObject();
            Crystals[i].transform.position = Vector2.zero;
        }
    }

    public void InitializeRenderer()
    {
        ActiveSprite = Resources.Load<Sprite>("Sprites/LifeCrystal");
        InActiveSprite = Resources.Load<Sprite>("Sprites/BrokenCrystal");
        CrystalRenders = new SpriteRenderer[Crystals.Length];
        for (int i = 0; i < Crystals.Length; i++)
        {
            CrystalRenders[i] = Crystals[i].AddComponent<SpriteRenderer>();
            CrystalRenders[i].sprite = ActiveSprite;
        }
    }

    public void Render()
    {
        int Count = 0;
        for (int i = 0; i < Crystals.Length; i++)
        {
            Crystals[i].transform.position = Position - new Vector2(0, 1) * i;

            if (Count < Value)
                CrystalRenders[i].sprite = ActiveSprite;
            else
                CrystalRenders[i].sprite = InActiveSprite;

            Count++;
        }
    }

    public void ServerUpdate()
    {

    }

    public void Update()
    {

    }

    public void BodyPhysicsUpdate() { }

    public void IsolateVelocity() { }

    public void RestoreVelocity() { }

    protected override void OnDelete()
    {
        for (int i = 0; i < Crystals.Length; i++)
            GameObject.Destroy(Crystals[i]);
    }

    public override byte[] GetBytes()
    {
        List<byte> Data = new List<byte>();
        Data.AddRange(BitConverter.GetBytes(Value));
        Data.AddRange(BitConverter.GetBytes(Position.x));
        Data.AddRange(BitConverter.GetBytes(Position.y));
        return Data.ToArray();
    }

    public override void ParseBytes(Packet Data)
    {
        ParseSnapshot(ParseBytesToSnapshot(Data));
    }

    public struct Data
    {
        public int Value;
        public Vector2 Position;
    }

    public override object GetSnapshot()
    {
        return new Data()
        {
            Value = Value,
            Position = Position
        };
    }

    public static object ParseBytesToSnapshot(DZNetwork.Packet Data)
    {
        return new Data()
        {
            Value = Data.ReadInt(),
            Position = new Vector2(Data.ReadFloat(), Data.ReadFloat())
        };
    }

    public override void ParseSnapshot(object ObjectData)
    {
        Data Data = (Data)ObjectData;
        Value = Data.Value;
    }

    public override void Interpolate(object FromData, object ToData, float Time)
    {
        Data From = (Data)FromData;
        Data To = (Data)ToData;
        Value = Mathf.RoundToInt(From.Value + (To.Value - From.Value) * Time);
        Position = From.Position + (To.Position - From.Position) * Time;
    }

    public override void Extrapolate(object FromData, float Time)
    {
        Data From = (Data)FromData;
        Value = Mathf.RoundToInt(From.Value);
        Position = From.Position;
    }
}