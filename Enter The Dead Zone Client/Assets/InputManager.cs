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
    public Vector2 MovementDirection;

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
        MovementDirection = Context.ReadValue<Vector2>();
        Debug.Log(MovementDirection);
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
        InputMapping.DeviceController DC = InputMapping.Devices[Device];
        PlayerController PC = new PlayerController(Device);
        DC.Controllers.Add(PC);
        Game.Client.AddPlayer(PC);
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
