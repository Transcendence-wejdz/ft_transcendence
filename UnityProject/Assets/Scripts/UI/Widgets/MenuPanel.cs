using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MenuPanel : MonoBehaviour
{
    public static MenuPanel CurrentOpen { get; private set; }

    // Fires once when this panel actually stops being the open one, however that happened (animated Close, or a forced external deactivation).
    public event System.Action OnClosed;

    [SerializeField] private float animationDuration = 0.15f;
    [SerializeField] private float closedScale = 0.85f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private PanelSlideIn slideIn;
    private Coroutine activeAnimation;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        slideIn = GetComponent<PanelSlideIn>();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        PauseManager.SetExternalPause(true);
        CurrentOpen = this;

        if (slideIn != null)
        {
            slideIn.Show();
            return;
        }

        if (activeAnimation != null)
            StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(Animate(opening: true));
    }

    // Full-screen menus with a PanelSlideIn play its exit instead of the scale pop.
    public void Close()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }

        if (slideIn != null)
        {
            slideIn.Hide();
            return;
        }

        if (activeAnimation != null)
            StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(Animate(opening: false));
    }

    // Only unpauses if still the open panel, so closing one panel to open another keeps the pause; also covers external deactivation.
    private void OnDisable()
    {
        activeAnimation = null;

        if (CurrentOpen == this)
        {
            CurrentOpen = null;
            PauseManager.SetExternalPause(false);
            OnClosed?.Invoke();
        }
    }

    // Unscaled time, so speed is independent of Time.timeScale.
    private IEnumerator Animate(bool opening)
    {
        float fromScale = opening ? closedScale : 1f;
        float toScale = opening ? 1f : closedScale;
        float fromAlpha = opening ? 0f : 1f;
        float toAlpha = opening ? 1f : 0f;

        canvasGroup.blocksRaycasts = opening;
        canvasGroup.interactable = opening;

        float t = 0f;
        while (t < animationDuration)
        {
            t += Time.unscaledDeltaTime;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / animationDuration), 3f);
            rectTransform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, eased);
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            yield return null;
        }

        rectTransform.localScale = Vector3.one * toScale;
        canvasGroup.alpha = toAlpha;

        if (!opening)
            gameObject.SetActive(false);

        activeAnimation = null;
    }
}
