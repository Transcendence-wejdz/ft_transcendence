using UnityEngine;
using UnityEngine.Rendering.Universal;

// Fades a one-shot FX's Light2D from its authored intensity to nothing.
[RequireComponent(typeof(Light2D))]
public class FxLightFade : MonoBehaviour
{
    [SerializeField] private float duration = 0.6f;

    private Light2D fxLight;
    private float startIntensity;
    private float elapsed;

    void Awake()
    {
        fxLight = GetComponent<Light2D>();
        startIntensity = fxLight.intensity;
    }

    // Off with the FX Glow setting.
    void Update()
    {
        fxLight.enabled = GameSettings.FxGlow;
        elapsed += Time.deltaTime;
        fxLight.intensity = startIntensity * (1f - Mathf.Clamp01(elapsed / duration));
    }
}
