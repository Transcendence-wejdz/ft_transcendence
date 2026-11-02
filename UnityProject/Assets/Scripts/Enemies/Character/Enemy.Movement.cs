using UnityEngine;

// Server-side chase shared by every EnemyBehaviour: targeting, flow-field steering, leash recycling and knockback.
public partial class Enemy
{
    private const float ProbeDistance = 1f;
    private const float MinDistance = 0.01f;
    private const float MinDirectionSqr = 0.0001f;
    private const float LineOfSightRadiusFactor = 1.15f;

    private static PhysicsMaterial2D slipperyMaterial;

    private Player target;
    private int seenPlayersVersion = -1;
    private float nextRetargetTime;

    private Vector2 steerDirection;
    private float steerSpeedMultiplier = 1f;
    private float nextSteerTime;
    private bool hasLineOfSight;
    private float nextLineOfSightTime;

    private float nextLeashCheckTime;
    private float nextAttackTime;
    private float knockbackEndTime;
    private Vector2 knockbackVelocity;

    public Player Target => target;

    // Zero friction so enemies slide along trunks.
    private static PhysicsMaterial2D GetSlipperyMaterial()
    {
        if (slipperyMaterial == null)
            slipperyMaterial = new PhysicsMaterial2D("EnemySlippery") { friction = 0f, bounciness = 0f };
        return slipperyMaterial;
    }

    // Spreads timers so enemies don't all retarget or raycast on the same tick.
    private void StaggerTimers()
    {
        nextRetargetTime = Time.time + Random.value * Kit.RetargetInterval;
        nextSteerTime = Time.time + Random.value * Kit.SteerInterval;
        nextLineOfSightTime = Time.time + Random.value * Kit.LineOfSightInterval;
        nextLeashCheckTime = Time.time + Random.value * Kit.LeashCheckInterval;
    }

    private bool NeedsNewTarget(EnemyFlowFieldManager manager)
    {
        if (target == null || target.IsDead)
            return true;
        if (seenPlayersVersion != manager.PlayersVersion)
            return true;
        return Time.time >= nextRetargetTime;
    }

    private void ChooseTarget(EnemyFlowFieldManager manager)
    {
        Player previous = target;

        nextRetargetTime = Time.time + Kit.RetargetInterval;
        seenPlayersVersion = manager.PlayersVersion;
        target = manager.FindNearestLivingPlayer(rb.position);
        if (target != previous)
            ResetEncircle();
        if (Kit.Behaviour == EnemyBehaviour.Encircle)
            manager.RegisterEncircler(this);
    }

    // Cached so cast cost stays flat however many enemies are near.
    private bool HasClearLine(Vector2 toTarget)
    {
        if (Time.time >= nextLineOfSightTime)
        {
            nextLineOfSightTime = Time.time + Kit.LineOfSightInterval;
            hasLineOfSight = ObstacleQuery.IsClear(BodyCenter, BodyCenter + toTarget, bodyRadius * LineOfSightRadiusFactor);
        }
        return hasLineOfSight;
    }

    // Straight at the player with a clear line, otherwise along the flow field.
    private Vector2 ComputeSteerDirection(EnemyFlowFieldManager manager, Vector2 toTarget)
    {
        Vector2 fieldDirection;

        if (toTarget.sqrMagnitude <= Kit.DirectChaseRange * Kit.DirectChaseRange && HasClearLine(toTarget))
            return toTarget.normalized;
        if (manager.TryGetDirection(target, BodyCenter, out fieldDirection))
            return fieldDirection;
        return toTarget.normalized;
    }

    private bool IsClearAhead(Vector2 direction)
    {
        return ObstacleQuery.IsClear(BodyCenter, BodyCenter + direction * ProbeDistance, bodyRadius);
    }

    // A circling heading is kept only with room ahead, else the enemy falls back to plain steering.
    private Vector2 KeepIfClear(Vector2 heading, EnemyFlowFieldManager manager, Vector2 toTarget)
    {
        if (IsClearAhead(heading))
            return heading;
        return ComputeSteerDirection(manager, toTarget);
    }

    private Vector2 ComputeBehaviourDirection(EnemyFlowFieldManager manager, Vector2 toTarget, bool retreating)
    {
        if (Kit.Behaviour == EnemyBehaviour.Ranged)
            return ComputeRangedDirection(manager, toTarget, retreating);
        if (Kit.Behaviour == EnemyBehaviour.Encircle)
            return ComputeEncircleDirection(manager, toTarget);
        return ComputeSteerDirection(manager, toTarget);
    }

    private void TickSteering(EnemyFlowFieldManager manager, Vector2 toTarget, bool retreating)
    {
        if (Time.time < nextSteerTime)
            return;
        nextSteerTime = Time.time + Kit.SteerInterval;
        steerSpeedMultiplier = 1f;
        steerDirection = ComputeBehaviourDirection(manager, toTarget, retreating);
    }

    private bool CanAttackFrom(Vector2 toTarget, bool retreating)
    {
        if (retreating)
            return false;
        if (toTarget.sqrMagnitude > Kit.AttackRange * Kit.AttackRange)
            return false;
        return !IsEncircleHoldingFire;
    }

    // In range: holds still (ranged shooters drift to spread out) and attacks once the cooldown is over.
    private void HoldAndAttack(Vector2 toTarget)
    {
        if (Kit.Behaviour == EnemyBehaviour.Ranged)
            rb.linearVelocity = IdleSpacingVelocity(toTarget);
        else
            rb.linearVelocity = Vector2.zero;
        if (Time.time < nextAttackTime)
            return;
        nextAttackTime = Time.time + Kit.AttackCooldown;
        StartAttack(target);
        hasHitWhileEngaged = true;
        if (encircleState == EncircleState.Engaged && Random.value < Kit.EncircleRegroupChance)
            regroupAfterAttack = true;
    }

    // Overrides steering for a short window, e.g. a shield push; server-only caller.
    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        knockbackVelocity = velocity;
        knockbackEndTime = Time.time + duration;
    }

    // Knockback, then retarget, leash, attack in range, else steer toward the target.
    private void TickMovement()
    {
        EnemyFlowFieldManager manager;
        Vector2 toTarget;
        bool retreating;

        if (Time.time < knockbackEndTime)
        {
            rb.linearVelocity = knockbackVelocity;
            return;
        }
        manager = EnemyFlowFieldManager.Instance;
        if (NeedsNewTarget(manager))
            ChooseTarget(manager);
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (TickLeash())
            return;
        if (IsWindingUp)
        {
            rb.linearVelocity = steerDirection * Kit.MoveSpeed * Kit.AttackMoveSpeedMultiplier;
            return;
        }
        toTarget = target.Center - BodyCenter;
        retreating = IsRetreatingBetweenShots(toTarget);
        if (CanAttackFrom(toTarget, retreating))
        {
            HoldAndAttack(toTarget);
            return;
        }
        TickSteering(manager, toTarget, retreating);
        rb.linearVelocity = steerDirection * Kit.MoveSpeed * steerSpeedMultiplier;
    }
}
