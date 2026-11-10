using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Vertex color painting of the hover and pin sweeps across the label.
public partial class HoverColorWipe
{
    // Edge width of the pin sweep in the fill's own 0-1 space, matching the hover edge on the text.
    private float PinSoftness(Rect areaRect, float textWidth)
    {
        float areaWidth = Mathf.Max(areaRect.width, MinWidth);

        return Mathf.Max(edgeSoftness * textWidth * label.rectTransform.localScale.x / areaWidth, MinWidth);
    }

    // Pin tint of one vertex, from where it sits across the pinned fill.
    private Color ApplyPin(Color c, Vector3 vertex, RectTransform area, Rect areaRect, float pinFront, float pinSoftness)
    {
        Vector3 local = area.InverseTransformPoint(label.rectTransform.TransformPoint(vertex));
        float u = (local.x - areaRect.xMin) / Mathf.Max(areaRect.width, MinWidth);

        return Color.Lerp(c, pinnedColor, Mathf.Clamp01((pinFront - u) / pinSoftness));
    }

    // Hover front runs across the text; the pin front trails the fill across the button with the same soft edge, so dark text only sits on the fill.
    private void PaintMesh(TMP_MeshInfo mesh, Bounds bounds, float width, float hoverFront, float pinFront)
    {
        RectTransform area = null;
        Rect areaRect = default;
        float pinSoftness = 1f;
        float t;
        Color c;
        Color32 c32;

        if (pinnedFill != null)
        {
            area = pinnedFill.rectTransform;
            areaRect = area.rect;
            pinSoftness = PinSoftness(areaRect, width);
        }
        for (int v = 0; v < mesh.vertexCount; v++)
        {
            t = (mesh.vertices[v].x - bounds.min.x) / width;
            c = Color.Lerp(label.color, hoverColor, Mathf.Clamp01((hoverFront - t) / edgeSoftness));
            if (area != null && pin > 0f)
                c = ApplyPin(c, mesh.vertices[v], area, areaRect, pinFront, pinSoftness);
            c32 = c;
            c32.a = mesh.colors32[v].a;
            mesh.colors32[v] = c32;
        }
    }

    private void PaintVertexColors()
    {
        float pinFront = Mathf.SmoothStep(0f, 1f, pin);
        TMP_TextInfo info;
        Bounds bounds;
        float width;
        float hoverFront;

        SetReveal(pinnedFill, pinFront);
        SetReveal(hoverFill, Mathf.SmoothStep(0f, 1f, wipe));
        if (label.havePropertiesChanged)
            label.ForceMeshUpdate();
        info = label.textInfo;
        if (info == null || info.characterCount == 0)
            return;
        bounds = label.textBounds;
        width = Mathf.Max(bounds.size.x, MinWidth);
        hoverFront = Mathf.SmoothStep(0f, 1f, wipe) * (1f + edgeSoftness);
        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            if (info.meshInfo[m].vertices != null)
                PaintMesh(info.meshInfo[m], bounds, width, hoverFront, pinFront);
        }
        label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
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
}
