using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Small key cap showing what triggers an action: the pad button icon while a gamepad is in use, else the current key (rebinds included).
public class KeyCapUI : MonoBehaviour
{
    [SerializeField] private string actionName;
    [SerializeField] private Image background;
    [SerializeField] private Image padIcon;
    [SerializeField] private TMP_Text label;

    private bool resolved;

    private Sprite CurrentPadIcon()
    {
        if (!InputModeTracker.UsingGamepad)
            return null;
        return GamepadIcons.ForPath(KeybindOverrides.GetGamepadPath(actionName));
    }

    private void Refresh()
    {
        Sprite sprite = CurrentPadIcon();
        string key = KeybindOverrides.GetPrimaryDisplay(actionName);

        resolved = key != null;
        padIcon.sprite = sprite;
        padIcon.enabled = sprite != null;
        background.enabled = sprite == null;
        label.enabled = sprite == null;
        label.text = key ?? string.Empty;
    }

    private void HandleInputModeChanged(bool usingGamepad)
    {
        Refresh();
    }

    void OnEnable()
    {
        KeybindOverrides.OnSaved += Refresh;
        InputModeTracker.OnModeChanged += HandleInputModeChanged;
        Refresh();
    }

    void OnDisable()
    {
        KeybindOverrides.OnSaved -= Refresh;
        InputModeTracker.OnModeChanged -= HandleInputModeChanged;
    }

    // The bindings live on the local player, who can spawn after the HUD.
    void Update()
    {
        if (!resolved)
            Refresh();
    }
}
