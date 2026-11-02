using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Combo sequencing: picks a combo, steps each pattern through Windup/Active/Recovery and replays phases on clients.
public partial class Boss
{
    private const float ChainWindupFraction = 0.6f;
    private const float ChargeOvershoot = 3f;
    private const float MinChargeLength = 4f;
    private const float MinChargeSpeed = 0.01f;
    private const float MinDirectionSqr = 0.0001f;

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
    private bool IsCharging => pattern == BossPattern.Charge && phase == BossPhase.Active;

    private void EnqueueMany(BossPattern step, int count)
    {
        for (int i = 0; i < count; i++)
            combo.Enqueue(step);
    }

    // Melee: charge through the target then slam, or slam alone.
    private void QueueMeleeCombo(bool chargeReady)
    {
        if (chargeReady)
            combo.Enqueue(BossPattern.Charge);
        if (chargeReady || Time.time >= nextSlamTime)
            combo.Enqueue(BossPattern.Slam);
    }

    // Far: chained charges finished by a summon, or a single charge.
    private void QueueRangedCombo()
    {
        if (!CanSummon || Time.time < nextSummonTime)
        {
            combo.Enqueue(BossPattern.Charge);
            return;
        }
        EnqueueMany(BossPattern.Charge, Mathf.Max(1, kit.ChargeChainCount));
        combo.Enqueue(BossPattern.Summon);
    }

    // Opens with a summon, then picks a melee or ranged combo from the distance.
    private void QueueCombo(float distance)
    {
        bool chargeReady = Time.time >= nextChargeTime;

        if (!openingDone && CanSummon)
            combo.Enqueue(BossPattern.Summon);
        else if (distance <= kit.SlamMaxRange)
            QueueMeleeCombo(chargeReady);
        else if (distance >= kit.ChargeMinRange && chargeReady)
            QueueRangedCombo();
    }

    private void ResetComboUsage()
    {
        comboUsedCharge = false;
        comboUsedSlam = false;
        comboUsedSummon = false;
    }

    private bool TryStartCombo(float distance)
    {
        QueueCombo(distance);
        openingDone = true;
        if (combo.Count == 0)
            return false;
        ResetComboUsage();
        StartNextPattern(false);
        return true;
    }

    // Leads the target by its estimated velocity and overshoots it, within the kit's charge length.
    private void AimCharge(Vector2 toTarget)
    {
        Vector2 aim = toTarget + targetVelocity * kit.ChargeLeadSeconds;
        float maxLength = Mathf.Max(MinChargeLength, kit.ChargeDistance);

        comboUsedCharge = true;
        patternDirection = Vector2.right;
        if (aim.sqrMagnitude > MinDirectionSqr)
            patternDirection = aim.normalized;
        chargeLength = Mathf.Clamp(aim.magnitude + ChargeOvershoot, MinChargeLength, maxLength);
    }

    private void AimStill(Vector2 toTarget)
    {
        if (pattern == BossPattern.Slam)
            comboUsedSlam = true;
        if (pattern == BossPattern.Summon)
            comboUsedSummon = true;
        if (toTarget.sqrMagnitude > MinDirectionSqr)
            patternDirection = toTarget.normalized;
    }

    // Re-aims every step at the current target, so each chained charge tracks where the player went.
    private void StartNextPattern(bool isChained)
    {
        Vector2 toTarget = patternDirection;

        pattern = combo.Dequeue();
        chained = isChained;
        if (target == null || target.IsDead)
            ChooseTarget(EnemyFlowFieldManager.Instance);
        if (target != null)
            toTarget = target.Center - BodyCenter;
        if (pattern == BossPattern.Charge)
            AimCharge(toTarget);
        else
            AimStill(toTarget);
        SetFacing(patternDirection.x);
        EnterPhase(BossPhase.Windup);
    }

    private void TickPattern()
    {
        if (IsCharging)
        {
            rb.linearVelocity = patternDirection * kit.ChargeSpeed;
            HitChargeVictims();
            PushMinionsAside();
        }
        else
            rb.linearVelocity = Vector2.zero;
        if (Time.time >= phaseEndTime)
            AdvancePhase();
    }

}
