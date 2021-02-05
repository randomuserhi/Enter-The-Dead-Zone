using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine.InputSystem;
using UnityEngine;

using DeadZoneEngine.Controllers;
using ClientHandle;
using DZNetwork;

public class PlayerController : Controller
{
    public Player Owner;
    public PlayerCreature.Control PlayerControl;

    public PlayerController(InputDevice Device) : base(Device)
    {
        if (Device == null) return;

        InputAction Movement = ActionMap.AddAction("Movement", InputActionType.PassThrough);
        if (Device is Keyboard)
            Movement.AddCompositeBinding("2DVector(mode=2)")
                    .With("Up", Device.path + "/w")
                    .With("Down", Device.path + "/s")
                    .With("Left", Device.path + "/a")
                    .With("Right", Device.path + "/d");
        else
            Movement.AddCompositeBinding("2DVector(mode=2)")
                    .With("Up", Device.path + "/stick/up")
                    .With("Down", Device.path + "/stick/down")
                    .With("Left", Device.path + "/stick/left")
                    .With("Right", Device.path + "/stick/right");
        Movement.performed += MoveAction;

        Actions.Push(new PlayerAction());
    }

    protected override void SetType()
    {
        Type = ControllerType.PlayerController;
    }

    public override void OnInput(UnityEngine.InputSystem.Controls.ButtonControl Control)
    {
        if (Owner != null || (IsKeyboard && Control.name != "enter"))
            return;
        //Player P = Game.Client.AddPlayer();
        //P.Controller = this;
        Enable();
    }

    public override void Tick()
    {
        if (Actions.Count > 0)
            Actions.Peek().Ticks++;
    }

    public void MoveAction(InputAction.CallbackContext Context)
    {
        if (PlayerControl == null) return;

        PlayerControl.MovementDirection = Context.ReadValue<Vector2>();
        if (Actions.Count > 0 && Actions.Peek().Type == PlayerAction.ActionType.Movement)
        {
            Vector2 Prev = (Vector2)Actions.Peek().Value;
            if (Prev != PlayerControl.MovementDirection)
            {
                Actions.Push(new PlayerAction(PlayerAction.ActionType.Movement, PlayerControl.MovementDirection));
            }
        }
        Debug.Log(PlayerControl.MovementDirection);
    }

    private class PlayerAction
    {
        public enum ActionType
        {
            None,
            Movement
        }
        public ActionType Type = ActionType.None;
        public object Value;
        public ushort Ticks = 0;

        public PlayerAction(ActionType Type = ActionType.None, object Value = null)
        {
            this.Type = Type;
            this.Value = Value;
        }

        public static void ParseBytes(Player Owner, Packet Bytes, ref ulong Ticks) //TODO:: Use the ticks here for interpolation, and fix client side interpolation of recieving snapshots
        {
            ActionType Type = (ActionType)Bytes.ReadInt();
            Debug.Log("Bruh");
            switch (Type)
            {
                case ActionType.None: break;
                case ActionType.Movement:
                    {
                        Owner.Entity.Controller.MovementDirection = new Vector2(Bytes.ReadFloat(), Bytes.ReadFloat());
                        Debug.Log(Owner.Entity.Controller.MovementDirection);
                    }
                    break;
                default: break;
            }
        }

        public byte[] GetBytes()
        {
            List<byte> Data = new List<byte>();
            Data.AddRange(BitConverter.GetBytes((int)Type));
            switch (Type)
            {
                case ActionType.None: break;
                case ActionType.Movement: { Vector2 V = (Vector2)Value; Data.AddRange(BitConverter.GetBytes(V.x)); Data.AddRange(BitConverter.GetBytes(V.y)); } break;
                default: break;
            }
            return Data.ToArray();
        }
    }
    private Stack<PlayerAction> Actions = new Stack<PlayerAction>();

    public override void ParseBytes(Packet Data)
    {
        int ActionCount = Data.ReadInt();
        ulong Ticks = Owner.Owner.CurrentServerTick;
        Debug.Log(ActionCount);
        for (int i = 0; i < ActionCount; i++)
        {
            PlayerAction.ParseBytes(Owner, Data, ref Ticks);
        }
    }
    public override byte[] GetBytes()
    {
        List<byte> Data = new List<byte>();
        Data.AddRange(BitConverter.GetBytes(Owner.ID));
        Data.AddRange(BitConverter.GetBytes(Actions.Count));
        while (Actions.Count > 0)
        {
            Data.AddRange(Actions.Pop().GetBytes());
        }
        return Data.ToArray();
    }
}

public static class InputManager
{
    public static void Initialize()
    {
        InputMapping.OnDeviceAdd += OnDeviceAdd;
        InputMapping.OnDeviceDisconnect += OnDeviceDisconnect;
        InputMapping.OnDeviceReconnect += OnDeviceReconnect;
        InputMapping.OnDeviceRemove += OnDeviceRemove;
    }

    public static void OnDeviceAdd(InputDevice Device)
    {
        InputMapping.Devices[Device].Controllers.Add(new PlayerController(Device));
    }

    public static void OnDeviceDisconnect(InputDevice Device)
    {
    }

    public static void OnDeviceReconnect(InputDevice Device)
    {
    }

    public static void OnDeviceRemove(InputDevice Device)
    {
    }
}
