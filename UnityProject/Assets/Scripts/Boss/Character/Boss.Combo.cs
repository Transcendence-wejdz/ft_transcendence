using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Combo sequencing: picks a combo, steps each pattern through Windup/Active/Recovery and replays phases on clients.
public partial class Boss
{
    private const float ChainWindupFraction = 0.6f;
    private const float ChargeOvershoot = 3f;
    private const float MinChargeLength = 4f;

    private readonly Queue<BossPattern> combo = new Queue<BossPattern>();
    private BossPattern pattern;
    private BossPhase phase;
    private float phaseEndTime;
    private bool chained;
    private float chargeLength;
    private Vector2 patternDirection;
    private bool comboUsedCharge;
    private bool comboUsedSlam;
    private bool comboUsedSummon;
    private bool openingDone;
    private float nextPatternTime;
    private float nextSlamTime;
    private float nextChargeTime;
    private float nextSummonTime;

    private bool CanSummon => kit.SummonKits != null && kit.SummonKits.Length > 0 && kit.SummonCount > 0;

    // Opens with a summon; then melee: charge through the target then slam (or slam alone); far: chained charges finished by a summon, or a single charge.
    private bool TryStartCombo(float distance)
    {
        bool chargeReady = Time.time >= nextChargeTime;
        if (!openingDone && CanSummon)
        {
            combo.Enqueue(BossPattern.Summon);
        }
        else if (distance <= kit.SlamMaxRange)
        {
            if (chargeReady)
                combo.Enqueue(BossPattern.Charge);
            if (chargeReady || Time.time >= nextSlamTime)
                combo.Enqueue(BossPattern.Slam);
        }
        else if (distance >= kit.ChargeMinRange && chargeReady)
        {
            bool finishWithSummon = CanSummon && Time.time >= nextSummonTime;
            int charges = finishWithSummon ? Mathf.Max(1, kit.ChargeChainCount) : 1;
            for (int i = 0; i < charges; i++)
                combo.Enqueue(BossPattern.Charge);
            if (finishWithSummon)
                combo.Enqueue(BossPattern.Summon);
        }

        openingDone = true;
        if (combo.Count == 0)
            return false;

        comboUsedCharge = comboUsedSlam = comboUsedSummon = false;
        StartNextPattern(false);
        return true;
    }

    // Re-aims every step at the current target, so each chained charge tracks where the player went.
    private void StartNextPattern(bool isChained)
    {
        pattern = combo.Dequeue();
        chained = isChained;
        if (target == null || target.IsDead)
            ChooseTarget(EnemyFlowFieldManager.Instance);

        Vector2 toTarget = target != null ? target.Center - BodyCenter : patternDirection;
        if (pattern == BossPattern.Charge)
        {
            comboUsedCharge = true;
            Vector2 aim = toTarget + targetVelocity * kit.ChargeLeadSeconds;
            patternDirection = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
            chargeLength = Mathf.Clamp(aim.magnitude + ChargeOvershoot, MinChargeLength, Mathf.Max(MinChargeLength, kit.ChargeDistance));
        }
        else
        {
            comboUsedSlam |= pattern == BossPattern.Slam;
            comboUsedSummon |= pattern == BossPattern.Summon;
            if (toTarget.sqrMagnitude > 0.0001f)
                patternDirection = toTarget.normalized;
        }

        SetFacing(patternDirection.x);
        EnterPhase(BossPhase.Windup);
    }

    // Steps inside a combo skip their recovery; only the last one recovers before the boss moves again.
    private void TickPattern()
    {
        bool charging = pattern == BossPattern.Charge && phase == BossPhase.Active;
        rb.linearVelocity = charging ? patternDirection * kit.ChargeSpeed : Vector2.zero;
        if (charging)
            HitChargeVictims();

        if (Time.time < phaseEndTime)
            return;

        if (phase == BossPhase.Windup)
            EnterPhase(BossPhase.Active);
        else if (phase == BossPhase.Active && combo.Count > 0)
            StartNextPattern(true);
        else if (phase == BossPhase.Active)
            EnterPhase(BossPhase.Recovery);
        else
            FinishCombo();
    }

    // Applies the phase's gameplay effect here (server), then replays its visuals on every peer with the same duration and lane length.
    private void EnterPhase(BossPhase next)
    {
        phase = next;
        float duration = PhaseDuration(pattern, next);
        phaseEndTime = Time.time + duration;

        if (next == BossPhase.Active && pattern == BossPattern.Charge)
            chargeHits.Clear();
        else if (next == BossPhase.Active && pattern == BossPattern.Slam)
            ResolveSlam();
        else if (next == BossPhase.Active && pattern == BossPattern.Summon)
            Summon();

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

    // Charge travel time follows its length and speed; everything else speeds up with the enrage tempo, chained windups are shorter.
    private float PhaseDuration(BossPattern p, BossPhase ph)
    {
        if (p == BossPattern.Charge && ph == BossPhase.Active)
            return chargeLength / Mathf.Max(0.01f, kit.ChargeSpeed);

        float baseDuration;
        if (ph == BossPhase.Recovery)
            baseDuration = kit.RecoveryDuration;
        else if (p == BossPattern.Charge)
            baseDuration = kit.ChargeWindup;
        else if (p == BossPattern.Slam)
            baseDuration = ph == BossPhase.Windup ? kit.SlamWindup : kit.SlamActiveDuration;
        else
            baseDuration = ph == BossPhase.Windup ? kit.SummonWindup : kit.SummonActiveDuration;

        if (ph == BossPhase.Windup && chained)
            baseDuration *= ChainWindupFraction;
        return baseDuration / Tempo;
    }

    // Clients replay the phase and judge Active hits against their own player.
    [Rpc(SendTo.NotServer)]
    private void PhaseRpc(BossPattern p, BossPhase ph, Vector2 direction, float duration, float laneLength)
    {
        PlayPhaseVisual(p, ph, direction, duration, laneLength);
        if (ph != BossPhase.Active)
            return;

        if (p == BossPattern.Slam)
        {
            JudgeLocalHit(kit.SlamRadius, kit.SlamDamage);
        }
        else if (p == BossPattern.Charge)
        {
            localChargeEndTime = Time.time + duration;
            localChargeHit = false;
        }
    }
}
