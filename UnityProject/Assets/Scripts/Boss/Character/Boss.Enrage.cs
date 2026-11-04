using UnityEngine;

// Phase 2 rules: the invulnerable roar that opens it, its push on the local player and the burning trail of charges.
public partial class Boss
{
    private bool roarDone;
    private float invulnerableEndTime;
    private Vector2 roarPushVelocity;
    private float roarPushEndTime;
    private bool droppingFire;
    private Vector2 lastFirePosition;
    private Vector2 chargeDirectionHere;

    private bool IsInvulnerable => Time.time < invulnerableEndTime;
    private bool IsRoarPending => !roarDone && IsEnraged;
    private bool IsChargingHere => IsCharging || Time.time < localChargeEndTime;

    // Called between combos: a running combo is cut short first, then the roar opens the phase.
    private bool TryStartRoar()
    {
        if (!IsRoarPending)
            return false;
        roarDone = true;
        ResetComboUsage();
        combo.Enqueue(BossPattern.Roar);
        StartNextPattern(false);
        return true;
    }

    private float RecoveryDuration()
    {
        if (IsEnraged)
            return kit.EnrageRecoveryDuration;
        return kit.RecoveryDuration;
    }

    // Every peer pushes its own player, against the boss as displayed on this machine.
    private void StartRoarPush()
    {
        Player local = LocalPlayer.GetPlayer();
        Vector2 away;

        if (local == null || local.IsDead || !IsWithin(local, kit.RoarRadius))
            return;
        away = local.Center - BodyCenter;
        if (away.sqrMagnitude < MinDirectionSqr)
            away = Vector2.right;
        roarPushVelocity = away.normalized * kit.RoarPushSpeed;
        roarPushEndTime = Time.time + kit.RoarPushDuration;
    }

    private void UpdateRoarPush()
    {
        Player local;

        if (Time.time >= roarPushEndTime)
            return;
        local = LocalPlayer.GetPlayer();
        if (local != null)
            local.AddPush(roarPushVelocity * Time.deltaTime);
    }

    private void DropFirePatch(Vector2 position)
    {
        GameObject patch = Instantiate(kit.FirePatchPrefab, position, Quaternion.identity);

        patch.GetComponent<BossFirePatch>().Init(kit.FireDamagePerSecond, kit.FireLifetime);
    }

    // One row of patches across the charge lane, so the fire covers the zone the telegraph showed.
    private void DropFireRow(Vector2 center)
    {
        Vector2 side = new Vector2(-chargeDirectionHere.y, chargeDirectionHere.x);
        float width = ChargeHitWidth;
        int count = Mathf.Max(1, Mathf.CeilToInt(width / kit.FireSpacing));
        float step = width / count;

        for (int i = 0; i < count; i++)
            DropFirePatch(center + side * ((i - (count - 1) * 0.5f) * step));
    }

    // Each peer drops its own patches along the charge it sees, and each patch only burns this machine's player.
    private void UpdateFireTrail()
    {
        Vector2 position = BodyCenter;

        if (!IsEnraged || !IsChargingHere || kit.FirePatchPrefab == null)
        {
            droppingFire = false;
            return;
        }
        if (droppingFire && (position - lastFirePosition).sqrMagnitude < kit.FireSpacing * kit.FireSpacing)
            return;
        droppingFire = true;
        lastFirePosition = position;
        DropFireRow(position);
    }
}
