using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Sweeps a text's hover color in from the left with a small scale-up on pointer enter or selection, and back out on exit.
public partial class HoverColorWipe : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private const float MinWidth = 0.01f;

    [SerializeField] private TMP_Text label;
    [SerializeField] private Color hoverColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float edgeSoftness = 0.25f;
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private Image hoverFill;
    [SerializeField] private Image pinnedFill;
    [SerializeField] private Color pinnedColor = new Color(0.08f, 0.07f, 0.06f, 1f);

    private Selectable selectable;
    private bool pointerInside;
    private bool selected;
    private bool pinned;
    private float wipe;
    private float pin;
    private float scale;
    private bool painting;

    // Mouse clicks also select, so only gamepad focus counts as hover; otherwise a press dragged off the button stays lit.
    private bool Hovered
    {
        get
        {
            if (pinned || !(pointerInside || (selected && InputModeTracker.UsingGamepad)))
                return false;
            return selectable == null || selectable.IsInteractable();
        }
    }

    private static float Target(bool on)
    {
        if (on)
            return 1f;
        return 0f;
    }

    // Filled images use fillAmount; others (Custom/UIBrushReveal) read the vertex alpha as reveal progress.
    private static void SetReveal(Image fill, float amount)
    {
        Color c;

        if (fill == null)
            return;
        if (fill.type == Image.Type.Filled)
        {
            fill.fillAmount = amount;
            return;
        }
        c = fill.color;
        c.a = amount;
        fill.color = c;
    }

    private void PaintScale()
    {
        label.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, hoverScale, Mathf.SmoothStep(0f, 1f, scale));
    }

    // TMP rebuilds (re-enable, layout, text change) reset vertex colors after LateUpdate, so repaint right after each one.
    private void OnTextChanged(Object changed)
    {
        if (changed == label && (wipe > 0f || pin > 0f))
            PaintColors();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
    }

    // Pinned (e.g. the active tab) keeps the scale-up and sweeps pinnedFill plus pinnedColor in from the left instead of the hover color.
    public void SetPinned(bool value)
    {
        pinned = value;
    }

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    // Hidden menus keep no hover or pin progress, so reopening replays the pinned sweep from the start.
    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        pointerInside = false;
        selected = false;
        wipe = 0f;
        pin = 0f;
        scale = 0f;
        PaintScale();
        PaintColors();
    }

    // Unscaled time since the pause menu freezes timeScale.
    private void LateUpdate()
    {
        float step = Time.unscaledDeltaTime / duration;
        float previousWipe = wipe;
        float previousPin = pin;
        float previousScale = scale;
        bool hovered = Hovered;

        wipe = Mathf.MoveTowards(wipe, Target(hovered), step);
        pin = Mathf.MoveTowards(pin, Target(pinned), step);
        scale = Mathf.MoveTowards(scale, Target(hovered || pinned), step);
        if (scale != previousScale)
            PaintScale();
        if (wipe != previousWipe || pin != previousPin)
            PaintColors();
    }
}
