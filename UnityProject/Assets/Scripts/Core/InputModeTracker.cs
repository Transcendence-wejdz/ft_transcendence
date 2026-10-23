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
        var go = new GameObject("InputModeTracker");
        DontDestroyOnLoad(go);
        go.AddComponent<InputModeTracker>();
        SetMode(Gamepad.current != null);
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

    private void Update()
    {
        Gamepad pad = Gamepad.current;
        if (pad != null && PadUsed(pad))
            SetMode(true);

        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.delta.ReadValue().magnitude > MouseMoveThreshold || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            SetMode(false);
    }

    private static bool PadUsed(Gamepad pad)
    {
        return pad.leftStick.ReadValue().magnitude > StickThreshold || pad.rightStick.ReadValue().magnitude > StickThreshold
            || pad.dpad.ReadValue().magnitude > StickThreshold
            || pad.leftTrigger.ReadValue() > TriggerThreshold || pad.rightTrigger.ReadValue() > TriggerThreshold
            || pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame
            || pad.buttonWest.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame
            || pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame;
    }

    private static void SetMode(bool gamepad)
    {
        if (gamepad == UsingGamepad)
            return;

        UsingGamepad = gamepad;
        OnModeChanged?.Invoke(gamepad);
    }
}
