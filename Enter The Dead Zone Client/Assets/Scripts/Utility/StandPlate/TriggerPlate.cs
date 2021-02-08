using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using DeadZoneEngine;
using DeadZoneEngine.Entities;
using DZNetwork;
using ClientHandle;

public class TriggerPlate : AbstractWorldEntity, IUpdatable, IRenderer, IServerSendable
{
    public int ServerObjectType { get; set; } = (int)DZSettings.EntityType.TriggerPlate;
    public bool RecentlyUpdated { get; set; } = false;
    public bool ProtectedDeletion { get; set; } = false;

    public int SortingLayer { get; set; }

    private GameObject Self;
    private SpriteRenderer Renderer;

    private GameObject Bar;
    private SpriteRenderer BarRenderer;

    public Vector2 Size = new Vector2(1, 1);
    public float Value = 0;

    public int RequiredNumPlayers = 0;

    public Action OnTrigger = null;
    public Action<Player> OnInteract = null;

    public TriggerPlate(ushort ID) : base(ID)
    {
        Init(new Vector2(0, 0));
    }

    public TriggerPlate(Vector2 Size, Vector2 Position)
    {
        this.Size = Size;
        Init(Position);
    }

    private void Init(Vector2 Position)
    {
        Self = new GameObject();
        Self.transform.localScale = Size;
        Self.transform.position = Position;

        Bar = new GameObject();
        Bar.transform.parent = Self.transform;
        Bar.transform.localPosition = new Vector2(-1, 0);
    }

    public void InitializeRenderer()
    {
        Renderer = Self.AddComponent<SpriteRenderer>();
        Renderer.sortingLayerName = "TriggerPlates";
        Renderer.sortingOrder = 0;
        Renderer.sprite = Resources.Load<Sprite>("Sprites/Square");

        BarRenderer = Bar.AddComponent<SpriteRenderer>();
        BarRenderer.sortingLayerName = "TriggerPlates";
        BarRenderer.sortingOrder = 1;
        BarRenderer.sprite = Resources.Load<Sprite>("Sprites/Square");
        BarRenderer.color = new Color(0, 0.64f, 0.9f);
    }

    public void Render()
    {
        BarRenderer.transform.localPosition = new Vector2(-0.5f + Value / 2, 0);
        BarRenderer.transform.localScale = new Vector2(Value, 0.9f);
    }

    public void ServerUpdate()
    {

    }

    public void Update()
    {
        
    }

    public void BodyPhysicsUpdate()
    {

    }

    protected override void OnDelete()
    {
        GameObject.Destroy(Self);
        GameObject.Destroy(Bar);
    }

    public override byte[] GetBytes()
    {
        List<byte> Data = new List<byte>();
        Data.AddRange(BitConverter.GetBytes(Self.transform.position.x));
        Data.AddRange(BitConverter.GetBytes(Self.transform.position.y));
        Data.AddRange(BitConverter.GetBytes(Size.x));
        Data.AddRange(BitConverter.GetBytes(Size.y));
        Data.AddRange(BitConverter.GetBytes(Mathf.Min(Value, 1)));
        return Data.ToArray();
    }
    public override void ParseBytes(Packet Data)
    {
        ParseSnapshot(ParseBytesToSnapshot(Data));
    }

    public struct Data
    {
        public Vector2 Position;
        public Vector2 Size;
        public float Value;
    }

    public override object GetSnapshot()
    {
        return new Data()
        {
            Position = Self.transform.position,
            Size = Size,
            Value = Value
        };
    }

    public static object ParseBytesToSnapshot(DZNetwork.Packet Data)
    {
        return new Data()
        {
            Position = new Vector2(Data.ReadFloat(), Data.ReadFloat()),
            Size = new Vector2(Data.ReadFloat(), Data.ReadFloat()),
            Value = Data.ReadFloat()
        };
    }

    public override void ParseSnapshot(object ObjectData)
    {
        Data Data = (Data)ObjectData;
        Self.transform.position = Data.Position;
        Size = Data.Size;
        Value = Data.Value;
    }

    public override void Interpolate(object FromData, object ToData, float Time)
    {
        Data From = (Data)FromData;
        Data To = (Data)ToData;
        Self.transform.position = From.Position + (To.Position - From.Position) * Time;
        Size = From.Size;
        Value = From.Value + (To.Value - From.Value) * Time;
    }

    public override void Extrapolate(object FromData, float Time)
    {
        Data From = (Data)FromData;
        Self.transform.position = From.Position;
        Size = From.Size;
        Value = From.Value;
    }
}
