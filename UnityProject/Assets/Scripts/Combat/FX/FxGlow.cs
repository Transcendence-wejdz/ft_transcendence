using UnityEngine;
using UnityEngine.Rendering.Universal;

// Sprite Light2D shaped like an ability FX's current frame; color, intensity, size and duration come from the caster's kit.
[RequireComponent(typeof(Light2D))]
public class FxGlow : MonoBehaviour
{
    [SerializeField] private SpriteRenderer source;
    [SerializeField] private AnimationCurve intensityOverTime = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f));

    private Light2D glow;
    private float peakIntensity;
    private float duration;
    private float startTime;

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
        if (source != null)
        {
            if (glow.lightCookieSprite != source.sprite)
                glow.lightCookieSprite = source.sprite;
            lit &= source.enabled && source.sprite != null;
        }

        float t = duration > 0f ? (Time.time - startTime) / duration : 0f;
        lit &= t < 1f;
        glow.enabled = lit;
        if (lit)
            glow.intensity = duration > 0f ? peakIntensity * intensityOverTime.Evaluate(t) : peakIntensity;
    }

    // Applies the caster's light color and the kit's glow values for this ability to every glow under an FX.
    public static void Setup(GameObject fx, GameObject attacker, ClassKit kit, AbilityType ability)
    {
        Color color = CharacterCatalog.FxLightColorOf(attacker);
        (float intensity, float size, float seconds) = kit == null ? (1.2f, 1.1f, 0f) : ability switch
        {
            AbilityType.Special => (kit.SpecialGlowIntensity, kit.SpecialGlowSize, kit.SpecialGlowDuration),
            AbilityType.Ultimate => (kit.UltimateGlowIntensity, kit.UltimateGlowSize, kit.UltimateGlowDuration),
            _ => (kit.AttackGlowIntensity, kit.AttackGlowSize, kit.AttackGlowDuration),
        };

        foreach (FxGlow fxGlow in fx.GetComponentsInChildren<FxGlow>(true))
        {
            fxGlow.glow = fxGlow.GetComponent<Light2D>();
            fxGlow.glow.color = color;
            fxGlow.peakIntensity = intensity;
            fxGlow.duration = seconds;
            fxGlow.startTime = Time.time;
            fxGlow.transform.localScale = Vector3.one * size;
        }
    }
}
