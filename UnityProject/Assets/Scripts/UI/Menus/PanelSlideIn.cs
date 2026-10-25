using System.Collections;
using UnityEngine;

// Entrance played every time the panel is shown (backdrop fades in, content slides in from the left, items fade in one after another), reversed by Hide.
public class PanelSlideIn : MonoBehaviour
{
    [SerializeField] private CanvasGroup backdrop;
    [SerializeField] private RectTransform slideTarget;
    [SerializeField] private CanvasGroup[] items;
    [SerializeField] private float slideDistance = 60f;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float itemFade = 0.2f;
    [SerializeField] private float itemStagger = 0.05f;

    private Vector2 restPosition;
    private bool hasRestPosition;
    private float time;
    private bool closing;

    public bool IsOpen => gameObject.activeSelf && !closing;

    private float Total => Mathf.Max(duration, itemFade + itemStagger * Mathf.Max(items.Length - 1, 0));

    // Also cancels a running Hide, playing back in from wherever it was.
    public void Show()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }
        StopAllCoroutines();
        StartCoroutine(Play(1f));
    }

    // Plays the entrance backwards, then deactivates the panel.
    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }
        StopAllCoroutines();
        StartCoroutine(Play(-1f));
    }

    // Applies the hidden state immediately so the first rendered frame is not the finished menu.
    private void OnEnable()
    {
        if (!hasRestPosition)
        {
            restPosition = slideTarget.anchoredPosition;
            hasRestPosition = true;
        }
        time = 0f;
        Apply();
        StartCoroutine(Play(1f));
    }

    private void OnDisable()
    {
        closing = false;
        time = Total;
        Apply();
    }

    // Unscaled time, since the pause menu freezes timeScale; input is blocked while closing.
    private IEnumerator Play(float direction)
    {
        bool opening = direction > 0f;
        closing = !opening;
        if (backdrop != null)
        {
            backdrop.interactable = opening;
            backdrop.blocksRaycasts = opening;
        }

        float target = opening ? Total : 0f;
        while (time != target)
        {
            time = Mathf.MoveTowards(time, target, Time.unscaledDeltaTime);
            Apply();
            yield return null;
        }

        if (!opening)
            gameObject.SetActive(false);
    }

    private void Apply()
    {
        float eased = EaseOut(time / duration);
        if (backdrop != null)
            backdrop.alpha = eased;
        slideTarget.anchoredPosition = restPosition + Vector2.left * slideDistance * (1f - eased);

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                items[i].alpha = EaseOut((time - i * itemStagger) / itemFade);
        }
    }

    private static float EaseOut(float x)
    {
        return 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);
    }
}
