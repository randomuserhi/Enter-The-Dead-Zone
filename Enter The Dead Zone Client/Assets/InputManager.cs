using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine.InputSystem;
using UnityEngine;

using DeadZoneEngine.Controllers;
using ClientHandle;
using static DeadZoneEngine.Controllers.InputMapping;
using DZNetwork;

public class PlayerController : Controller
{
    public Player Owner;
    public PlayerCreature.Control PlayerControl;

    public PlayerController(Player Owner, PlayerCreature.Control PlayerControl) : base()
    {
        this.Owner = Owner;
        this.PlayerControl = PlayerControl;
    }
    private InputAction Interact;
    public PlayerController(InputDevice Device, DeviceController DC) : base(Device, DC)
    {
        InputAction Movement = ActionMap.AddAction("Movement", InputActionType.PassThrough);
        Interact = ActionMap.AddAction("Interact");
        if (Device is Keyboard)
        {
            Movement.AddCompositeBinding("2DVector(mode=2)")
                    .With("Up", Device.path + "/w")
                    .With("Down", Device.path + "/s")
                    .With("Left", Device.path + "/a")
                    .With("Right", Device.path + "/d");
            Interact.AddBinding(Device.path + "/space");
        }
        else
        {
            Movement.AddCompositeBinding("2DVector(mode=2)")
                    .With("Up", Device.path + "/stick/up")
                    .With("Down", Device.path + "/stick/down")
                    .With("Left", Device.path + "/stick/left")
                    .With("Right", Device.path + "/stick/right");
            Interact.AddBinding(Device.path + "/trigger");
        }
        Movement.performed += MoveAction;
    }

    protected override void SetType()
    {
        Type = ControllerType.PlayerController;
    }

    public override void OnInput(UnityEngine.InputSystem.Controls.ButtonControl Control)
    {
        if (Owner != null || (IsKeyboard && Control.name != "enter"))
            return;
        Player P = Game.Client.AddPlayer();
        P.Controller = this;
        DC.Enable();
    }

    private Vector2 MovementDirection;
    public void MoveAction(InputAction.CallbackContext Context)
    {
        MovementDirection = Context.ReadValue<Vector2>();
    }

    public override void Tick()
    {
        if (PlayerControl == null) return;
        PlayerControl.MovementDirection = MovementDirection;
        PlayerControl.Interact = Interact.ReadValue<float>();
    }

    public override void ParseBytes(Packet Data)
    {
        PlayerControl.InputID = Data.ReadULong();
        PlayerControl.Interact = Data.ReadFloat();
        PlayerControl.MovementDirection = new Vector2(Data.ReadFloat(), Data.ReadFloat());
    }
    public override byte[] GetBytes()
    {
        List<byte> Data = new List<byte>();
        Data.Add(Owner.ID);
        Data.AddRange(BitConverter.GetBytes(PlayerControl.InputID));
        Data.AddRange(BitConverter.GetBytes(PlayerControl.Interact));
        Data.AddRange(BitConverter.GetBytes(PlayerControl.MovementDirection.x));
        Data.AddRange(BitConverter.GetBytes(PlayerControl.MovementDirection.y));
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
        InputMapping.Devices[Device].Controllers.Add(new PlayerController(Device, InputMapping.Devices[Device]));
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
