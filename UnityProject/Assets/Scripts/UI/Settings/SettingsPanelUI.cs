using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Settings panel: gameplay preference toggles and graphics options, synced from GameSettings whenever the panel opens.
public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private Toggle enemyHealthBarsToggle;
    [SerializeField] private Toggle screenShakeToggle;
    [SerializeField] private Toggle attackPreviewToggle;
    [SerializeField] private Toggle specialPreviewToggle;
    [SerializeField] private Toggle ultimatePreviewToggle;
    [SerializeField] private Toggle damageNumbersToggle;
    [SerializeField] private Toggle showFpsToggle;
    [SerializeField] private Toggle showPingToggle;
    [SerializeField] private Toggle[] qualityOptions;
    [SerializeField] private Toggle hdrToggle;
    [SerializeField] private Toggle aimAssistToggle;
    [SerializeField] private Slider aimDeadzoneSlider;
    [SerializeField] private TMP_Text aimDeadzoneLabel;
    [SerializeField] private Slider deadzoneSlider;
    [SerializeField] private TMP_Text deadzoneLabel;
    [SerializeField] private Slider cameraDistanceSlider;
    [SerializeField] private TMP_Text cameraDistanceLabel;
    [SerializeField] private Slider cursorSizeSlider;
    [SerializeField] private TMP_Text cursorSizeLabel;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TMP_Text mouseSensitivityLabel;
    [SerializeField] private Slider aimSpeedSlider;
    [SerializeField] private TMP_Text aimSpeedLabel;

    private void Awake()
    {
        enemyHealthBarsToggle.onValueChanged.AddListener(value => GameSettings.ShowEnemyHealthBars = value);
        screenShakeToggle.onValueChanged.AddListener(value => GameSettings.ScreenShakeEnabled = value);
        attackPreviewToggle.onValueChanged.AddListener(value => GameSettings.ShowAttackPreview = value);
        specialPreviewToggle.onValueChanged.AddListener(value => GameSettings.ShowSpecialPreview = value);
        ultimatePreviewToggle.onValueChanged.AddListener(value => GameSettings.ShowUltimatePreview = value);
        damageNumbersToggle.onValueChanged.AddListener(value => GameSettings.ShowDamageNumbers = value);
        showFpsToggle.onValueChanged.AddListener(value => GameSettings.ShowFps = value);
        showPingToggle.onValueChanged.AddListener(value => GameSettings.ShowPing = value);
        hdrToggle.onValueChanged.AddListener(value => GameSettings.HdrEnabled = value);
        aimAssistToggle.onValueChanged.AddListener(value => GameSettings.AimAssist = value);

        for (int i = 0; i < qualityOptions.Length; i++)
        {
            int level = i;
            qualityOptions[i].onValueChanged.AddListener(isOn => { if (isOn) GameSettings.GraphicsQuality = level; });
        }

        BindPercentSlider(deadzoneSlider, deadzoneLabel, GameSettings.StickDeadzoneMin, GameSettings.StickDeadzoneMax, GameSettings.StickDeadzoneStep, value => GameSettings.StickDeadzone = value);
        BindPercentSlider(cameraDistanceSlider, cameraDistanceLabel, GameSettings.CameraDistanceMin, GameSettings.CameraDistanceMax, GameSettings.CameraDistanceStep, value => GameSettings.CameraDistance = value);
        BindPercentSlider(cursorSizeSlider, cursorSizeLabel, GameSettings.CursorSizeMin, GameSettings.CursorSizeMax, GameSettings.CursorSizeStep, value => GameSettings.CursorSize = value);
        BindPercentSlider(aimDeadzoneSlider, aimDeadzoneLabel, GameSettings.AimDeadzoneMin, GameSettings.AimDeadzoneMax, GameSettings.AimDeadzoneStep, value => GameSettings.AimDeadzone = value);
        BindPercentSlider(mouseSensitivitySlider, mouseSensitivityLabel, GameSettings.MouseSensitivityMin, GameSettings.MouseSensitivityMax, GameSettings.MouseSensitivityStep, value => GameSettings.MouseSensitivity = value);
        BindPercentSlider(aimSpeedSlider, aimSpeedLabel, GameSettings.GamepadAimSpeedMin, GameSettings.GamepadAimSpeedMax, GameSettings.GamepadAimSpeedStep, value => GameSettings.GamepadAimSpeed = value);
    }

    private void OnEnable()
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

        for (int i = 0; i < qualityOptions.Length; i++)
            qualityOptions[i].SetIsOnWithoutNotify(i == GameSettings.GraphicsQuality);

        SyncPercentSlider(deadzoneSlider, deadzoneLabel, GameSettings.StickDeadzone, GameSettings.StickDeadzoneStep);
        SyncPercentSlider(cameraDistanceSlider, cameraDistanceLabel, GameSettings.CameraDistance, GameSettings.CameraDistanceStep);
        SyncPercentSlider(cursorSizeSlider, cursorSizeLabel, GameSettings.CursorSize, GameSettings.CursorSizeStep);
        SyncPercentSlider(aimDeadzoneSlider, aimDeadzoneLabel, GameSettings.AimDeadzone, GameSettings.AimDeadzoneStep);
        SyncPercentSlider(mouseSensitivitySlider, mouseSensitivityLabel, GameSettings.MouseSensitivity, GameSettings.MouseSensitivityStep);
        SyncPercentSlider(aimSpeedSlider, aimSpeedLabel, GameSettings.GamepadAimSpeed, GameSettings.GamepadAimSpeedStep);
    }

    // Whole-step slider showing its value as a percentage, so mouse drag and gamepad left/right land on the same values.
    private static void BindPercentSlider(Slider slider, TMP_Text label, float min, float max, float step, System.Action<float> apply)
    {
        slider.wholeNumbers = true;
        slider.minValue = Mathf.Round(min / step);
        slider.maxValue = Mathf.Round(max / step);
        slider.onValueChanged.AddListener(steps =>
        {
            apply(steps * step);
            label.text = Mathf.RoundToInt(steps * step * 100f) + "%";
        });
    }

    private static void SyncPercentSlider(Slider slider, TMP_Text label, float value, float step)
    {
        slider.SetValueWithoutNotify(Mathf.Round(value / step));
        label.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
