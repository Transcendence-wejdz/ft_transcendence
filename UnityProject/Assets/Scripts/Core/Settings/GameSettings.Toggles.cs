using UnityEngine;

// On/off preferences: HUD readouts, feedback, aim previews and aim assist.
public static partial class GameSettings
{
    public static bool ShowEnemyHealthBars
    {
        get => GetBool(ShowEnemyHealthBarsKey, true);
        set => SetBool(ShowEnemyHealthBarsKey, value);
    }

    public static bool ScreenShakeEnabled
    {
        get => GetBool(ScreenShakeEnabledKey, true);
        set => SetBool(ScreenShakeEnabledKey, value);
    }

    public static bool ShowDamageNumbers
    {
        get => GetBool(ShowDamageNumbersKey, true);
        set => SetBool(ShowDamageNumbersKey, value);
    }

    public static bool ShowFps
    {
        get => GetBool(ShowFpsKey, true);
        set => SetBool(ShowFpsKey, value);
    }

    public static bool ShowPing
    {
        get => GetBool(ShowPingKey, true);
        set => SetBool(ShowPingKey, value);
    }

    public static bool HdrEnabled
    {
        get => GetBool(HdrEnabledKey, !IsWebGL);
        set => SetBool(HdrEnabledKey, value);
    }

    public static bool ShowAttackPreview
    {
        get => GetBool(ShowAttackPreviewKey, true);
        set => SetBool(ShowAttackPreviewKey, value);
    }

    public static bool ShowSpecialPreview
    {
        get => GetBool(ShowSpecialPreviewKey, true);
        set => SetBool(ShowSpecialPreviewKey, value);
    }

    public static bool ShowUltimatePreview
    {
        get => GetBool(ShowUltimatePreviewKey, true);
        set => SetBool(ShowUltimatePreviewKey, value);
    }

    // Whether the hold-aim preview of an action is drawn; the hold-to-cast behaviour stays either way.
    public static bool ShowAimPreview(AbilityType ability)
    {
        if (ability == AbilityType.Special)
            return ShowSpecialPreview;
        if (ability == AbilityType.Ultimate)
            return ShowUltimatePreview;
        return ShowAttackPreview;
    }

    // Gamepad only: Attacks bend toward the nearest target close to the aim.
    public static bool AimAssist
    {
        get => GetBool(AimAssistKey, true);
        set => SetBool(AimAssistKey, value);
    }
}
