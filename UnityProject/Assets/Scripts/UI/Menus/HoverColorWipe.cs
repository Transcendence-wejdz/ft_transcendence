using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Sweeps a text's hover color in from the left with a small scale-up on pointer enter or selection, and back out on exit.
public class HoverColorWipe : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
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

    private bool Hovered => (pointerInside || selected) && !pinned && (selectable == null || selectable.IsInteractable());

    public void OnPointerEnter(PointerEventData eventData) => pointerInside = true;
    public void OnPointerExit(PointerEventData eventData) => pointerInside = false;
    public void OnSelect(BaseEventData eventData) => selected = true;
    public void OnDeselect(BaseEventData eventData) => selected = false;

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
        wipe = Mathf.MoveTowards(wipe, Hovered ? 1f : 0f, step);
        pin = Mathf.MoveTowards(pin, pinned ? 1f : 0f, step);
        scale = Mathf.MoveTowards(scale, Hovered || pinned ? 1f : 0f, step);

        if (scale != previousScale)
            PaintScale();
        if (wipe != previousWipe || pin != previousPin)
            PaintColors();
    }

    // TMP rebuilds (re-enable, layout, text change) reset vertex colors after LateUpdate, so repaint right after each one.
    private void OnTextChanged(Object changed)
    {
        if (changed == label && (wipe > 0f || pin > 0f))
            PaintColors();
    }

    private void PaintScale()
    {
        label.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, hoverScale, Mathf.SmoothStep(0f, 1f, scale));
    }

    // Filled images use fillAmount; others (Custom/UIBrushReveal) read the vertex alpha as reveal progress.
    private static void SetReveal(Image fill, float amount)
    {
        if (fill == null)
            return;
        if (fill.type == Image.Type.Filled)
        {
            fill.fillAmount = amount;
            return;
        }
        Color c = fill.color;
        c.a = amount;
        fill.color = c;
    }

    // Guarded because ForceMeshUpdate raises TEXT_CHANGED_EVENT, which would call back in here.
    private void PaintColors()
    {
        if (painting)
            return;
        painting = true;
        try
        {
            PaintVertexColors();
        }
        finally
        {
            painting = false;
        }
    }

    // Hover front runs across the text; the pin front trails the fill across the button with the same soft edge, so dark text only sits on the fill.
    private void PaintVertexColors()
    {
        float pinFront = Mathf.SmoothStep(0f, 1f, pin);
        SetReveal(pinnedFill, pinFront);
        SetReveal(hoverFill, Mathf.SmoothStep(0f, 1f, wipe));

        if (label.havePropertiesChanged)
            label.ForceMeshUpdate();
        TMP_TextInfo info = label.textInfo;
        if (info == null || info.characterCount == 0)
            return;

        Color baseColor = label.color;
        Bounds bounds = label.textBounds;
        float width = Mathf.Max(bounds.size.x, 0.01f);
        float hoverFront = Mathf.SmoothStep(0f, 1f, wipe) * (1f + edgeSoftness);
        RectTransform area = pinnedFill != null ? pinnedFill.rectTransform : null;
        Rect areaRect = area != null ? area.rect : default;
        float pinSoftness = area != null ? Mathf.Max(edgeSoftness * width * label.rectTransform.localScale.x / Mathf.Max(areaRect.width, 0.01f), 0.01f) : 1f;

        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            TMP_MeshInfo mesh = info.meshInfo[m];
            if (mesh.vertices == null)
                continue;

            for (int v = 0; v < mesh.vertexCount; v++)
            {
                float t = (mesh.vertices[v].x - bounds.min.x) / width;
                Color c = Color.Lerp(baseColor, hoverColor, Mathf.Clamp01((hoverFront - t) / edgeSoftness));

                if (area != null && pin > 0f)
                {
                    Vector3 local = area.InverseTransformPoint(label.rectTransform.TransformPoint(mesh.vertices[v]));
                    float u = (local.x - areaRect.xMin) / Mathf.Max(areaRect.width, 0.01f);
                    c = Color.Lerp(c, pinnedColor, Mathf.Clamp01((pinFront - u) / pinSoftness));
                }

                Color32 c32 = c;
                c32.a = mesh.colors32[v].a;
                mesh.colors32[v] = c32;
            }
        }
        label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
