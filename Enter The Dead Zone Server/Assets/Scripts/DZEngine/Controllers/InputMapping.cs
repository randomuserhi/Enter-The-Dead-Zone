using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine;
using DZNetwork;

namespace DeadZoneEngine.Controllers
{
    public enum ControllerType
    {
        PlayerController
    }

    public static class InputMapping
    {
        public static Action<InputDevice> OnDeviceAdd;
        public static Action<InputDevice> OnDeviceReconnect;
        public static Action<InputDevice> OnDeviceRemove;
        public static Action<InputDevice> OnDeviceDisconnect;

        public class DeviceController
        {
            public List<Controller> Controllers = new List<Controller>();

            public byte[] GetBytes()
            {
                List<byte> Data = new List<byte>();
                Data.AddRange(BitConverter.GetBytes(Controllers.Count));
                for (int i = 0; i < Controllers.Count; i++)
                {
                    Data.AddRange(BitConverter.GetBytes((int)Controllers[i].Type));
                    Data.AddRange(Controllers[i].GetBytes());
                }
                return Data.ToArray();
            }

            public void Tick()
            {
                for (int i = 0; i < Controllers.Count; i++)
                {
                    Controllers[i].Tick();
                }
            }

            public void OnInput(ButtonControl Control)
            {
                for (int i = 0; i < Controllers.Count; i++)
                    Controllers[i].OnInput(Control);
            }

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
            //Detects any key press https://forum.unity.com/threads/check-if-any-key-is-pressed.763751/
            InputSystem.onEvent += (Event, Device) =>
            {
                if (!Devices.ContainsKey(Device)) return;
                if (!Event.IsA<StateEvent>() && !Event.IsA<DeltaStateEvent>()) return;
                var Controls = Device.allControls;
                float ButtonPressPoint = InputSystem.settings.defaultButtonPressPoint;
                for (var i = 0; i < Controls.Count; ++i)
                {
                    ButtonControl Control = Controls[i] as ButtonControl;
                    if (Control == null || Control.synthetic || Control.noisy)
                        continue;
                    if (Control.ReadValueFromEvent(Event, out var Value) && Value >= ButtonPressPoint)
                    {
                        Devices[Device].OnInput(Control);
                        break;
                    }
                }
            };
            InputSystem.onDeviceChange += OnDeviceChange;
            for (int i = 0; i < InputSystem.devices.Count; i++)
            {
                if (!(InputSystem.devices[i] is Mouse))
                {
                    DeviceController DC = new DeviceController();
                    Devices.Add(InputSystem.devices[i], DC);
                    OnDeviceAdd?.Invoke(InputSystem.devices[i]);
                }
            }
        }

        public static void Tick()
        {
            List<DeviceController> Controllers = Devices.Values.ToList();
            foreach (DeviceController DC in Controllers)
            {
                DC.Tick();
            }
        }

        public static byte[] GetBytes()
        {
            List<byte> Data = new List<byte>();
            List<DeviceController> Controllers = Devices.Values.ToList();
            foreach (DeviceController DC in Controllers)
            {
                Data.AddRange(DC.GetBytes());
            }
            return Data.ToArray();
        }

        private static void OnDeviceChange(InputDevice Device, InputDeviceChange Change)
        {
            switch (Change)
            {
                case InputDeviceChange.Added:
                    Debug.Log("Device: " + Device.displayName + " was added");
                    if (!Devices.ContainsKey(Device))
                    {
                        Devices.Add(Device, new DeviceController());
                        OnDeviceAdd?.Invoke(Device);
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
                        Devices.Add(Device, new DeviceController());
                    OnDeviceReconnect?.Invoke(Device);
                    Devices[Device].Enable();
                    break;
            }
        }

        public static void Rebind(InputAction Action, InputDevice Device)
        {

        }
    }

    public abstract class Controller
    {
        public ControllerType Type;
        private InputDevice Device;
        protected bool IsKeyboard { get; private set; } = true;
        protected InputActionMap ActionMap = new InputActionMap("Controller");

        public Controller(InputDevice Device)
        {
            this.Device = Device;
            IsKeyboard = Device is Keyboard;
            SetType();
        }

        protected abstract void SetType();

        //Triggered when any button on the device is pressed
        public virtual void OnInput(ButtonControl Control)
        {
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

        public virtual void Tick()
        {

        }

        public abstract void ParseBytes(Packet Data);
        public abstract byte[] GetBytes();
    }
}
