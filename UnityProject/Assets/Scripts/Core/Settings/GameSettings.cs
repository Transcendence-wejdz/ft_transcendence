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

    public const float StickDeadzoneMin = 0.05f;
    public const float StickDeadzoneMax = 0.5f;
    public const float StickDeadzoneStep = 0.05f;

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
}
