using UnityEngine;

// The local player's class, color and name choices, with change events for the HUD; usable from any scene.
public static class LocalCharacterLook
{
    public const int MaxNameLength = 24;

    public static event System.Action<Sprite> OnPortraitChanged;
    public static event System.Action<Sprite> OnBackgroundChanged;
    public static event System.Action<string> OnNameChanged;

    // Both the local player and the catalog are needed for any look query.
    private static bool TryGetLook(out GameObject player, out CharacterCatalog catalog)
    {
        player = LocalPlayer.Get();
        catalog = CharacterCatalog.Instance;
        return player != null && catalog != null;
    }

    private static void RaiseLook(CharacterCatalog catalog, int classIndex, int colorIndex)
    {
        Sprite background = catalog.GetBackground(colorIndex);

        OnPortraitChanged?.Invoke(catalog.GetPortrait(classIndex, colorIndex));
        if (background != null)
            OnBackgroundChanged?.Invoke(background);
    }

    private static void Apply(GameObject player, int classIndex, int colorIndex)
    {
        CharacterCatalog catalog = CharacterCatalog.Instance;
        Player customization;

        if (catalog == null || !catalog.ApplyVisuals(player, classIndex, colorIndex))
            return;
        RaiseLook(catalog, classIndex, colorIndex);
        customization = player.GetComponent<Player>();
        if (customization != null)
            customization.SetSelection(classIndex, colorIndex);
    }

    // Trimmed and cut to MaxNameLength; null when nothing is left.
    private static string CleanName(string value)
    {
        if (value == null)
            return null;
        value = value.Trim();
        if (string.IsNullOrEmpty(value))
            return null;
        if (value.Length > MaxNameLength)
            value = value.Substring(0, MaxNameLength);
        return value;
    }

    public static Sprite CurrentPortrait()
    {
        GameObject player;
        CharacterCatalog catalog;

        if (!TryGetLook(out player, out catalog))
            return null;
        return catalog.GetPortrait(catalog.ClassIndexOf(player), catalog.ColorIndexOf(player));
    }

    public static Sprite CurrentBackground()
    {
        GameObject player;
        CharacterCatalog catalog;

        if (!TryGetLook(out player, out catalog))
            return null;
        return catalog.GetBackground(catalog.ColorIndexOf(player));
    }

    public static string CurrentName()
    {
        Player customization = LocalPlayer.GetPlayer();

        if (customization == null)
            return string.Empty;
        return customization.PlayerName;
    }

    public static void SelectClass(int index)
    {
        GameObject player;
        CharacterCatalog catalog;

        if (TryGetLook(out player, out catalog))
            Apply(player, index, catalog.ColorIndexOf(player));
    }

    public static void SelectColor(int index)
    {
        GameObject player;
        CharacterCatalog catalog;

        if (TryGetLook(out player, out catalog))
            Apply(player, catalog.ClassIndexOf(player), index);
    }

    // Sets class and color together, e.g. from the web profile.
    public static void ApplyProfile(int classIndex, int colorIndex)
    {
        GameObject player = LocalPlayer.Get();

        if (player != null)
            Apply(player, classIndex, colorIndex);
    }

    // Trims and clamps the name, applies it to the local player and returns what was kept (null if rejected).
    public static string SetPlayerName(string value)
    {
        GameObject player;
        Player customization;

        value = CleanName(value);
        if (value == null)
            return null;
        player = LocalPlayer.Get();
        if (player == null)
            return null;
        customization = player.GetComponent<Player>();
        if (customization != null)
            customization.SetName(value);
        OnNameChanged?.Invoke(value);
        return value;
    }

    // Re-raises every event for a freshly loaded local player.
    public static void NotifyProfileLoaded(GameObject player)
    {
        CharacterCatalog catalog = CharacterCatalog.Instance;
        int classIndex;
        int colorIndex;
        Player customization;

        if (catalog == null)
            return;
        classIndex = catalog.ClassIndexOf(player);
        colorIndex = catalog.ColorIndexOf(player);
        if (!catalog.IsValid(classIndex, colorIndex))
            return;
        RaiseLook(catalog, classIndex, colorIndex);
        customization = player.GetComponent<Player>();
        if (customization != null && !string.IsNullOrEmpty(customization.PlayerName))
            OnNameChanged?.Invoke(customization.PlayerName);
    }
}
