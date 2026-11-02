using System.Collections;
using UnityEngine;

// NPC menu panel: one open at a time, blocks gameplay input while open, opens and closes with a scale pop or its PanelSlideIn.
[RequireComponent(typeof(CanvasGroup))]
public class MenuPanel : MonoBehaviour
{
    [SerializeField] private float animationDuration = 0.15f;
    [SerializeField] private float closedScale = 0.85f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private PanelSlideIn slideIn;
    private Coroutine activeAnimation;
    private MenuPanel underlying;
    private bool isClosing;

    public static MenuPanel CurrentOpen { get; private set; }

    // Fires once when this panel actually stops being the open one, however that happened (animated Close, or a forced external deactivation).
    public event System.Action OnClosed;

    private void StartAnimation(bool opening)
    {
        if (activeAnimation != null)
            StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(Animate(opening));
    }

    private void ApplyPop(float scale, float alpha)
    {
        rectTransform.localScale = Vector3.one * scale;
        canvasGroup.alpha = alpha;
    }

    // Unscaled time, so speed is independent of Time.timeScale.
    private IEnumerator Animate(bool opening)
    {
        float fromScale = 1f;
        float toScale = closedScale;
        float fromAlpha = 1f;
        float toAlpha = 0f;
        float t = 0f;
        float eased;

        if (opening)
        {
            fromScale = closedScale;
            toScale = 1f;
            fromAlpha = 0f;
            toAlpha = 1f;
        }
        canvasGroup.blocksRaycasts = opening;
        canvasGroup.interactable = opening;
        while (t < animationDuration)
        {
            t += Time.unscaledDeltaTime;
            eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / animationDuration), 3f);
            ApplyPop(Mathf.Lerp(fromScale, toScale, eased), Mathf.Lerp(fromAlpha, toAlpha, eased));
            yield return null;
        }
        ApplyPop(toScale, toAlpha);
        if (!opening)
            gameObject.SetActive(false);
        activeAnimation = null;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        PauseManager.SetExternalPause(true);
        if (CurrentOpen != null && CurrentOpen != this)
            underlying = CurrentOpen;
        isClosing = false;
        CurrentOpen = this;
        if (slideIn != null)
            slideIn.Show();
        else
            StartAnimation(true);
    }

    // Full-screen menus with a PanelSlideIn play its exit instead of the scale pop.
    public void Close()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }
        isClosing = true;
        if (slideIn != null)
            slideIn.Hide();
        else
            StartAnimation(false);
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        slideIn = GetComponent<PanelSlideIn>();
    }

    // A panel opened over another (a ready check over an NPC menu) hands control back to it when it closes.
    private bool RestoreUnderlying()
    {
        bool restored = underlying != null && underlying.gameObject.activeInHierarchy && !underlying.isClosing;

        if (restored)
            CurrentOpen = underlying;
        underlying = null;
        return restored;
    }

    // Only unpauses if still the open panel, so closing one panel to open another keeps the pause; also covers external deactivation.
    private void OnDisable()
    {
        activeAnimation = null;
        if (CurrentOpen != this)
            return;
        CurrentOpen = null;
        if (!RestoreUnderlying())
            PauseManager.SetExternalPause(false);
        OnClosed?.Invoke();
    }
}
