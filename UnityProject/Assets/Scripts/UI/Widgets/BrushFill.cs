using UnityEngine;
using UnityEngine.UI;

// Bar fill helper: brush-reveal images take the fraction as their reveal (painted edge), plain filled images as fillAmount.
public static class BrushFill
{
    private const string BrushShader = "Custom/UIBrushReveal";
    private const float RevealStart = 0.05f;
    private const float RevealSpan = 0.85f;

    // The stroke's reveal threshold runs from about 0.04 (left) to 0.9 (right), so the fraction is remapped onto that span.
    public static void Set(Image image, float fraction)
    {
        if (image == null)
            return;

        fraction = Mathf.Clamp01(fraction);
        if (!IsBrush(image))
        {
            image.fillAmount = fraction;
            return;
        }

        Color color = image.color;
        color.a = fraction <= 0f ? 0f : fraction >= 1f ? 1f : RevealStart + fraction * RevealSpan;
        image.color = color;
    }

    private static bool IsBrush(Image image)
    {
        Material material = image.material;
        return material != null && material.shader != null && material.shader.name == BrushShader;
    }
}
