using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

// Receives the signed-in web user's profile via unityInstance.SendMessage("WebProfileBridge", "SetProfile", json) and pushes in-game changes back.
public partial class WebProfileBridge : MonoBehaviour
{
    private const float SettingsPushDelay = 0.5f;
    private const int MaxAuthProfileLength = 30;

    // Same order as CharacterCatalog.kitsByClass and colorVariants.
    private static readonly string[] ClassNames = { "Archer", "Monk", "Lancer", "Pawn", "Warrior" };
    private static readonly string[] ColorNames = { "Black", "Blue", "Purple", "Red", "Yellow" };

    public static bool HasProfile { get; private set; }
    public static string UserId { get; private set; } = string.Empty;
    public static string ConfirmedName { get; private set; } = string.Empty;

    private static int confirmedClass = -1;
    private static int confirmedColor = -1;
    private static bool applyingRemote;

    private Coroutine settingsPush;

    // Unity Auth profile (alphanumeric, max 30 chars) so each web account gets its own anonymous identity in this browser.
    public static string AuthProfileName
    {
        get
        {
            string id = "u" + UserId.Replace("-", "");

            if (id.Length <= MaxAuthProfileLength)
                return id;
            return id.Substring(0, MaxAuthProfileLength);
        }
    }

    // Spawns the persistent receiver under the exact name the web page targets.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go;

        if (FindAnyObjectByType<WebProfileBridge>() != null)
            return;
        go = new GameObject("WebProfileBridge");
        go.AddComponent<WebProfileBridge>();
        DontDestroyOnLoad(go);
    }

    // Waits (unscaled) until the web page has sent the profile, or the timeout elapses.
    public static async Task<bool> WaitForProfileAsync(float timeoutSeconds)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;

        while (!HasProfile && Time.realtimeSinceStartup < deadline)
            await Awaitable.NextFrameAsync();
        return HasProfile;
    }

    // Saves the profile so later spawns load it, then applies it to the current local player.
    private static WebProfile ParseProfile(string json)
    {
        try
        {
            return JsonUtility.FromJson<WebProfile>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"WebProfileBridge: invalid profile JSON. {e.Message}");
            return null;
        }
    }

    // The web values win; anything the account doesn't set keeps the local save.
    private static void MergeWithLocal(WebProfile profile, out int classIndex, out int colorIndex, out string playerName)
    {
        int webClass = Array.IndexOf(ClassNames, profile.playerClass);
        int webColor = Array.IndexOf(ColorNames, profile.color);

        PlayerProfileStore.TryLoad(out classIndex, out colorIndex, out playerName);
        if (webClass >= 0)
            classIndex = webClass;
        if (webColor >= 0)
            colorIndex = webColor;
        if (!string.IsNullOrWhiteSpace(profile.displayName))
            playerName = profile.displayName.Trim();
    }

    private static void Confirm(WebProfile profile, int classIndex, int colorIndex, string playerName)
    {
        UserId = profile.userId ?? string.Empty;
        ConfirmedName = playerName;
        confirmedClass = classIndex;
        confirmedColor = colorIndex;
        HasProfile = true;
        GoldWallet.SetFromServer(profile.gold);
        PermanentUpgrades.SetFromServer(profile.upgradesJson);
    }

    // Flagged as remote so the changes it triggers aren't pushed straight back to the backend.
    private static void ApplyRemote(WebProfile profile, int classIndex, int colorIndex, string playerName)
    {
        applyingRemote = true;
        try
        {
            ApplySettings(profile.settingsJson);
            if (LocalPlayer.Get() == null)
                return;
            LocalCharacterLook.ApplyProfile(classIndex, colorIndex);
            if (!string.IsNullOrEmpty(playerName))
                LocalCharacterLook.SetPlayerName(playerName);
        }
        finally
        {
            applyingRemote = false;
        }
    }

    // Saves the profile so later spawns load it, then applies it to the current local player.
    public void SetProfile(string json)
    {
        WebProfile profile = ParseProfile(json);
        int classIndex;
        int colorIndex;
        string playerName;

        if (profile == null)
            return;
        MergeWithLocal(profile, out classIndex, out colorIndex, out playerName);
        Confirm(profile, classIndex, colorIndex, playerName);
        PlayerProfileStore.Save(classIndex, colorIndex, playerName);
        ApplyRemote(profile, classIndex, colorIndex, playerName);
    }

    // Overwrites local settings with the account's saved ones; missing keys keep their current value.
    private static void ApplySettings(string json)
    {
        WebSettings settings;

        if (string.IsNullOrEmpty(json))
            return;
        settings = CurrentSettings();
        try
        {
            JsonUtility.FromJsonOverwrite(json, settings);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"WebProfileBridge: invalid settings JSON. {e.Message}");
            return;
        }
        WriteSettings(settings);
    }

    // Only touches values that differ, so unchanged settings don't fire OnChanged.
    private static void WriteSettings(WebSettings settings)
    {
        if (GameSettings.ShowEnemyHealthBars != settings.showEnemyHealthBars)
            GameSettings.ShowEnemyHealthBars = settings.showEnemyHealthBars;
        if (GameSettings.ScreenShakeEnabled != settings.screenShake)
            GameSettings.ScreenShakeEnabled = settings.screenShake;
        if (GameSettings.ShowDamageNumbers != settings.showDamageNumbers)
            GameSettings.ShowDamageNumbers = settings.showDamageNumbers;
        if (GameSettings.ShowFps != settings.showFps)
            GameSettings.ShowFps = settings.showFps;
        if (GameSettings.ShowPing != settings.showPing)
            GameSettings.ShowPing = settings.showPing;
        if ((settings.keybinds ?? "") != KeybindOverrides.Json)
            KeybindOverrides.SetJson(settings.keybinds);
    }

    private static WebSettings CurrentSettings()
    {
        return new WebSettings
        {
            showEnemyHealthBars = GameSettings.ShowEnemyHealthBars,
            screenShake = GameSettings.ScreenShakeEnabled,
            showDamageNumbers = GameSettings.ShowDamageNumbers,
            showFps = GameSettings.ShowFps,
            showPing = GameSettings.ShowPing,
            keybinds = KeybindOverrides.Json
        };
    }

    private void OnEnable()
    {
        GameSettings.OnChanged += QueueSettingsPush;
        KeybindOverrides.OnSaved += QueueSettingsPush;
    }

    private void OnDisable()
    {
        GameSettings.OnChanged -= QueueSettingsPush;
        KeybindOverrides.OnSaved -= QueueSettingsPush;
    }
}
