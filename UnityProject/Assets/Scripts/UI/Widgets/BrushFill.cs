using UnityEngine;
using UnityEngine.UI;

// Bar fill helper: brush-reveal images take the fraction as their reveal (painted edge), plain filled images as fillAmount.
public static class BrushFill
{
    private const string BrushShader = "Custom/UIBrushReveal";
    private const float RevealStart = 0.05f;
    private const float RevealSpan = 0.85f;

    // Empty and full stay exact so the stroke never shows a sliver at either end.
    private static float RevealFor(float fraction)
    {
        if (fraction <= 0f)
            return 0f;
        if (fraction >= 1f)
            return 1f;
        return RevealStart + fraction * RevealSpan;
    }

    private static bool IsBrush(Image image)
    {
        Material material = image.material;

        return material != null && material.shader != null && material.shader.name == BrushShader;
    }

    // The stroke's reveal threshold runs from about 0.04 (left) to 0.9 (right), so the fraction is remapped onto that span.
    public static void Set(Image image, float fraction)
    {
        Color color;

        if (image == null)
            return;
        fraction = Mathf.Clamp01(fraction);
        if (!IsBrush(image))
        {
            image.fillAmount = fraction;
            return;
        }
        color = image.color;
        color.a = RevealFor(fraction);
        image.color = color;
    }
}
