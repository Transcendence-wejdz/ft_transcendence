using UnityEngine;

// Display and effects preferences applied by GraphicsQualityApplier: frame cap, VSync, FX glow lights and bloom.
public static partial class GameSettings
{
    private const string FpsLimitKey = "Settings.FpsLimit";
    private const string VSyncKey = "Settings.VSync";
    private const string FxGlowKey = "Settings.FxGlow";
    private const string BloomKey = "Settings.Bloom";

    public static readonly string[] FpsLimitNames = { "30", "60", "120", "Max" };
    public static readonly int[] FpsLimitValues = { 30, 60, 120, -1 };

    // Browsers ignore VSync and the swap interval, so it only exists on desktop.
    public static bool IsDesktop => !IsWebGL;

    // Index into FpsLimitNames; Max lets the display (or VSync) set the pace.
    public static int FpsLimit
    {
        get => GetIndex(FpsLimitKey, FpsLimitNames.Length - 1, FpsLimitNames.Length);
        set => SetIndex(FpsLimitKey, value, FpsLimitNames.Length);
    }

    public static bool VSync
    {
        get => GetBool(VSyncKey, true);
        set => SetBool(VSyncKey, value);
    }

    // Lights cast by ability FX; turning them off is the biggest lighting saving in crowded fights.
    public static bool FxGlow
    {
        get => GetBool(FxGlowKey, true);
        set => SetBool(FxGlowKey, value);
    }

    public static bool Bloom
    {
        get => GetBool(BloomKey, !IsWebGL);
        set => SetBool(BloomKey, value);
    }
}
