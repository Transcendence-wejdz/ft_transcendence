using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Wiring every control to its GameSettings value, and syncing them back when the panel opens.
public partial class SettingsPanelUI
{
    private void BindToggles()
    {
        BindToggle(enemyHealthBarsToggle, value => GameSettings.ShowEnemyHealthBars = value);
        BindToggle(screenShakeToggle, value => GameSettings.ScreenShakeEnabled = value);
        BindToggle(attackPreviewToggle, value => GameSettings.ShowAttackPreview = value);
        BindToggle(specialPreviewToggle, value => GameSettings.ShowSpecialPreview = value);
        BindToggle(ultimatePreviewToggle, value => GameSettings.ShowUltimatePreview = value);
        BindToggle(damageNumbersToggle, value => GameSettings.ShowDamageNumbers = value);
        BindToggle(showFpsToggle, value => GameSettings.ShowFps = value);
        BindToggle(showPingToggle, value => GameSettings.ShowPing = value);
        BindToggle(hdrToggle, value => GameSettings.HdrEnabled = value);
        BindToggle(aimAssistToggle, value => GameSettings.AimAssist = value);
    }

    private void BindOptionGroups()
    {
        for (int i = 0; i < qualityOptions.Length; i++)
            BindOption(qualityOptions[i], i, level => GameSettings.GraphicsQuality = level);
        for (int i = 0; i < cursorStyleOptions.Length; i++)
        {
            BindOption(cursorStyleOptions[i], i, style => GameSettings.CursorStyle = style);
            SetStyleIcon(cursorStyleOptions[i], i);
        }
        for (int i = 0; i < cursorColorOptions.Length; i++)
        {
            BindOption(cursorColorOptions[i], i, color => GameSettings.CursorColor = color);
            TintSwatch(cursorColorOptions[i], i);
        }
    }

    private void BindSliders()
    {
        BindPercentSlider(deadzoneSlider, deadzoneLabel, GameSettings.StickDeadzoneMin, GameSettings.StickDeadzoneMax, GameSettings.StickDeadzoneStep, value => GameSettings.StickDeadzone = value);
        BindPercentSlider(cameraDistanceSlider, cameraDistanceLabel, GameSettings.CameraDistanceMin, GameSettings.CameraDistanceMax, GameSettings.CameraDistanceStep, value => GameSettings.CameraDistance = value);
        BindPercentSlider(cursorSizeSlider, cursorSizeLabel, GameSettings.CursorSizeMin, GameSettings.CursorSizeMax, GameSettings.CursorSizeStep, value => GameSettings.CursorSize = value);
        BindPercentSlider(aimDeadzoneSlider, aimDeadzoneLabel, GameSettings.AimDeadzoneMin, GameSettings.AimDeadzoneMax, GameSettings.AimDeadzoneStep, value => GameSettings.AimDeadzone = value);
        BindPercentSlider(mouseSensitivitySlider, mouseSensitivityLabel, GameSettings.MouseSensitivityMin, GameSettings.MouseSensitivityMax, GameSettings.MouseSensitivityStep, value => GameSettings.MouseSensitivity = value);
        BindPercentSlider(aimSpeedSlider, aimSpeedLabel, GameSettings.GamepadAimSpeedMin, GameSettings.GamepadAimSpeedMax, GameSettings.GamepadAimSpeedStep, value => GameSettings.GamepadAimSpeed = value);
    }

    private void SyncToggles()
    {
        enemyHealthBarsToggle.SetIsOnWithoutNotify(GameSettings.ShowEnemyHealthBars);
        screenShakeToggle.SetIsOnWithoutNotify(GameSettings.ScreenShakeEnabled);
        attackPreviewToggle.SetIsOnWithoutNotify(GameSettings.ShowAttackPreview);
        specialPreviewToggle.SetIsOnWithoutNotify(GameSettings.ShowSpecialPreview);
        ultimatePreviewToggle.SetIsOnWithoutNotify(GameSettings.ShowUltimatePreview);
        damageNumbersToggle.SetIsOnWithoutNotify(GameSettings.ShowDamageNumbers);
        showFpsToggle.SetIsOnWithoutNotify(GameSettings.ShowFps);
        showPingToggle.SetIsOnWithoutNotify(GameSettings.ShowPing);
        hdrToggle.SetIsOnWithoutNotify(GameSettings.HdrEnabled);
        aimAssistToggle.SetIsOnWithoutNotify(GameSettings.AimAssist);
    }

    private void SyncSliders()
    {
        SyncPercentSlider(deadzoneSlider, deadzoneLabel, GameSettings.StickDeadzone, GameSettings.StickDeadzoneStep);
        SyncPercentSlider(cameraDistanceSlider, cameraDistanceLabel, GameSettings.CameraDistance, GameSettings.CameraDistanceStep);
        SyncPercentSlider(cursorSizeSlider, cursorSizeLabel, GameSettings.CursorSize, GameSettings.CursorSizeStep);
        SyncPercentSlider(aimDeadzoneSlider, aimDeadzoneLabel, GameSettings.AimDeadzone, GameSettings.AimDeadzoneStep);
        SyncPercentSlider(mouseSensitivitySlider, mouseSensitivityLabel, GameSettings.MouseSensitivity, GameSettings.MouseSensitivityStep);
        SyncPercentSlider(aimSpeedSlider, aimSpeedLabel, GameSettings.GamepadAimSpeed, GameSettings.GamepadAimSpeedStep);
    }
}
