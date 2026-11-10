using Unity.Cinemachine;
using UnityEngine;

// Hit flash for everyone, plus the owner's own shake, red screen edges and camera punch.
public partial class Player
{
    private const float BigHitWeight = 0.5f;
    private const float LightHitVignetteFraction = 0.6f;

    [Header("References")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private SpriteHitFlash hitFlash;

    [Header("Feedback")]
    [SerializeField] private float hurtShake = 0.35f;
    [SerializeField] private float hurtShakeMin = 0.08f;
    [SerializeField] private float hurtVignetteStrength = 0.6f;
    [SerializeField] private float hurtVignetteMin = 0.2f;
    [SerializeField] private float hurtVignetteDuration = 0.45f;
    [SerializeField] private float bigHitFraction = 0.25f;
    [SerializeField] private float hurtPunchZoom = 0.06f;
    [SerializeField] private float hurtPunchDuration = 0.2f;
    [SerializeField] private float attackShake = 0.06f;
    [SerializeField] private float specialShake = 0.15f;
    [SerializeField] private float hitLandedShake = 0.12f;

    private ScreenEdgeFlash edgeFlash;
    private float lastHealth = -1f;

    private void AwakeFeedback()
    {
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
        if (hitFlash == null)
            hitFlash = GetComponent<SpriteHitFlash>();
    }

    private void OnEnableFeedback()
    {
        OnHealthReplicated += HandleHealthReplicated;
        OnAbilityUsed += HandleAbilityUsed;
        SlashAttackFX.OnHitLanded += HandleHitLanded;
    }

    private void OnDisableFeedback()
    {
        OnHealthReplicated -= HandleHealthReplicated;
        OnAbilityUsed -= HandleAbilityUsed;
        SlashAttackFX.OnHitLanded -= HandleHitLanded;
    }

    private void OnDestroyFeedback()
    {
        if (edgeFlash != null)
            Destroy(edgeFlash.gameObject);
    }

    // Everyone sees the flash; the owner also gets shake, red edges and a punch on big hits; a drop to full health is a class switch, not a hit.
    private void Shake(float force)
    {
        if (impulseSource != null && GameSettings.ScreenShakeEnabled)
            impulseSource.GenerateImpulse(Random.insideUnitCircle.normalized * force);
    }

    // 0 for a scratch, 1 for a hit worth bigHitFraction of the max health or more.
    private float HitWeight(float damage, float max)
    {
        if (max <= 0f)
            return 1f;
        return Mathf.Clamp01(damage / max / bigHitFraction);
    }

    private void PulseEdges(float weight)
    {
        float strength = Mathf.Lerp(hurtVignetteMin, hurtVignetteStrength, weight);
        float duration = Mathf.Lerp(hurtVignetteDuration * LightHitVignetteFraction, hurtVignetteDuration, weight);

        if (edgeFlash == null)
            edgeFlash = ScreenEdgeFlash.Create();
        edgeFlash.Pulse(strength, duration);
    }

    private void PlayOwnHurtFeedback(float weight)
    {
        Shake(Mathf.Lerp(hurtShakeMin, hurtShake, weight));
        PulseEdges(weight);
        if (weight > BigHitWeight && GameSettings.ScreenShakeEnabled)
            GetComponentInChildren<CameraDistanceSetting>(true)?.Punch(hurtPunchZoom * weight, hurtPunchDuration);
    }

    private void HandleHealthReplicated(float current, float max)
    {
        bool tookDamage = lastHealth >= 0f && current < lastHealth && current < max;
        float damage = lastHealth - current;

        lastHealth = current;
        if (!tookDamage)
            return;
        hitFlash?.Flash();
        if (this.IsLocallyControlled())
            PlayOwnHurtFeedback(HitWeight(damage, max));
    }

    private void HandleAbilityUsed(AbilityType ability, float cooldown)
    {
        if (!this.IsLocallyControlled())
            return;
        if (ability == AbilityType.Attack)
            Shake(attackShake);
        else if (ability == AbilityType.Special)
            Shake(specialShake);
    }

    // Raised only by the attacker's own hitbox.
    private void HandleHitLanded(GameObject attacker, Vector3 position)
    {
        if (attacker == gameObject && this.IsLocallyControlled())
            Shake(hitLandedShake);
    }

}
