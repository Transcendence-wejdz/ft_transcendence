using System.Collections;
using UnityEngine;
using TMPro;

// Per-NPC world-space "press E" prompt at a fixed local position.
public class InteractPromptUI : MonoBehaviour
{
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private TextMeshProUGUI keyLabel;
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

    private void Awake()
    {
        RefreshKeyLabel();

        if (promptRoot != null)
        {
            canvasGroup = promptRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = promptRoot.gameObject.AddComponent<CanvasGroup>();

            keyCap = promptRoot.Find("KeyCap") as RectTransform;
            if (keyCap != null)
            {
                keyCapGroup = keyCap.GetComponent<CanvasGroup>();
                if (keyCapGroup == null)
                    keyCapGroup = keyCap.gameObject.AddComponent<CanvasGroup>();
            }

            if (labelAlwaysVisible && keyCap != null)
            {
                keyCap.localScale = Vector3.one * closedScale;
                keyCapGroup.alpha = 0f;
            }
            else
            {
                labelAlwaysVisible = false;
                promptRoot.localScale = Vector3.one * closedScale;
                canvasGroup.alpha = 0f;
                promptRoot.gameObject.SetActive(false);
            }
        }
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

        if (activeAnimation != null)
            StopCoroutine(activeAnimation);

        if (labelAlwaysVisible)
        {
            activeAnimation = StartCoroutine(Animate(keyCap, keyCapGroup, opening: true));
            return;
        }

        activeAnimation = StartCoroutine(Animate(promptRoot, canvasGroup, opening: true));
        if (keyCap != null && blinkAnimation == null)
            blinkAnimation = StartCoroutine(BlinkKeyCap());
    }

    // Reflects the player's current Interact keybind, including rebinds; falls back to keyGlyph if unresolved.
    private void RefreshKeyLabel()
    {
        if (keyLabel != null)
            keyLabel.text = KeybindOverrides.GetKeyboardDisplay("Interact") ?? keyGlyph;
    }

    // activeInHierarchy, not activeSelf: a deactivated parent leaves this object's own flag true while StartCoroutine below would still fail.
    public void Hide()
    {
        if (promptRoot == null || !promptRoot.gameObject.activeInHierarchy)
            return;

        if (blinkAnimation != null)
        {
            StopCoroutine(blinkAnimation);
            blinkAnimation = null;
            keyCap.localScale = Vector3.one;
            keyCapGroup.alpha = 1f;
        }

        if (activeAnimation != null)
            StopCoroutine(activeAnimation);
        activeAnimation = labelAlwaysVisible
            ? StartCoroutine(Animate(keyCap, keyCapGroup, opening: false))
            : StartCoroutine(Animate(promptRoot, canvasGroup, opening: false));
    }

    // Unscaled-time scale and alpha tween from the current state, so it works while paused and when interrupted.
    private IEnumerator Animate(RectTransform target, CanvasGroup group, bool opening)
    {
        float fromScale = target.localScale.x;
        float toScale = opening ? 1f : closedScale;
        float fromAlpha = group.alpha;
        float toAlpha = opening ? 1f : 0f;

        float t = 0f;
        while (t < animationDuration)
        {
            t += Time.unscaledDeltaTime;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / animationDuration), 3f);
            target.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, eased);
            group.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            yield return null;
        }

        target.localScale = Vector3.one * toScale;
        group.alpha = toAlpha;
        activeAnimation = null;

        if (opening && labelAlwaysVisible && blinkAnimation == null)
            blinkAnimation = StartCoroutine(BlinkKeyCap());

        if (!opening && target == promptRoot)
            promptRoot.gameObject.SetActive(false);
    }

    // Pulses the keycap's alpha and scale.
    private IEnumerator BlinkKeyCap()
    {
        while (true)
        {
            float wave = (Mathf.Sin(Time.unscaledTime * keyCapBlinkSpeed) + 1f) * 0.5f;
            keyCapGroup.alpha = Mathf.Lerp(keyCapBlinkMinAlpha, 1f, wave);
            keyCap.localScale = Vector3.one * Mathf.Lerp(1f - keyCapBlinkScaleAmplitude, 1f, wave);
            yield return null;
        }
    }
}
