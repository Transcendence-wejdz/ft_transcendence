using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Light2D on only at night (see NightLighting), dimmed when crowded; can flicker and follow a sprite's facing, e.g. a held torch.
[RequireComponent(typeof(Light2D))]
public class NightLight : MonoBehaviour
{
    private const float CrowdCheckInterval = 0.2f;
    private const float CrowdFadeSpeed = 2f;

    private static readonly List<NightLight> Shining = new List<NightLight>();

    [SerializeField] private float flicker;
    [SerializeField] private float flickerSpeed = 8f;
    [SerializeField] private SpriteRenderer mirrorWithSprite;

    private Light2D light2D;
    private float baseIntensity;
    private Vector3 baseLocalPosition;
    private float noiseSeed;
    private float crowdScale = 1f;
    private float targetCrowdScale = 1f;
    private float nextCrowdCheckTime;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        baseIntensity = light2D.intensity;
        baseLocalPosition = transform.localPosition;
        noiseSeed = Random.value * 100f;
    }

    void OnEnable()
    {
        NightLighting.OnNightChanged += Apply;
        Apply(NightLighting.IsNight);
    }

    void OnDisable()
    {
        NightLighting.OnNightChanged -= Apply;
        Shining.Remove(this);
    }

    private void Apply(bool night)
    {
        light2D.enabled = night;
        if (!night)
            Shining.Remove(this);
        else if (!Shining.Contains(this))
            Shining.Add(this);
    }

    void LateUpdate()
    {
        if (!light2D.enabled)
            return;

        if (Time.time >= nextCrowdCheckTime)
        {
            nextCrowdCheckTime = Time.time + CrowdCheckInterval;
            targetCrowdScale = CrowdScale();
        }
        crowdScale = Mathf.MoveTowards(crowdScale, targetCrowdScale, CrowdFadeSpeed * Time.deltaTime);

        float flame = flicker > 0f ? 1f - flicker * Mathf.PerlinNoise(noiseSeed, Time.time * flickerSpeed) : 1f;
        light2D.intensity = baseIntensity * flame * crowdScale;

        if (mirrorWithSprite != null)
        {
            Vector3 position = baseLocalPosition;
            if (mirrorWithSprite.flipX)
                position.x = -position.x;
            transform.localPosition = position;
        }
    }

    // 1 alone, 1/sqrt(1 + n) with n neighbours weighted by closeness: four stacked lights each shine at half strength.
    private float CrowdScale()
    {
        float crowd = 0f;
        Vector2 position = transform.position;
        foreach (NightLight other in Shining)
        {
            if (other == this)
                continue;

            float reach = Mathf.Max(light2D.pointLightOuterRadius, other.light2D.pointLightOuterRadius);
            if (reach <= 0f)
                continue;

            float distance = Vector2.Distance(position, other.transform.position);
            crowd += Mathf.Clamp01(1f - distance / reach);
        }
        return 1f / Mathf.Sqrt(1f + crowd);
    }
}
