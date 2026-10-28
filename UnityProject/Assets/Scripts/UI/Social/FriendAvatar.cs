using UnityEngine;

// Shared look for name-based avatars, so a friend has the same initial and color in every list and popup.
public static class FriendAvatar
{
    // Stable per-name hue, muted so the initial stays readable.
    public static Color ColorFor(string displayName)
    {
        uint hash = 2166136261;
        foreach (char c in displayName ?? string.Empty)
            hash = (hash ^ c) * 16777619;
        return Color.HSVToRGB((hash % 360) / 360f, 0.45f, 0.85f);
    }

    public static string InitialFor(string displayName)
    {
        return string.IsNullOrEmpty(displayName) ? "?" : char.ToUpperInvariant(displayName[0]).ToString();
    }
}
