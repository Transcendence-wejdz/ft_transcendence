using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Shared top-of-screen popup card: slides down while fading in, runs an auto-hide timer bar, and slides back up on hide.
public class ToastCard : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private CanvasGroup contentGroup;
    [SerializeField] private Image timerFill;
    [SerializeField] private float autoHideSeconds = 30f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private float slideDistance = 30f;

    private Coroutine routine;
    private Vector2 restPosition;

    public bool IsShown => content.activeSelf;

    private void Restart(IEnumerator next)
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(next);
    }

    // Unscaled time throughout, since the pause menu may have frozen timeScale.
    private IEnumerator ShowRoutine()
    {
        float shownAt;

        yield return Animate(contentGroup.alpha, 1f);
        shownAt = Time.unscaledTime;
        while (Time.unscaledTime - shownAt < autoHideSeconds)
        {
            timerFill.fillAmount = 1f - (Time.unscaledTime - shownAt) / autoHideSeconds;
            yield return null;
        }
        yield return HideRoutine();
    }

    private IEnumerator HideRoutine()
    {
        yield return Animate(contentGroup.alpha, 0f);
        content.SetActive(false);
        routine = null;
    }

    // Buttons only take clicks once fully shown.
    // Slides up as it fades out, down as it fades in.
    private void ApplyAlpha(RectTransform rect, float alpha)
    {
        contentGroup.alpha = alpha;
        rect.anchoredPosition = restPosition + Vector2.up * slideDistance * (1f - alpha);
    }

    private IEnumerator Animate(float from, float to)
    {
        RectTransform rect = (RectTransform)content.transform;
        float t = 0f;
        float eased;

        contentGroup.interactable = false;
        contentGroup.blocksRaycasts = to > 0f;
        while (t < animationDuration)
        {
            t += Time.unscaledDeltaTime;
            eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / animationDuration), 3f);
            ApplyAlpha(rect, Mathf.Lerp(from, to, eased));
            yield return null;
        }
        ApplyAlpha(rect, to);
        contentGroup.interactable = to > 0f;
        if (to > 0f)
            timerFill.fillAmount = 1f;
    }

    // Showing again while visible restarts the timer without replaying the slide-in from scratch.
    public void Show()
    {
        content.SetActive(true);
        Restart(ShowRoutine());
    }

    public void Hide()
    {
        if (content.activeSelf)
            Restart(HideRoutine());
    }

    private void Awake()
    {
        restPosition = ((RectTransform)content.transform).anchoredPosition;
        content.SetActive(false);
    }
}
