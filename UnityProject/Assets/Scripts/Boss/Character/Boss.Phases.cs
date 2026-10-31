using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Pattern phases: Windup, Active, Recovery, their durations, effects and the replay on clients.
public partial class Boss
{
    // Steps inside a combo skip their recovery; only the last one recovers before the boss moves again.
    private void AdvancePhase()
    {
        if (phase == BossPhase.Windup)
            EnterPhase(BossPhase.Active);
        else if (phase == BossPhase.Active && combo.Count > 0)
            StartNextPattern(true);
        else if (phase == BossPhase.Active)
            EnterPhase(BossPhase.Recovery);
        else
            FinishCombo();
    }

    // Server-side gameplay effect of entering a pattern's Active phase.
    private void ApplyActiveEffect()
    {
        if (pattern == BossPattern.Charge)
            chargeHits.Clear();
        else if (pattern == BossPattern.Slam)
            ResolveSlam();
        else if (pattern == BossPattern.Summon)
            Summon();
    }

    // Applies the phase's gameplay effect here (server), then replays its visuals on every peer with the same duration and lane length.
    private void EnterPhase(BossPhase next)
    {
        float duration;

        phase = next;
        duration = PhaseDuration(pattern, next);
        phaseEndTime = Time.time + duration;
        if (next == BossPhase.Active)
            ApplyActiveEffect();
        PlayPhaseVisual(pattern, next, patternDirection, duration, chargeLength);
        if (IsSpawned)
            PhaseRpc(pattern, next, patternDirection, duration, chargeLength);
    }

    // Cooldowns start once the whole combo is over; only a short pause separates two combos.
    private void FinishCombo()
    {
        float tempo = Tempo;

        if (comboUsedSlam)
            nextSlamTime = Time.time + kit.SlamCooldown / tempo;
        if (comboUsedCharge)
            nextChargeTime = Time.time + kit.ChargeCooldown / tempo;
        if (comboUsedSummon)
            nextSummonTime = Time.time + kit.SummonInterval / tempo;
        pattern = BossPattern.None;
        nextPatternTime = Time.time + kit.PatternCooldown / tempo;
        nextRetargetTime = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    // Kit duration of a phase before enrage and chaining (charge Active is handled apart).
    private float BaseDuration(BossPattern p, BossPhase ph)
    {
        if (ph == BossPhase.Recovery)
            return kit.RecoveryDuration;
        if (p == BossPattern.Charge)
            return kit.ChargeWindup;
        if (p == BossPattern.Slam)
        {
            if (ph == BossPhase.Windup)
                return kit.SlamWindup;
            return kit.SlamActiveDuration;
        }
        if (ph == BossPhase.Windup)
            return kit.SummonWindup;
        return kit.SummonActiveDuration;
    }

    // Charge travel time follows its length and speed; everything else speeds up with the enrage tempo, chained windups are shorter.
    private float PhaseDuration(BossPattern p, BossPhase ph)
    {
        float duration;

        if (p == BossPattern.Charge && ph == BossPhase.Active)
            return chargeLength / Mathf.Max(MinChargeSpeed, kit.ChargeSpeed);
        duration = BaseDuration(p, ph);
        if (ph == BossPhase.Windup && chained)
            duration *= ChainWindupFraction;
        return duration / Tempo;
    }

    // Clients replay the phase and judge Active hits against their own player.
    [Rpc(SendTo.NotServer)]
    private void PhaseRpc(BossPattern p, BossPhase ph, Vector2 direction, float duration, float laneLength)
    {
        PlayPhaseVisual(p, ph, direction, duration, laneLength);
        if (ph != BossPhase.Active)
            return;
        if (p == BossPattern.Slam)
            JudgeLocalHit(kit.SlamRadius, kit.SlamDamage);
        else if (p == BossPattern.Charge)
        {
            localChargeEndTime = Time.time + duration;
            localChargeHit = false;
        }
    }
}
