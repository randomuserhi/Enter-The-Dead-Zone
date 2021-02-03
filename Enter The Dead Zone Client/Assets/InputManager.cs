using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine.InputSystem;
using UnityEngine;

using DeadZoneEngine.Controllers;

public class PlayerController : Controller
{
    public PlayerCreature.Control PlayerControl;

    public PlayerController(InputDevice Device) : base(Device)
    {
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
    }

    public void MoveAction(InputAction.CallbackContext Context)
    {
        if (PlayerControl == null) return;

        PlayerControl.MovementDirection = Context.ReadValue<Vector2>();
        Debug.Log(PlayerControl.MovementDirection);
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
        ClientHandle.Player P = Game.Client.AddPlayer();
        PlayerController PC = new PlayerController(Device);
        InputMapping.Devices[Device].Controllers.Add(PC);
        P.Controller = PC;
        PC.PlayerControl = P.Entity.Controller;
        P.Entity.Controller.Owner = PC;
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
