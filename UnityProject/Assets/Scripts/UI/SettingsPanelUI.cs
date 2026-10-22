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
    [SerializeField] private Button qualityPreviousButton;
    [SerializeField] private Button qualityNextButton;
    [SerializeField] private TMP_Text qualityLabel;
    [SerializeField] private Toggle hdrToggle;

    private void Awake()
    {
        enemyHealthBarsToggle.onValueChanged.AddListener(value => GameSettings.ShowEnemyHealthBars = value);
        screenShakeToggle.onValueChanged.AddListener(value => GameSettings.ScreenShakeEnabled = value);
        damageNumbersToggle.onValueChanged.AddListener(value => GameSettings.ShowDamageNumbers = value);
        showFpsToggle.onValueChanged.AddListener(value => GameSettings.ShowFps = value);
        showPingToggle.onValueChanged.AddListener(value => GameSettings.ShowPing = value);
        qualityPreviousButton.onClick.AddListener(() => StepQuality(-1));
        qualityNextButton.onClick.AddListener(() => StepQuality(1));
        hdrToggle.onValueChanged.AddListener(value => GameSettings.HdrEnabled = value);
    }

    private void OnEnable()
    {
        enemyHealthBarsToggle.SetIsOnWithoutNotify(GameSettings.ShowEnemyHealthBars);
        screenShakeToggle.SetIsOnWithoutNotify(GameSettings.ScreenShakeEnabled);
        damageNumbersToggle.SetIsOnWithoutNotify(GameSettings.ShowDamageNumbers);
        showFpsToggle.SetIsOnWithoutNotify(GameSettings.ShowFps);
        showPingToggle.SetIsOnWithoutNotify(GameSettings.ShowPing);
        hdrToggle.SetIsOnWithoutNotify(GameSettings.HdrEnabled);
        RefreshQuality();
    }

    // Wraps around, so the arrows cycle Low, Medium, High, Ultra.
    private void StepQuality(int direction)
    {
        int count = GameSettings.GraphicsQualityNames.Length;
        GameSettings.GraphicsQuality = (GameSettings.GraphicsQuality + direction + count) % count;
        RefreshQuality();
    }

    private void RefreshQuality()
    {
        qualityLabel.text = GameSettings.GraphicsQualityNames[GameSettings.GraphicsQuality];
    }
}
