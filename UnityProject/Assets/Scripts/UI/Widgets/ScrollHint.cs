using UnityEngine;
using UnityEngine.UI;

// Arrows on the edges of a scroll view, each shown while there is more content to scroll towards it.
public class ScrollHint : MonoBehaviour
{
    private const float EdgeTolerance = 0.01f;
    private const float MinOverflow = 1f;

    [SerializeField] private CanvasGroup upArrow;
    [SerializeField] private CanvasGroup downArrow;
    [SerializeField] private float fadeSpeed = 6f;
    [Tooltip("Pixels the arrows bob towards the hidden content.")]
    [SerializeField] private float bobDistance = 3f;
    [SerializeField] private float bobSpeed = 4f;

    private ScrollRect scroll;
    private Vector2 upRest;
    private Vector2 downRest;

    private void Awake()
    {
        scroll = GetComponentInParent<ScrollRect>(true);
        upRest = ((RectTransform)upArrow.transform).anchoredPosition;
        downRest = ((RectTransform)downArrow.transform).anchoredPosition;
    }

    private void OnEnable()
    {
        upArrow.alpha = 0f;
        downArrow.alpha = 0f;
    }

    private bool Overflows()
    {
        if (scroll == null || scroll.content == null || scroll.viewport == null)
            return false;
        return scroll.content.rect.height - scroll.viewport.rect.height > MinOverflow;
    }

    private static void Fade(CanvasGroup arrow, bool visible, float step)
    {
        float target = 0f;

        if (visible)
            target = 1f;
        arrow.alpha = Mathf.MoveTowards(arrow.alpha, target, step);
    }

    // Unscaled since the pause menu may freeze time.
    private void LateUpdate()
    {
        bool overflows = Overflows();
        float position = 1f;
        float bob = (Mathf.Sin(Time.unscaledTime * bobSpeed) * 0.5f + 0.5f) * bobDistance;
        float step = Time.unscaledDeltaTime * fadeSpeed;

        if (overflows)
            position = scroll.verticalNormalizedPosition;
        Fade(upArrow, overflows && position < 1f - EdgeTolerance, step);
        Fade(downArrow, overflows && position > EdgeTolerance, step);
        ((RectTransform)upArrow.transform).anchoredPosition = upRest + Vector2.up * bob;
        ((RectTransform)downArrow.transform).anchoredPosition = downRest + Vector2.down * bob;
    }
}
