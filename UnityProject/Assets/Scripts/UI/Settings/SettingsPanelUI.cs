using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Settings panel: gameplay preference toggles and graphics options, synced from GameSettings whenever the panel opens.
public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private Toggle enemyHealthBarsToggle;
    [SerializeField] private Toggle screenShakeToggle;
    [SerializeField] private Toggle damageNumbersToggle;
    [SerializeField] private Toggle showFpsToggle;
    [SerializeField] private Toggle showPingToggle;
    [SerializeField] private Toggle[] qualityOptions;
    [SerializeField] private Toggle hdrToggle;
    [SerializeField] private Slider deadzoneSlider;
    [SerializeField] private TMP_Text deadzoneLabel;

    private void Awake()
    {
        enemyHealthBarsToggle.onValueChanged.AddListener(value => GameSettings.ShowEnemyHealthBars = value);
        screenShakeToggle.onValueChanged.AddListener(value => GameSettings.ScreenShakeEnabled = value);
        damageNumbersToggle.onValueChanged.AddListener(value => GameSettings.ShowDamageNumbers = value);
        showFpsToggle.onValueChanged.AddListener(value => GameSettings.ShowFps = value);
        showPingToggle.onValueChanged.AddListener(value => GameSettings.ShowPing = value);
        hdrToggle.onValueChanged.AddListener(value => GameSettings.HdrEnabled = value);

        for (int i = 0; i < qualityOptions.Length; i++)
        {
            int level = i;
            qualityOptions[i].onValueChanged.AddListener(isOn => { if (isOn) GameSettings.GraphicsQuality = level; });
        }

        deadzoneSlider.minValue = Mathf.Round(GameSettings.StickDeadzoneMin / GameSettings.StickDeadzoneStep);
        deadzoneSlider.maxValue = Mathf.Round(GameSettings.StickDeadzoneMax / GameSettings.StickDeadzoneStep);
        deadzoneSlider.wholeNumbers = true;
        deadzoneSlider.onValueChanged.AddListener(OnDeadzoneChanged);
    }

    private void OnEnable()
    {
        enemyHealthBarsToggle.SetIsOnWithoutNotify(GameSettings.ShowEnemyHealthBars);
        screenShakeToggle.SetIsOnWithoutNotify(GameSettings.ScreenShakeEnabled);
        damageNumbersToggle.SetIsOnWithoutNotify(GameSettings.ShowDamageNumbers);
        showFpsToggle.SetIsOnWithoutNotify(GameSettings.ShowFps);
        showPingToggle.SetIsOnWithoutNotify(GameSettings.ShowPing);
        hdrToggle.SetIsOnWithoutNotify(GameSettings.HdrEnabled);

        for (int i = 0; i < qualityOptions.Length; i++)
            qualityOptions[i].SetIsOnWithoutNotify(i == GameSettings.GraphicsQuality);

        deadzoneSlider.SetValueWithoutNotify(Mathf.Round(GameSettings.StickDeadzone / GameSettings.StickDeadzoneStep));
        RefreshDeadzone();
    }

    // Slider works in whole steps so mouse drag and gamepad left/right land on the same values.
    private void OnDeadzoneChanged(float steps)
    {
        GameSettings.StickDeadzone = steps * GameSettings.StickDeadzoneStep;
        RefreshDeadzone();
    }

    private void RefreshDeadzone()
    {
        deadzoneLabel.text = Mathf.RoundToInt(GameSettings.StickDeadzone * 100f) + "%";
    }
}
