using UnityEngine;
using UnityEngine.InputSystem;

// Last used pointing device: gamepad once one connects or is used, mouse again as soon as it moves or clicks.
[DefaultExecutionOrder(-200)]
public class InputModeTracker : MonoBehaviour
{
    private const float StickThreshold = 0.5f;
    private const float TriggerThreshold = 0.5f;
    private const float MouseMoveThreshold = 2f;

    public static bool UsingGamepad { get; private set; }

    public static event System.Action<bool> OnModeChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameObject go = new GameObject("InputModeTracker");

        DontDestroyOnLoad(go);
        go.AddComponent<InputModeTracker>();
        SetMode(Gamepad.current != null);
    }

    // Global inner deadzone of every stick, read by the default stickDeadzone processor of Move, Aim and UI navigation.
    private static void ApplyStickDeadzone()
    {
        InputSystem.settings.defaultDeadzoneMin = GameSettings.StickDeadzone;
    }

    private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!(device is Gamepad))
            return;
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            SetMode(true);
        else if ((change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected) && Gamepad.all.Count == 0)
            SetMode(false);
    }

    private static bool PadAxisUsed(Gamepad pad)
    {
        if (pad.leftStick.ReadValue().magnitude > StickThreshold || pad.rightStick.ReadValue().magnitude > StickThreshold)
            return true;
        if (pad.dpad.ReadValue().magnitude > StickThreshold)
            return true;
        return pad.leftTrigger.ReadValue() > TriggerThreshold || pad.rightTrigger.ReadValue() > TriggerThreshold;
    }

    private static bool PadButtonPressed(Gamepad pad)
    {
        if (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame)
            return true;
        if (pad.buttonWest.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)
            return true;
        return pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame;
    }

    private static bool PadUsed(Gamepad pad)
    {
        return PadAxisUsed(pad) || PadButtonPressed(pad);
    }

    private static bool MouseUsed(Mouse mouse)
    {
        if (mouse.delta.ReadValue().magnitude > MouseMoveThreshold)
            return true;
        return mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
    }

    private static void SetMode(bool gamepad)
    {
        if (gamepad == UsingGamepad)
            return;
        UsingGamepad = gamepad;
        OnModeChanged?.Invoke(gamepad);
    }

    private void Update()
    {
        Gamepad pad = Gamepad.current;
        Mouse mouse = Mouse.current;

        if (pad != null && PadUsed(pad))
            SetMode(true);
        if (mouse != null && MouseUsed(mouse))
            SetMode(false);
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        GameSettings.OnChanged += ApplyStickDeadzone;
        ApplyStickDeadzone();
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
        GameSettings.OnChanged -= ApplyStickDeadzone;
    }
}
