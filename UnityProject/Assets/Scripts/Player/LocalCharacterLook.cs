using UnityEngine;

// The local player's class, color and name choices, with change events for the HUD; usable from any scene.
public static class LocalCharacterLook
{
    public const int MaxNameLength = 24;

    public static event System.Action<Sprite> OnPortraitChanged;
    public static event System.Action<Sprite> OnBackgroundChanged;
    public static event System.Action<string> OnNameChanged;

    public static Sprite CurrentPortrait()
    {
        GameObject player = LocalPlayer.Get();
        CharacterCatalog catalog = CharacterCatalog.Instance;
        if (player == null || catalog == null)
            return null;

        return catalog.GetPortrait(catalog.ClassIndexOf(player), catalog.ColorIndexOf(player));
    }

    public static Sprite CurrentBackground()
    {
        GameObject player = LocalPlayer.Get();
        CharacterCatalog catalog = CharacterCatalog.Instance;
        if (player == null || catalog == null)
            return null;

        return catalog.GetBackground(catalog.ColorIndexOf(player));
    }

    public static string CurrentName()
    {
        GameObject player = LocalPlayer.Get();
        Player customization = player != null ? player.GetComponent<Player>() : null;
        return customization != null ? customization.PlayerName : string.Empty;
    }

    public static void SelectClass(int index)
    {
        GameObject player = LocalPlayer.Get();
        CharacterCatalog catalog = CharacterCatalog.Instance;
        if (player == null || catalog == null)
            return;

        Apply(player, index, catalog.ColorIndexOf(player));
    }

    public static void SelectColor(int index)
    {
        GameObject player = LocalPlayer.Get();
        CharacterCatalog catalog = CharacterCatalog.Instance;
        if (player == null || catalog == null)
            return;

        Apply(player, catalog.ClassIndexOf(player), index);
    }

    // Sets class and color together, e.g. from the web profile.
    public static void ApplyProfile(int classIndex, int colorIndex)
    {
        GameObject player = LocalPlayer.Get();
        if (player == null)
            return;

        Apply(player, classIndex, colorIndex);
    }

    // Trims and clamps the name, applies it to the local player and returns what was kept (null if rejected).
    public static string SetPlayerName(string value)
    {
        value = value != null ? value.Trim() : string.Empty;
        if (string.IsNullOrEmpty(value))
            return null;
        if (value.Length > MaxNameLength)
            value = value.Substring(0, MaxNameLength);

        GameObject player = LocalPlayer.Get();
        if (player == null)
            return null;

        Player customization = player.GetComponent<Player>();
        if (customization != null)
            customization.SetName(value);

        OnNameChanged?.Invoke(value);
        return value;
    }

    // Re-raises every event for a freshly loaded local player.
    public static void NotifyProfileLoaded(GameObject player)
    {
        CharacterCatalog catalog = CharacterCatalog.Instance;
        if (catalog == null)
            return;

        int classIndex = catalog.ClassIndexOf(player);
        int colorIndex = catalog.ColorIndexOf(player);
        if (!catalog.IsValid(classIndex, colorIndex))
            return;

        RaiseLook(catalog, classIndex, colorIndex);

        Player customization = player.GetComponent<Player>();
        if (customization != null && !string.IsNullOrEmpty(customization.PlayerName))
            OnNameChanged?.Invoke(customization.PlayerName);
    }

    private static void Apply(GameObject player, int classIndex, int colorIndex)
    {
        CharacterCatalog catalog = CharacterCatalog.Instance;
        if (catalog == null || !catalog.ApplyVisuals(player, classIndex, colorIndex))
            return;

        RaiseLook(catalog, classIndex, colorIndex);

        Player customization = player.GetComponent<Player>();
        if (customization != null)
            customization.SetSelection(classIndex, colorIndex);
    }

    private static void RaiseLook(CharacterCatalog catalog, int classIndex, int colorIndex)
    {
        OnPortraitChanged?.Invoke(catalog.GetPortrait(classIndex, colorIndex));
        Sprite background = catalog.GetBackground(colorIndex);
        if (background != null)
            OnBackgroundChanged?.Invoke(background);
    }
}
