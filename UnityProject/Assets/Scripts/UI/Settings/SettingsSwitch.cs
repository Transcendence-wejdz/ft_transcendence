using UnityEngine;
using UnityEngine.UI;

// Pill switch look for a Toggle: animates the knob across and blends the track color on value change.
[RequireComponent(typeof(Toggle))]
public class SettingsSwitch : MonoBehaviour
{
    [SerializeField] private Image track;
    [SerializeField] private RectTransform knob;
    [SerializeField] private Color onColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color offColor = new Color(0.22f, 0.22f, 0.24f, 1f);
    [SerializeField] private float knobTravel = 8f;
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private float knobPop = 1.2f;

    private Toggle toggle;
    private bool shown;
    private float progress;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
    }

    // Opens on the current value without replaying the slide.
    private void OnEnable()
    {
        shown = toggle.isOn;
        progress = shown ? 1f : 0f;
        Apply();
    }

    // Also catches SetIsOnWithoutNotify, which fires no event; unscaled since the pause freezes time.
    private void LateUpdate()
    {
        shown = toggle.isOn;
        float target = shown ? 1f : 0f;
        if (progress == target)
            return;
        progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / duration);
        Apply();
    }

    // Knob eases across, swelling mid-travel, while the track blends between both colors.
    private void Apply()
    {
        float eased = progress * progress * (3f - 2f * progress);
        track.color = Color.Lerp(offColor, onColor, eased);
        knob.anchoredPosition = new Vector2(Mathf.Lerp(-knobTravel, knobTravel, eased), knob.anchoredPosition.y);
        knob.localScale = Vector3.one * Mathf.Lerp(1f, knobPop, Mathf.Sin(eased * Mathf.PI));
    }
}
