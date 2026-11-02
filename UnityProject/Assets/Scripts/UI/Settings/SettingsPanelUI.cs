using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Settings panel: gameplay preference toggles and graphics options, synced from GameSettings whenever the panel opens.
public partial class SettingsPanelUI : MonoBehaviour
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
    [SerializeField] private Toggle[] cursorStyleOptions;
    [SerializeField] private Toggle[] cursorColorOptions;
    [SerializeField] private Image crosshairPreviewFill;
    [SerializeField] private Image crosshairPreviewOutline;
    [Tooltip("Preview size at 100% Cursor Size.")]
    [SerializeField] private float crosshairPreviewSize = 32f;
    [Tooltip("Largest preview size, so big Cursor Sizes do not spill far over the rows around it.")]
    [SerializeField] private float crosshairPreviewMaxSize = 44f;
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

    private static string PercentText(float value)
    {
        return Mathf.RoundToInt(value * 100f) + "%";
    }

    private static void BindToggle(Toggle toggle, System.Action<bool> apply)
    {
        toggle.onValueChanged.AddListener(value => apply(value));
    }

    // Radio group: the option turned on writes its index.
    private static void BindOption(Toggle option, int index, System.Action<int> apply)
    {
        option.onValueChanged.AddListener(isOn => ApplyIfOn(isOn, index, apply));
    }

    private static void ApplyIfOn(bool isOn, int index, System.Action<int> apply)
    {
        if (isOn)
            apply(index);
    }

    private static void SyncOptions(Toggle[] options, int selected)
    {
        for (int i = 0; i < options.Length; i++)
            options[i].SetIsOnWithoutNotify(i == selected);
    }

    // Style options show an "Icon" image of their crosshair instead of a name.
    private static void SetStyleIcon(Toggle option, int style)
    {
        Transform icon = option.transform.Find("Icon");

        if (icon != null)
            icon.GetComponent<Image>().sprite = CursorManager.CrosshairSprite(style, false);
    }

    // Color options show a "Swatch" image instead of a name; its tint comes from the same palette as the crosshair.
    private static void TintSwatch(Toggle option, int color)
    {
        Transform swatch = option.transform.Find("Swatch");

        if (swatch == null || color >= GameSettings.CursorColorValues.Length)
            return;
        swatch.GetComponent<Image>().color = GameSettings.CursorColorValues[color];
    }

    private static void ApplySliderStep(float steps, float step, TMP_Text label, System.Action<float> apply)
    {
        apply(steps * step);
        label.text = PercentText(steps * step);
    }

    // Whole-step slider showing its value as a percentage, so mouse drag and gamepad left/right land on the same values.
    private static void BindPercentSlider(Slider slider, TMP_Text label, float min, float max, float step, System.Action<float> apply)
    {
        slider.wholeNumbers = true;
        slider.minValue = Mathf.Round(min / step);
        slider.maxValue = Mathf.Round(max / step);
        slider.onValueChanged.AddListener(steps => ApplySliderStep(steps, step, label, apply));
    }

    private static void SyncPercentSlider(Slider slider, TMP_Text label, float value, float step)
    {
        slider.SetValueWithoutNotify(Mathf.Round(value / step));
        label.text = PercentText(value);
    }

    private static void SetCrosshairImage(Image image, Sprite sprite, Color color, Vector2 size)
    {
        image.sprite = sprite;
        image.color = color;
        image.rectTransform.sizeDelta = size;
    }

    // The crosshair as it shows in game: chosen style and color, outlined, scaled by Cursor Size.
    private void RefreshCrosshairPreview()
    {
        int style = GameSettings.CursorStyle;
        Vector2 size = Vector2.one * Mathf.Min(crosshairPreviewSize * GameSettings.CursorSize, crosshairPreviewMaxSize);

        if (crosshairPreviewFill == null || crosshairPreviewOutline == null)
            return;
        SetCrosshairImage(crosshairPreviewFill, CursorManager.CrosshairSprite(style, false), CursorManager.CrosshairColor(), size);
        SetCrosshairImage(crosshairPreviewOutline, CursorManager.CrosshairSprite(style, true), CursorManager.CrosshairOutlineTint, size);
    }

    private void Awake()
    {
        BindToggles();
        BindOptionGroups();
        BindSliders();
    }

    private void OnEnable()
    {
        GameSettings.OnChanged += RefreshCrosshairPreview;
        RefreshCrosshairPreview();
        SyncToggles();
        SyncOptions(qualityOptions, GameSettings.GraphicsQuality);
        SyncOptions(cursorStyleOptions, GameSettings.CursorStyle);
        SyncOptions(cursorColorOptions, GameSettings.CursorColor);
        SyncSliders();
    }

    private void OnDisable()
    {
        GameSettings.OnChanged -= RefreshCrosshairPreview;
    }
}
