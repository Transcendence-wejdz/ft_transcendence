using UnityEngine;

// Server-side movement, the entry point of the enemy AI: TickMovement decides what the enemy does this physics tick.
public partial class Enemy
{
    private static PhysicsMaterial2D slipperyMaterial;

    private float nextLeashCheckTime;
    private float nextAttackTime;
    private float knockbackEndTime;
    private Vector2 knockbackVelocity;

    public bool IsKnockedBack => Time.time < knockbackEndTime;

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

    // Overrides steering for a short window, e.g. a shield push; server-only caller.
    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        knockbackVelocity = velocity;
        knockbackEndTime = Time.time + duration;
    }

    private bool CanAttack(Vector2 toTarget)
    {
        if (isRetreating || IsEncircleHoldingFire)
            return false;
        return toTarget.sqrMagnitude <= Kit.AttackRange * Kit.AttackRange;
    }

    // In range: holds still (ranged shooters drift to spread out) and attacks once the cooldown is over.
    private void HoldAndAttack(Vector2 toTarget)
    {
        rb.linearVelocity = Vector2.zero;
        if (Kit.Behaviour == EnemyBehaviour.Ranged)
            rb.linearVelocity = IdleSpacingVelocity(toTarget);
        if (Time.time < nextAttackTime)
            return;
        nextAttackTime = Time.time + Kit.AttackCooldown;
        StartAttack(target);
        if (Kit.Behaviour == EnemyBehaviour.Encircle)
            OnEncircleAttack();
    }

    // Priority order: knockback, no target, leash, attack windup, attack in range, else move along the heading.
    private void TickMovement()
    {
        Vector2 toTarget;

        if (IsKnockedBack)
        {
            rb.linearVelocity = knockbackVelocity;
            return;
        }
        UpdateTarget();
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
        UpdateRetreat(toTarget);
        if (CanAttack(toTarget))
        {
            HoldAndAttack(toTarget);
            return;
        }
        TickSteering(toTarget);
        rb.linearVelocity = steerDirection * Kit.MoveSpeed * steerSpeedFactor;
    }
}
