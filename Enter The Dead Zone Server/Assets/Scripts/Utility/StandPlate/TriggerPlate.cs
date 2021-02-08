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
    private float Incrementer = 0;
    public float MaxValue = 10;

    public int RequiredNumPlayers = 0;

    public Action OnTrigger = null;
    public Action<Player> OnInteract = null;

    public TriggerPlate(ushort ID) : base(ID)
    {
        Init(new Vector2(0, 0));
    }

    public TriggerPlate(Vector2 Size, Vector2 Position, int RequiredNumPlayers = 0, float MaxValue = 10)
    {
        this.Size = Size;
        this.MaxValue = MaxValue;
        this.RequiredNumPlayers = RequiredNumPlayers;
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

    private bool CheckBound(Vector2 Pos)
    {
        return Pos.x > Self.transform.position.x - Self.transform.localScale.x / 2 &&
               Pos.x < Self.transform.position.x + Self.transform.localScale.x / 2 &&
               Pos.y > Self.transform.position.y - Self.transform.localScale.y / 2 &&
               Pos.y < Self.transform.position.y + Self.transform.localScale.y / 2;
    }
    private bool CheckInBounds(Vector2 Position, float Margin)
    {
        return CheckBound(new Vector2(Position.x - Margin, Position.y - Margin)) ||
               CheckBound(new Vector2(Position.x + Margin, Position.y + Margin)) ||
               CheckBound(new Vector2(Position.x - Margin, Position.y + Margin)) ||
               CheckBound(new Vector2(Position.x + Margin, Position.y - Margin));
    }

    private bool Triggered = false;
    private Player LastPlayerTrigger = null;
    private Client LastClientTrigger = null;
    public void Update()
    {
        List<Client> Clients = ClientID.ConnectedClients.Values.ToList();
        int Count = 0;
        int TotalNumPlayers = 0;
        foreach (Client C in Clients)
        {
            if (C == null) continue;
            TotalNumPlayers += C.NumPlayers;
            for (int i = 0; i < C.Players.Length; i++)
            {
                if (C == LastClientTrigger && LastPlayerTrigger != null && i == LastPlayerTrigger.ID && C.Players[i] == null) LastPlayerTrigger = null;
                if (C.Players[i] == null || C.Players[i].Entity == null) continue;

                PlayerCreature Entity = C.Players[i].Entity;
                
                if (CheckInBounds(Entity.Position, 0.5f))
                {
                    Count++;
                    if (Entity.Controller.Interact > 0 && LastPlayerTrigger == null)
                    {
                        LastPlayerTrigger = C.Players[i];
                        LastClientTrigger = C;
                        OnInteract?.Invoke(C.Players[i]);
                        if (RequiredNumPlayers < 0)
                        {
                            Incrementer ++;
                            if (Incrementer > MaxValue)
                            {
                                Incrementer = 0;
                            }
                            Value = (float)Incrementer / MaxValue;
                        }
                    }
                    else if (Entity.Controller.Interact <= 0.1f && C.Players[i] == LastPlayerTrigger)
                    {
                        LastPlayerTrigger = null;
                    }
                }
            }
        }
        if (TotalNumPlayers > 0)
        {
            if (RequiredNumPlayers == 0 && Count == TotalNumPlayers)
            {
                Value += Time.fixedDeltaTime / 3;
            }
            else if (RequiredNumPlayers > 0 && Count >= RequiredNumPlayers)
            {
                Value += Time.fixedDeltaTime / 3;
            }
            else if (RequiredNumPlayers >= 0)
            {
                Value -= Time.fixedDeltaTime * 2;
                Triggered = false;
            }
        }

        if (Value >= 1 && Triggered == false)
        {
            Triggered = true;
            Value = 1;
            OnTrigger?.Invoke();
        }
        else if (Value < 0)
            Value = 0;
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
}
