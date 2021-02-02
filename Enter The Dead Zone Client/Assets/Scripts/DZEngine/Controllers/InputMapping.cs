using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine.InputSystem;
using UnityEngine;

namespace DeadZoneEngine.Controllers
{
    public static class InputMapping
    {
        public static Action<InputDevice> OnDeviceAdd;
        public static Action<InputDevice> OnDeviceReconnect;
        public static Action<InputDevice> OnDeviceRemove;
        public static Action<InputDevice> OnDeviceDisconnect;

        public class DeviceController
        {
            public List<Controller> Controllers = new List<Controller>();

            public void Enable()
            {
                for (int i = 0; i < Controllers.Count; i++)
                    Controllers[i].Enable();
            }

            public void Disable()
            {
                for (int i = 0; i < Controllers.Count; i++)
                    Controllers[i].Disable();
            }
        }

        private class DeviceComparer : IEqualityComparer<InputDevice>
        {
            public bool Equals(InputDevice A, InputDevice B)
            {
                return A.Equals(B);
            }

            public int GetHashCode(InputDevice A)
            {
                return A.GetHashCode();
            }
        }

        public static Dictionary<InputDevice, DeviceController> Devices = new Dictionary<InputDevice, DeviceController>(new DeviceComparer());

        public static void Initialize()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            for (int i = 0; i < InputSystem.devices.Count; i++)
            {
                if (!(InputSystem.devices[i] is Mouse))
                {
                    DeviceController DC = new DeviceController();
                    Devices.Add(InputSystem.devices[i], DC);
                    OnDeviceAdd?.Invoke(InputSystem.devices[i]);
                    DC.Enable();
                }
            }
        }

        private static void OnDeviceChange(InputDevice Device, InputDeviceChange Change)
        {
            switch (Change)
            {
                case InputDeviceChange.Added:
                    Debug.Log("Device: " + Device.displayName + " was added");
                    if (!Devices.ContainsKey(Device))
                    {
                        Devices.Add(Device, new DeviceController()); //TODO:: some way of checking if this device has been encountered before to load saved configurations
                        OnDeviceAdd?.Invoke(Device); //TODO:: this is wrong shouldnt be inside brackets but otherwise it causes addplayer to be caused multiple times due to bad code, refactor required
                    }
                    Devices[Device].Enable();
                    break;
                case InputDeviceChange.Removed:
                    Debug.Log("Device: " + Device.displayName + " was removed");
                    OnDeviceRemove?.Invoke(Device);
                    if (Devices.ContainsKey(Device))
                        Devices[Device].Disable();
                    break;
                case InputDeviceChange.Disconnected:
                    Debug.Log("Device: " + Device.displayName + " was disconnected");
                    OnDeviceDisconnect?.Invoke(Device);
                    if (Devices.ContainsKey(Device))
                        Devices[Device].Disable();
                    break;
                case InputDeviceChange.Reconnected:
                    Debug.Log("Device: " + Device.displayName + " has reconnected");
                    if (!Devices.ContainsKey(Device))
                        Devices.Add(Device, new DeviceController()); //TODO:: some way of checking if this device has been encountered before to load saved configurations
                    OnDeviceReconnect?.Invoke(Device);
                    Devices[Device].Enable();
                    break;
            }
        }

        public static void Rebind(InputAction Action, InputDevice Device)
        {
            Action.Disable();
            var RebindOperation = Action.PerformInteractiveRebinding()
                                        .WithControlsHavingToMatchPath(Device.path)
                                        .WithControlsExcluding("Mouse")
                                        .OnMatchWaitForAnother(0.1f)
                                        .WithCancelingThrough("<Keyboard>/escape");
            RebindOperation.OnComplete((Operation) =>
            {
                Debug.Log($"Rebound '" + Action + "' to '" + Operation.selectedControl + "'");
                Operation.Dispose();
                Action.Enable();
            });
            RebindOperation.OnCancel((Operation) =>
            {
                Debug.Log($"Cancelled rebinding of '" + Action + "'");
                Operation.Dispose();
                Action.Enable();
            });
            RebindOperation.Start();
        }
    }

    public class Controller
    {
        private InputDevice Device;
        protected InputActionMap ActionMap = new InputActionMap("Controller");

        public Controller(InputDevice Device)
        {
            this.Device = Device;
        }

        public void Enable()
        {
            ActionMap.Enable();
        }

        public void Disable()
        {
            ActionMap.Disable();
        }

        public void RebindAll()
        {
            for (int i = 0; i < ActionMap.actions.Count; i++)
            {
                InputMapping.Rebind(ActionMap.actions[i], Device);
            }
        }

        public void Reset()
        {
            ActionMap.RemoveAllBindingOverrides();
        }

        public void Reset(string ActionName)
        {
            ActionMap.FindAction(ActionName).RemoveAllBindingOverrides();
        }

        public void Reset(int Index)
        {
            ActionMap.actions[Index].RemoveAllBindingOverrides();
        }

        public void Rebind(string ActionName)
        {
            InputMapping.Rebind(ActionMap.FindAction(ActionName), Device);
        }

        public void Rebind(int Index)
        {
            InputMapping.Rebind(ActionMap.actions[Index], Device);
        }

        public void Rebind(InputAction Action)
        {
            InputMapping.Rebind(Action, Device);
        }
    }
}
