using UnityEngine;

// Local, per-client gameplay preferences; not synced to other players.
public static partial class GameSettings
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
    private const string CursorStyleKey = "Settings.CursorStyle";
    private const string CursorColorKey = "Settings.CursorColor";
    private const string RelayRegionKey = "Settings.RelayRegion";
    private const string DefaultRelayRegion = "europe-west4";
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
    public static readonly string[] CursorStyleNames = { "Cross", "Target", "Dot" };
    public static readonly string[] CursorColorNames = { "White", "Red", "Green", "Yellow", "Cyan" };
    public static readonly Color[] CursorColorValues =
    {
        Color.white,
        new Color(1f, 0.25f, 0.25f),
        new Color(0.35f, 1f, 0.35f),
        new Color(1f, 0.9f, 0.25f),
        new Color(0.3f, 0.95f, 1f),
    };

    public static event System.Action OnChanged;

    // Browsers pay a lot more per pixel, so WebGL starts on High without HDR and everything else on Ultra with HDR.
    private static bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;

    private static void Commit()
    {
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    private static bool GetBool(string key, bool defaultValue)
    {
        int fallback = 0;

        if (defaultValue)
            fallback = 1;
        return PlayerPrefs.GetInt(key, fallback) != 0;
    }

    private static void SetBool(string key, bool value)
    {
        if (value)
            PlayerPrefs.SetInt(key, 1);
        else
            PlayerPrefs.SetInt(key, 0);
        Commit();
    }

    private static float GetFloat(string key, float defaultValue, float min, float max)
    {
        return Mathf.Clamp(PlayerPrefs.GetFloat(key, defaultValue), min, max);
    }

    private static void SetFloat(string key, float value, float min, float max)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp(value, min, max));
        Commit();
    }

    private static int GetIndex(string key, int defaultValue, int count)
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(key, defaultValue), 0, count - 1);
    }

    private static void SetIndex(string key, int value, int count)
    {
        PlayerPrefs.SetInt(key, Mathf.Clamp(value, 0, count - 1));
        Commit();
    }

    private static int DefaultGraphicsQuality()
    {
        if (IsWebGL)
            return 2;
        return 3;
    }

    public static int GraphicsQuality
    {
        get => GetIndex(GraphicsQualityKey, DefaultGraphicsQuality(), GraphicsQualityNames.Length);
        set => SetIndex(GraphicsQualityKey, value, GraphicsQualityNames.Length);
    }

    public static float StickDeadzone
    {
        get => GetFloat(StickDeadzoneKey, 0.15f, StickDeadzoneMin, StickDeadzoneMax);
        set => SetFloat(StickDeadzoneKey, value, StickDeadzoneMin, StickDeadzoneMax);
    }

    // Multiplier on the player camera's base orthographic size.
    public static float CameraDistance
    {
        get => GetFloat(CameraDistanceKey, 1f, CameraDistanceMin, CameraDistanceMax);
        set => SetFloat(CameraDistanceKey, value, CameraDistanceMin, CameraDistanceMax);
    }

    public static float CursorSize
    {
        get => GetFloat(CursorSizeKey, 1f, CursorSizeMin, CursorSizeMax);
        set => SetFloat(CursorSizeKey, value, CursorSizeMin, CursorSizeMax);
    }

    // In-game crosshair shape (index into CursorStyleNames); menus always use the arrow.
    public static int CursorStyle
    {
        get => GetIndex(CursorStyleKey, 0, CursorStyleNames.Length);
        set => SetIndex(CursorStyleKey, value, CursorStyleNames.Length);
    }

    // In-game crosshair color (index into CursorColorNames and CursorColorValues); an out of range save (the old "Player" choice) falls back to white.
    public static int CursorColor
    {
        get
        {
            int color = PlayerPrefs.GetInt(CursorColorKey, 0);

            if (color < 0 || color >= CursorColorNames.Length)
                return 0;
            return color;
        }
        set => SetIndex(CursorColorKey, value, CursorColorNames.Length);
    }

    // In-game cursor speed multiplier; at 1 the OS cursor is used as is, otherwise a virtual cursor takes over outside menus (see CursorManager).
    public static float MouseSensitivity
    {
        get => GetFloat(MouseSensitivityKey, 1f, MouseSensitivityMin, MouseSensitivityMax);
        set => SetFloat(MouseSensitivityKey, value, MouseSensitivityMin, MouseSensitivityMax);
    }

    // Right stick tilt needed before the aim turns; below it the aim keeps its last direction. Stacks on top of the stick deadzone.
    public static float AimDeadzone
    {
        get => GetFloat(AimDeadzoneKey, 0.4f, AimDeadzoneMin, AimDeadzoneMax);
        set => SetFloat(AimDeadzoneKey, value, AimDeadzoneMin, AimDeadzoneMax);
    }

    // Multiplier on how fast the right stick moves a free-aimed ability zone.
    public static float GamepadAimSpeed
    {
        get => GetFloat(GamepadAimSpeedKey, 1f, GamepadAimSpeedMin, GamepadAimSpeedMax);
        set => SetFloat(GamepadAimSpeedKey, value, GamepadAimSpeedMin, GamepadAimSpeedMax);
    }

    // Relay region used when hosting, europe-west4 until one is picked; empty means Auto (Relay picks the lowest-latency one).
    public static string RelayRegion
    {
        get => PlayerPrefs.GetString(RelayRegionKey, DefaultRelayRegion);
        set
        {
            PlayerPrefs.SetString(RelayRegionKey, value ?? string.Empty);
            Commit();
        }
    }
}
