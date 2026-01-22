using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using System;

public enum DeviceType
{
    Keyboard,
    Xbox,
    PlayStation
}

public class InputDeviceManager : MonoBehaviour
{
    public static InputDeviceManager Instance;
    public event Action<DeviceType> OnInputChanged;

    [Header("--- Debug Status ---")]
    [SerializeField] private DeviceType currentDevice = DeviceType.Keyboard;

    // A threshold to ignore stick drift and sensitive triggers
    private const float INPUT_THRESHOLD = 0.2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        InputSystem.onActionChange += OnActionChange;
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= OnActionChange;
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        // 1. Only listen to actual input events
        if (change != InputActionChange.ActionPerformed) return;

        if (obj is InputAction action)
        {
            InputControl control = action.activeControl;
            if (control == null) return;
            if (control.device == null) return;

            // --- AGGRESSIVE NOISE FILTERING ---

            // A. Ignore Sensors completely (Gyro, Accel, Gravity, Orientation)
            if (control.device.description.deviceClass == "Sensor") return;
            if (control.path.Contains("gyro") || control.path.Contains("accel") || control.path.Contains("sensor")) return;

            // B. Ignore "Mouse" input coming from a Gamepad (The PS4/5 Touchpad)
            // The Touchpad often registers as a pointer, confusing the system.
            if (control.device is Gamepad && (control is TouchControl || control.path.Contains("touch"))) return;

            // C. VALUE CHECK: Is the input strong enough?
            // This filters out Stick Drift (0.01) and Hair-Trigger sensitivity (0.05)
            if (HasSignificantInput(action, control))
            {
                DetectDevice(control.device);
            }
        }
    }

    private bool HasSignificantInput(InputAction action, InputControl control)
    {
        // 1. Buttons (Boolean) - These are usually safe, but let's be sure.
        if (control is ButtonControl button)
        {
            if (button.isPressed) return true;
        }

        // 2. Sticks / Vectors (Vector2)
        // Must move the stick at least 20% to count as a switch.
        if (action.expectedControlType == "Vector2")
        {
            float magnitude = action.ReadValue<Vector2>().magnitude;
            return magnitude > INPUT_THRESHOLD;
        }

        // 3. Triggers (Axis / float)
        // Must press L2/R2 at least 20% to count.
        if (action.expectedControlType == "Axis" || action.expectedControlType == "float")
        {
            float value = Mathf.Abs(action.ReadValue<float>());
            return value > INPUT_THRESHOLD;
        }

        // Fallback: If it's a Key or something else, assume it's valid if triggered.
        return true;
    }

    private void DetectDevice(InputDevice device)
    {
        DeviceType detected = currentDevice;

        // 1. KEYBOARD / MOUSE
        if (device is Keyboard || device is Mouse)
        {
            detected = DeviceType.Keyboard;
        }
        // 2. GAMEPADS
        else if (device is Gamepad)
        {
            // Detect PlayStation
            // We check for "DualShock", "Sony", or the specific Interface Name.
            if (device is DualShockGamepad ||
                device.description.interfaceName == "DualShock4" ||
                device.description.product.Contains("Sony") ||
                device.description.product.Contains("Wireless Controller") ||
                device.name.Contains("DualSense"))
            {
                detected = DeviceType.PlayStation;
            }
            else
            {
                // Default to Xbox for everything else (XInput)
                detected = DeviceType.Xbox;
            }
        }

        // Only switch if it's actually different
        if (detected != currentDevice)
        {
            currentDevice = detected;
            // Debug.Log($"Input Switched to {currentDevice} via {device.name}");
            OnInputChanged?.Invoke(currentDevice);
        }
    }

    public DeviceType GetCurrentDevice() => currentDevice;
}