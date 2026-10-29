using UnityEngine;

// Local, per-client gameplay preferences; not synced to other players.
public static class GameSettings
{
    private const string ShowEnemyHealthBarsKey = "Settings.ShowEnemyHealthBars";
    private const string ScreenShakeEnabledKey = "Settings.ScreenShakeEnabled";
    private const string ShowDamageNumbersKey = "Settings.ShowDamageNumbers";
    private const string ShowFpsKey = "Settings.ShowFps";
    private const string ShowPingKey = "Settings.ShowPing";
    private const string GraphicsQualityKey = "Settings.GraphicsQuality";
    private const string HdrEnabledKey = "Settings.HdrEnabled";
    private const string StickDeadzoneKey = "Settings.StickDeadzone";
    private const string CameraDistanceKey = "Settings.CameraDistance";
    private const string CursorSizeKey = "Settings.CursorSize";
    private const string RelayRegionKey = "Settings.RelayRegion";
    private const string MouseSensitivityKey = "Settings.MouseSensitivity";
    private const string GamepadAimSpeedKey = "Settings.GamepadAimSpeed";
    private const string AimAssistKey = "Settings.AimAssist";
    private const string AimDeadzoneKey = "Settings.AimDeadzone";
    private const string ShowAttackPreviewKey = "Settings.ShowAttackPreview";
    private const string ShowSpecialPreviewKey = "Settings.ShowSpecialPreview";
    private const string ShowUltimatePreviewKey = "Settings.ShowUltimatePreview";

    public const float StickDeadzoneMin = 0.05f;
    public const float StickDeadzoneMax = 0.5f;
    public const float StickDeadzoneStep = 0.05f;
    public const float CameraDistanceMin = 0.75f;
    public const float CameraDistanceMax = 1.25f;
    public const float CameraDistanceStep = 0.05f;
    public const float CursorSizeMin = 0.5f;
    public const float CursorSizeMax = 2f;
    public const float CursorSizeStep = 0.1f;
    public const float MouseSensitivityMin = 0.25f;
    public const float MouseSensitivityMax = 3f;
    public const float MouseSensitivityStep = 0.05f;
    public const float AimDeadzoneMin = 0.05f;
    public const float AimDeadzoneMax = 0.9f;
    public const float AimDeadzoneStep = 0.05f;
    public const float GamepadAimSpeedMin = 0.25f;
    public const float GamepadAimSpeedMax = 3f;
    public const float GamepadAimSpeedStep = 0.05f;

    public static readonly string[] GraphicsQualityNames = { "Low", "Medium", "High", "Ultra" };

    public static event System.Action OnChanged;

    public static bool ShowEnemyHealthBars
    {
        get => PlayerPrefs.GetInt(ShowEnemyHealthBarsKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(ShowEnemyHealthBarsKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool ScreenShakeEnabled
    {
        get => PlayerPrefs.GetInt(ScreenShakeEnabledKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(ScreenShakeEnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool ShowDamageNumbers
    {
        get => PlayerPrefs.GetInt(ShowDamageNumbersKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(ShowDamageNumbersKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool ShowFps
    {
        get => PlayerPrefs.GetInt(ShowFpsKey, 0) != 0;
        set
        {
            PlayerPrefs.SetInt(ShowFpsKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool ShowPing
    {
        get => PlayerPrefs.GetInt(ShowPingKey, 0) != 0;
        set
        {
            PlayerPrefs.SetInt(ShowPingKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    // Browsers pay a lot more per pixel, so WebGL starts on High without HDR and everything else on Ultra with HDR.
    private static bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;

    public static int GraphicsQuality
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(GraphicsQualityKey, IsWebGL ? 2 : 3), 0, GraphicsQualityNames.Length - 1);
        set
        {
            PlayerPrefs.SetInt(GraphicsQualityKey, Mathf.Clamp(value, 0, GraphicsQualityNames.Length - 1));
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool HdrEnabled
    {
        get => PlayerPrefs.GetInt(HdrEnabledKey, IsWebGL ? 0 : 1) != 0;
        set
        {
            PlayerPrefs.SetInt(HdrEnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static float StickDeadzone
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(StickDeadzoneKey, 0.15f), StickDeadzoneMin, StickDeadzoneMax);
        set
        {
            PlayerPrefs.SetFloat(StickDeadzoneKey, Mathf.Clamp(value, StickDeadzoneMin, StickDeadzoneMax));
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    // Multiplier on the player camera's base orthographic size.
    public static float CameraDistance
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(CameraDistanceKey, 1f), CameraDistanceMin, CameraDistanceMax);
        set
        {
            PlayerPrefs.SetFloat(CameraDistanceKey, Mathf.Clamp(value, CameraDistanceMin, CameraDistanceMax));
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static float CursorSize
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(CursorSizeKey, 1f), CursorSizeMin, CursorSizeMax);
        set
        {
            PlayerPrefs.SetFloat(CursorSizeKey, Mathf.Clamp(value, CursorSizeMin, CursorSizeMax));
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool ShowAttackPreview
    {
        get => PlayerPrefs.GetInt(ShowAttackPreviewKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(ShowAttackPreviewKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool ShowSpecialPreview
    {
        get => PlayerPrefs.GetInt(ShowSpecialPreviewKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(ShowSpecialPreviewKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool ShowUltimatePreview
    {
        get => PlayerPrefs.GetInt(ShowUltimatePreviewKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(ShowUltimatePreviewKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    // Whether the hold-aim preview of an action is drawn; the hold-to-cast behaviour stays either way.
    public static bool ShowAimPreview(AbilityType ability) => ability switch
    {
        AbilityType.Special => ShowSpecialPreview,
        AbilityType.Ultimate => ShowUltimatePreview,
        _ => ShowAttackPreview,
    };

    // In-game cursor speed multiplier; at 1 the OS cursor is used as is, otherwise a virtual cursor takes over outside menus (see CursorManager).
    public static float MouseSensitivity
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey, 1f), MouseSensitivityMin, MouseSensitivityMax);
        set
        {
            PlayerPrefs.SetFloat(MouseSensitivityKey, Mathf.Clamp(value, MouseSensitivityMin, MouseSensitivityMax));
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    // Right stick tilt needed before the aim turns; below it the aim keeps its last direction. Stacks on top of the stick deadzone.
    public static float AimDeadzone
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(AimDeadzoneKey, 0.4f), AimDeadzoneMin, AimDeadzoneMax);
        set
        {
            PlayerPrefs.SetFloat(AimDeadzoneKey, Mathf.Clamp(value, AimDeadzoneMin, AimDeadzoneMax));
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    // Gamepad only: Attacks bend toward the nearest target close to the aim.
    public static bool AimAssist
    {
        get => PlayerPrefs.GetInt(AimAssistKey, 1) != 0;
        set
        {
            PlayerPrefs.SetInt(AimAssistKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    // Multiplier on how fast the right stick moves a free-aimed ability zone.
    public static float GamepadAimSpeed
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(GamepadAimSpeedKey, 1f), GamepadAimSpeedMin, GamepadAimSpeedMax);
        set
        {
            PlayerPrefs.SetFloat(GamepadAimSpeedKey, Mathf.Clamp(value, GamepadAimSpeedMin, GamepadAimSpeedMax));
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    // Relay region used when hosting; empty means Auto (Relay picks the lowest-latency one).
    public static string RelayRegion
    {
        get => PlayerPrefs.GetString(RelayRegionKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(RelayRegionKey, value ?? string.Empty);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }
}
