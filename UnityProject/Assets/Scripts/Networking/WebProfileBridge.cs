using System;
using UnityEngine;

// Receives the signed-in web user's profile via unityInstance.SendMessage("WebProfileBridge", "SetProfile", json).
public class WebProfileBridge : MonoBehaviour
{
    [Serializable]
    private class WebProfile
    {
        public string displayName;
        public string playerClass;
        public string color;
    }

    // Same order as CharacterCustomizationMenu.kitsByClass and colorVariants.
    private static readonly string[] ClassNames = { "Archer", "Monk", "Lancer", "Pawn", "Warrior" };
    private static readonly string[] ColorNames = { "Black", "Blue", "Purple", "Red", "Yellow" };

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

        PlayerProfileStore.Save(classIndex, colorIndex, playerName);

        CharacterCustomizationMenu menu = CharacterCustomizationMenu.Instance;
        if (menu == null || LocalPlayer.Get() == null)
            return;

        menu.ApplyProfile(classIndex, colorIndex);
        if (!string.IsNullOrEmpty(playerName))
            menu.SetPlayerName(playerName);
    }
}
