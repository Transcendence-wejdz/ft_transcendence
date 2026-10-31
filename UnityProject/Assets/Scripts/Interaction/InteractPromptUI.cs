using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Per-NPC world-space "press E" prompt at a fixed local position.
public partial class InteractPromptUI : MonoBehaviour
{
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private TextMeshProUGUI keyLabel;
    [SerializeField] private Image keyIcon;
    [SerializeField] private Image keyBackground;
    [SerializeField] private TextMeshProUGUI actionLabel;
    [SerializeField] private string keyGlyph = "E";
    [SerializeField] private float animationDuration = 0.15f;
    [SerializeField] private float closedScale = 0.85f;
    [SerializeField] private float keyCapBlinkSpeed = 3f;
    [SerializeField] private float keyCapBlinkMinAlpha = 0.35f;
    [SerializeField] private float keyCapBlinkScaleAmplitude = 0.1f;
    [SerializeField] private bool labelAlwaysVisible;

    private CanvasGroup canvasGroup;
    private RectTransform keyCap;
    private CanvasGroup keyCapGroup;
    private Coroutine activeAnimation;
    private Coroutine blinkAnimation;

    private static CanvasGroup GetOrAddCanvasGroup(Component host)
    {
        CanvasGroup group = host.GetComponent<CanvasGroup>();

        if (group == null)
            group = host.gameObject.AddComponent<CanvasGroup>();
        return group;
    }

    // Cancels any mirroring inherited from the host (e.g. an NPC flipped with scale.x = -1) so the text never reads backwards.
    private void CancelMirroring()
    {
        Vector3 scale = transform.localScale;

        if (transform.lossyScale.x >= 0f)
            return;
        scale.x = -scale.x;
        transform.localScale = scale;
    }

    private void CacheKeyCap()
    {
        keyCap = promptRoot.Find("KeyCap") as RectTransform;
        if (keyCap != null)
            keyCapGroup = GetOrAddCanvasGroup(keyCap);
    }

    // Starts closed: only the keycap hides when the label always shows, else the whole prompt.
    private void InitClosedState()
    {
        if (labelAlwaysVisible && keyCap != null)
        {
            keyCap.localScale = Vector3.one * closedScale;
            keyCapGroup.alpha = 0f;
            return;
        }
        labelAlwaysVisible = false;
        promptRoot.localScale = Vector3.one * closedScale;
        canvasGroup.alpha = 0f;
        promptRoot.gameObject.SetActive(false);
    }

    private Sprite GetGamepadIcon()
    {
        if (!InputModeTracker.UsingGamepad)
            return null;
        return GamepadIcons.ForPath(KeybindOverrides.GetGamepadPath("Interact"));
    }

    // Shows the pad button icon while a gamepad is in use, else the current keyboard key (rebinds included, keyGlyph if unresolved).
    private void RefreshKeyLabel()
    {
        Sprite padIcon = GetGamepadIcon();

        if (keyIcon != null)
        {
            keyIcon.sprite = padIcon;
            keyIcon.enabled = padIcon != null;
        }
        if (keyBackground != null)
            keyBackground.enabled = padIcon == null;
        if (keyLabel != null)
        {
            keyLabel.enabled = padIcon == null;
            keyLabel.text = KeybindOverrides.GetKeyboardDisplay("Interact") ?? keyGlyph;
        }
    }

    private void HandleInputModeChanged(bool usingGamepad)
    {
        RefreshKeyLabel();
    }

    // Sets the action text without showing the prompt, for labels that stay visible out of range.
    public void SetLabel(string text)
    {
        if (actionLabel != null)
            actionLabel.text = text;
    }

    public void Show(string text)
    {
        RefreshKeyLabel();
        SetLabel(text);
        if (promptRoot == null)
            return;
        promptRoot.gameObject.SetActive(true);
        if (!promptRoot.gameObject.activeInHierarchy)
            return;
        StartAnimation(true);
        if (!labelAlwaysVisible && keyCap != null && blinkAnimation == null)
            blinkAnimation = StartCoroutine(BlinkKeyCap());
    }

    // activeInHierarchy, not activeSelf: a deactivated parent leaves this object's own flag true while StartCoroutine below would still fail.
    public void Hide()
    {
        if (promptRoot == null || !promptRoot.gameObject.activeInHierarchy)
            return;
        StopBlink();
        StartAnimation(false);
    }

    private void Awake()
    {
        CancelMirroring();
        RefreshKeyLabel();
        if (promptRoot == null)
            return;
        canvasGroup = GetOrAddCanvasGroup(promptRoot);
        CacheKeyCap();
        InitClosedState();
    }

    private void OnEnable()
    {
        InputModeTracker.OnModeChanged += HandleInputModeChanged;
        RefreshKeyLabel();
    }

    private void OnDisable()
    {
        InputModeTracker.OnModeChanged -= HandleInputModeChanged;
    }
}
