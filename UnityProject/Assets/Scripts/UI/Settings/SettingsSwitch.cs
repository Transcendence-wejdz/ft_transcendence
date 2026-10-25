using UnityEngine;
using UnityEngine.UI;

// Pill switch look for a Toggle: slides the knob and recolors the track on value change.
[RequireComponent(typeof(Toggle))]
public class SettingsSwitch : MonoBehaviour
{
    [SerializeField] private Image track;
    [SerializeField] private RectTransform knob;
    [SerializeField] private Color onColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color offColor = new Color(0.22f, 0.22f, 0.24f, 1f);
    [SerializeField] private float knobTravel = 8f;

    private Toggle toggle;
    private bool shown;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(Apply);
    }

    private void OnEnable()
    {
        Apply(toggle.isOn);
    }

    // Catches SetIsOnWithoutNotify, which fires no event.
    private void LateUpdate()
    {
        if (shown != toggle.isOn)
            Apply(toggle.isOn);
    }

    private void Apply(bool isOn)
    {
        shown = isOn;
        track.color = isOn ? onColor : offColor;
        knob.anchoredPosition = new Vector2(isOn ? knobTravel : -knobTravel, knob.anchoredPosition.y);
    }
}
