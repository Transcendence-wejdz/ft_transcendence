using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

// Receives the signed-in web user's profile via unityInstance.SendMessage("WebProfileBridge", "SetProfile", json) and pushes in-game changes back.
public class WebProfileBridge : MonoBehaviour
{
    [Serializable]
    private class WebProfile
    {
        public string userId;
        public string displayName;
        public string playerClass;
        public string color;
        public string settingsJson;
        public int gold;
        public string upgradesJson;
    }

    [Serializable]
    private class WebSettings
    {
        public bool showEnemyHealthBars;
        public bool screenShake;
        public bool showDamageNumbers;
        public bool showFps;
        public bool showPing;
        public string keybinds;
    }

    [Serializable]
    private class SelectionPatch
    {
        public string @class;
        public string color;
    }

    [Serializable]
    private class NamePatch
    {
        public string display_name;
    }

    [Serializable]
    private class SettingsPatch
    {
        public WebSettings settings;
    }

    // Same order as CharacterCustomizationMenu.kitsByClass and colorVariants.
    private static readonly string[] ClassNames = { "Archer", "Monk", "Lancer", "Pawn", "Warrior" };
    private static readonly string[] ColorNames = { "Black", "Blue", "Purple", "Red", "Yellow" };

    private const float SettingsPushDelay = 0.5f;

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
            return id.Length > 30 ? id.Substring(0, 30) : id;
        }
    }

    // Spawns the persistent receiver under the exact name the web page targets.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindAnyObjectByType<WebProfileBridge>() != null)
            return;

        GameObject go = new GameObject("WebProfileBridge");
        go.AddComponent<WebProfileBridge>();
        DontDestroyOnLoad(go);
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

    // Waits (unscaled) until the web page has sent the profile, or the timeout elapses.
    public static async Task<bool> WaitForProfileAsync(float timeoutSeconds)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (!HasProfile && Time.realtimeSinceStartup < deadline)
            await Awaitable.NextFrameAsync();
        return HasProfile;
    }

    // Saves the profile so later spawns load it, then applies it to the current local player.
    public void SetProfile(string json)
    {
        WebProfile profile;
        try
        {
            profile = JsonUtility.FromJson<WebProfile>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"WebProfileBridge: invalid profile JSON. {e.Message}");
            return;
        }
        if (profile == null)
            return;

        PlayerProfileStore.TryLoad(out int classIndex, out int colorIndex, out string playerName);
        int webClass = Array.IndexOf(ClassNames, profile.playerClass);
        int webColor = Array.IndexOf(ColorNames, profile.color);
        if (webClass >= 0)
            classIndex = webClass;
        if (webColor >= 0)
            colorIndex = webColor;
        if (!string.IsNullOrWhiteSpace(profile.displayName))
            playerName = profile.displayName.Trim();

        UserId = profile.userId ?? string.Empty;
        ConfirmedName = playerName;
        confirmedClass = classIndex;
        confirmedColor = colorIndex;
        HasProfile = true;
        GoldWallet.SetFromServer(profile.gold);
        PermanentUpgrades.SetFromServer(profile.upgradesJson);

        PlayerProfileStore.Save(classIndex, colorIndex, playerName);

        applyingRemote = true;
        try
        {
            ApplySettings(profile.settingsJson);

            CharacterCustomizationMenu menu = CharacterCustomizationMenu.Instance;
            if (menu == null || LocalPlayer.Get() == null)
                return;

            menu.ApplyProfile(classIndex, colorIndex);
            if (!string.IsNullOrEmpty(playerName))
                menu.SetPlayerName(playerName);
        }
        finally
        {
            applyingRemote = false;
        }
    }

    // Overwrites local settings with the account's saved ones; missing keys keep their current value.
    private static void ApplySettings(string json)
    {
        if (string.IsNullOrEmpty(json))
            return;

        WebSettings settings = CurrentSettings();
        try
        {
            JsonUtility.FromJsonOverwrite(json, settings);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"WebProfileBridge: invalid settings JSON. {e.Message}");
            return;
        }

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

    // Saves a class/color change made in-game to the account, skipping values the backend already has.
    public static void PushSelection(int classIndex, int colorIndex)
    {
        if (!HasProfile || applyingRemote || (classIndex == confirmedClass && colorIndex == confirmedColor))
            return;
        if (classIndex < 0 || classIndex >= ClassNames.Length || colorIndex < 0 || colorIndex >= ColorNames.Length)
            return;

        string body = JsonUtility.ToJson(new SelectionPatch { @class = ClassNames[classIndex], color = ColorNames[colorIndex] });
        WebBackend.Patch(body, status =>
        {
            if (status < 200 || status >= 300)
                return;
            confirmedClass = classIndex;
            confirmedColor = colorIndex;
        });
    }

    // Saves an in-game rename to the account; a rejected name (taken/invalid) reverts to the account's name.
    public static void PushName(string playerName)
    {
        if (!HasProfile || applyingRemote || string.IsNullOrEmpty(playerName) || playerName == ConfirmedName)
            return;

        WebBackend.Patch(JsonUtility.ToJson(new NamePatch { display_name = playerName }), status =>
        {
            if (status >= 200 && status < 300)
            {
                ConfirmedName = playerName;
                _ = ServicesAuth.SyncPlayerNameAsync();
            }
            else if (status == 400 || status == 409)
            {
                Debug.LogWarning($"WebProfileBridge: name \"{playerName}\" rejected by the server (HTTP {status}), reverting.");
                CharacterCustomizationMenu menu = CharacterCustomizationMenu.Instance;
                if (menu != null)
                    menu.SetPlayerName(ConfirmedName);
            }
        });
    }

    // Debounced so a burst of rebinds/toggles sends a single request.
    private void QueueSettingsPush()
    {
        if (!HasProfile || applyingRemote)
            return;

        if (settingsPush != null)
            StopCoroutine(settingsPush);
        settingsPush = StartCoroutine(PushSettingsAfterDelay());
    }

    private IEnumerator PushSettingsAfterDelay()
    {
        yield return new WaitForSecondsRealtime(SettingsPushDelay);
        settingsPush = null;
        WebBackend.Patch(JsonUtility.ToJson(new SettingsPatch { settings = CurrentSettings() }));
    }
}
