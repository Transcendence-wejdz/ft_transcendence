using UnityEngine;
using UnityEngine.Rendering.Universal;

// Sprite Light2D shaped like an ability FX's current frame; color, intensity, size and duration come from the caster's kit.
[RequireComponent(typeof(Light2D))]
public class FxGlow : MonoBehaviour
{
    private const float DefaultIntensity = 1.2f;
    private const float DefaultSize = 1.1f;

    [SerializeField] private SpriteRenderer source;
    [SerializeField] private AnimationCurve intensityOverTime = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f));

    private Light2D glow;
    private float peakIntensity;
    private float duration;
    private float startTime;

    private bool IsTimed => duration > 0f;

    // Normalized age over the kit's duration; an untimed glow stays at 0.
    private float Progress()
    {
        if (!IsTimed)
            return 0f;
        return (Time.time - startTime) / duration;
    }

    // Copies the FX's current frame into the light cookie; unlit while the source sprite is hidden.
    private bool SyncWithSource()
    {
        if (source == null)
            return true;
        if (glow.lightCookieSprite != source.sprite)
            glow.lightCookieSprite = source.sprite;
        return source.enabled && source.sprite != null;
    }

    private float IntensityAt(float t)
    {
        if (!IsTimed)
            return peakIntensity;
        return peakIntensity * intensityOverTime.Evaluate(t);
    }

    private void Configure(Color color, float intensity, float seconds, float size)
    {
        glow = GetComponent<Light2D>();
        glow.color = color;
        peakIntensity = intensity;
        duration = seconds;
        startTime = Time.time;
        transform.localScale = Vector3.one * size;
    }

    // Applies the caster's light color and the kit's glow values for this ability to every glow under an FX.
    public static void Setup(GameObject fx, GameObject attacker, AbilityKit ability)
    {
        Color color = CharacterCatalog.FxLightColorOf(attacker);
        float intensity = DefaultIntensity;
        float size = DefaultSize;
        float seconds = 0f;

        if (ability != null)
        {
            intensity = ability.GlowIntensity;
            size = ability.GlowSize;
            seconds = ability.GlowDuration;
        }
        foreach (FxGlow fxGlow in fx.GetComponentsInChildren<FxGlow>(true))
            fxGlow.Configure(color, intensity, seconds, size);
    }

    void Awake()
    {
        glow = GetComponent<Light2D>();
        peakIntensity = glow.intensity;
        startTime = Time.time;
    }

    // Late so the light picks up the frame the FX flipbook just set this frame.
    void LateUpdate()
    {
        bool lit = peakIntensity > 0f;
        float t;

        lit &= SyncWithSource();
        t = Progress();
        lit &= t < 1f;
        glow.enabled = lit;
        if (lit)
            glow.intensity = IntensityAt(t);
    }
}
