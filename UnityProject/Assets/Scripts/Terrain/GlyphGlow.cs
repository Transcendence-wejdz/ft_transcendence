using UnityEngine;
using UnityEngine.Rendering.Universal;

// Pulses a glowing rune, its halo and light with a short bright peak; dull until a player holds it.
public class GlyphGlow : MonoBehaviour
{
    [SerializeField] private float minAlpha = 0.45f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float scalePulse = 0.08f;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float timeOffset = 0f;
    [SerializeField] private float minLightIntensity = 0.6f;
    [SerializeField] private float maxLightIntensity = 1.4f;
    [SerializeField] private SpriteRenderer halo;
    [SerializeField] private float minHaloAlpha = 0.1f;
    [SerializeField] private float maxHaloAlpha = 0.8f;
    [SerializeField] private float haloScalePulse = 0.2f;
    [SerializeField] private float peakSharpness = 2f;
    [SerializeField] private float peakWhiten = 0.5f;
    [SerializeField] private float idlePulse = 0.2f;
    [SerializeField] private float idleDim = 0.5f;
    [SerializeField] private float idleDesaturate = 0.6f;

    private SpriteRenderer spriteRenderer;
    private Light2D light2D;
    private Vector3 baseScale;
    private Vector3 haloBaseScale;
    private Color baseColor;
    private Color authoredColor;
    private Color authoredLightColor;
    private bool held;

    private Color DullColor(Color color)
    {
        Color dull = Color.Lerp(color, Color.gray, idleDesaturate) * idleDim;

        dull.a = color.a;
        return dull;
    }

    private void UpdateHalo(float t)
    {
        Color c = baseColor;

        if (halo == null)
            return;
        c.a = Mathf.Lerp(minHaloAlpha, maxHaloAlpha, t);
        halo.color = c;
        halo.transform.localScale = haloBaseScale * (1f + haloScalePulse * t);
    }

    // Shows who holds this glyph, its glow included.
    public void SetTint(Color color)
    {
        held = true;
        baseColor = color;
        if (light2D != null)
            light2D.color = color;
    }

    public void ClearTint()
    {
        held = false;
        baseColor = DullColor(authoredColor);
        if (light2D != null)
            light2D.color = authoredLightColor;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        light2D = GetComponentInChildren<Light2D>();
        baseScale = transform.localScale;
        if (halo != null)
            haloBaseScale = halo.transform.localScale;
        authoredColor = spriteRenderer.color;
        baseColor = DullColor(authoredColor);
        if (light2D != null)
            authoredLightColor = light2D.color;
    }

    void Update()
    {
        float t;
        Color c = baseColor;

        if (!spriteRenderer.isVisible)
            return;
        t = Mathf.Pow((Mathf.Sin((Time.time + timeOffset) * pulseSpeed) + 1f) * 0.5f, peakSharpness);
        if (!held)
            t *= idlePulse;
        c = Color.Lerp(c, Color.white, t * peakWhiten);
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        spriteRenderer.color = c;
        transform.localScale = baseScale * (1f + scalePulse * t);
        if (light2D != null)
            light2D.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, t);
        UpdateHalo(t);
    }
}
