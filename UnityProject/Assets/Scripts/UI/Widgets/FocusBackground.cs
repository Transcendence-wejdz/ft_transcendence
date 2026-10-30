using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Gamepad focus drawn as a fade of this row's own background instead of the navigator's scale-up, for full-width rows in a masked list.
[RequireComponent(typeof(Image))]
public class FocusBackground : MonoBehaviour, IFocusVisual
{
    [SerializeField] private Color focusedColor = new Color(1f, 0.85f, 0.45f, 0.12f);
    [SerializeField] private float fadeSpeed = 8f;

    private Image background;
    private Color idleColor;
    private float highlight;

    private void Awake()
    {
        background = GetComponent<Image>();
        idleColor = background.color;
    }

    private void OnDisable()
    {
        highlight = 0f;
        background.color = idleColor;
    }

    // Unscaled since the pause menu may freeze time; mouse clicks also select, so only gamepad focus counts.
    private void Update()
    {
        GameObject selected = EventSystem.current != null && InputModeTracker.UsingGamepad ? EventSystem.current.currentSelectedGameObject : null;
        float target = selected == gameObject ? 1f : 0f;
        if (highlight == target)
            return;

        highlight = Mathf.MoveTowards(highlight, target, Time.unscaledDeltaTime * fadeSpeed);
        background.color = Color.Lerp(idleColor, focusedColor, highlight);
    }
}
