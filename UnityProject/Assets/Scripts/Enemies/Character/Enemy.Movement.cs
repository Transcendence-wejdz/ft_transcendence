using UnityEngine;

// Server-side chase: targeting, flow-field steering, leash recycling and knockback.
public partial class Enemy
{
    private static PhysicsMaterial2D slipperyMaterial;

    private Player target;
    private int seenPlayersVersion = -1;
    private Vector2 steerDirection;
    private bool hasLineOfSight;
    private float nextRetargetTime;
    private float nextSteerTime;
    private float nextLineOfSightTime;
    private float nextLeashCheckTime;
    private float nextAttackTime;
    private float knockbackEndTime;
    private Vector2 knockbackVelocity;

    // Overrides steering for a short window, e.g. a shield push; server-only caller.
    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        knockbackVelocity = velocity;
        knockbackEndTime = Time.time + duration;
    }

    // Spreads timers so enemies don't all retarget or raycast on the same tick.
    private void StaggerTimers()
    {
        nextRetargetTime = Time.time + Random.value * Kit.RetargetInterval;
        nextSteerTime = Time.time + Random.value * Kit.SteerInterval;
        nextLineOfSightTime = Time.time + Random.value * Kit.LineOfSightInterval;
        nextLeashCheckTime = Time.time + Random.value * Kit.LeashCheckInterval;
    }

    // Knockback, then retarget, leash, attack in range, else steer toward the target.
    private void TickMovement()
    {
        if (Time.time < knockbackEndTime)
        {
            rb.linearVelocity = knockbackVelocity;
            return;
        }

        EnemyFlowFieldManager manager = EnemyFlowFieldManager.Instance;
        if (target == null || target.IsDead || seenPlayersVersion != manager.PlayersVersion || Time.time >= nextRetargetTime)
            ChooseTarget(manager);

        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time >= nextLeashCheckTime)
        {
            nextLeashCheckTime = Time.time + Kit.LeashCheckInterval;
            if (CheckLeash())
                return;
        }

        if (IsWindingUp)
        {
            rb.linearVelocity = steerDirection * Kit.MoveSpeed * Kit.AttackMoveSpeedMultiplier;
            return;
        }

        Vector2 toTarget = target.Center - BodyCenter;
        if (toTarget.sqrMagnitude <= Kit.AttackRange * Kit.AttackRange)
        {
            rb.linearVelocity = Vector2.zero;
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + Kit.AttackCooldown;
                StartAttack(target);
            }
            return;
        }

        if (Time.time >= nextSteerTime)
        {
            nextSteerTime = Time.time + Kit.SteerInterval;
            steerDirection = ComputeSteerDirection(manager, toTarget);
        }
        rb.linearVelocity = steerDirection * Kit.MoveSpeed;
    }

    private void ChooseTarget(EnemyFlowFieldManager manager)
    {
        nextRetargetTime = Time.time + Kit.RetargetInterval;
        seenPlayersVersion = manager.PlayersVersion;
        target = manager.FindNearestLivingPlayer(rb.position);
    }

    // Teleports an enemy left far from every player back near the action.
    private bool CheckLeash()
    {
        float maxDistance = Kit.MaxDistanceFromPlayers;
        if (waveSpawner == null || (target.Center - BodyCenter).sqrMagnitude <= maxDistance * maxDistance)
            return false;

        Vector3 position = waveSpawner.FindSpawnPosition();
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
        if (networkTransform != null && IsSpawned)
            networkTransform.Teleport(position, transform.rotation, transform.localScale);
        else
            transform.position = position;
        return true;
    }

    // Straight at the player with a clear line, otherwise along the flow field.
    private Vector2 ComputeSteerDirection(EnemyFlowFieldManager manager, Vector2 toTarget)
    {
        if (toTarget.sqrMagnitude <= Kit.DirectChaseRange * Kit.DirectChaseRange && HasClearLine(toTarget))
            return toTarget.normalized;
        if (manager.TryGetDirection(target, BodyCenter, out Vector2 fieldDirection))
            return fieldDirection;
        return toTarget.normalized;
    }

    // Cached so cast cost stays flat however many enemies are near.
    private bool HasClearLine(Vector2 toTarget)
    {
        if (Time.time >= nextLineOfSightTime)
        {
            nextLineOfSightTime = Time.time + Kit.LineOfSightInterval;
            hasLineOfSight = ObstacleQuery.IsClear(BodyCenter, BodyCenter + toTarget, bodyRadius * 1.15f);
        }
        return hasLineOfSight;
    }

    // Zero friction so enemies slide along trunks.
    private static PhysicsMaterial2D GetSlipperyMaterial()
    {
        if (slipperyMaterial == null)
            slipperyMaterial = new PhysicsMaterial2D("EnemySlippery") { friction = 0f, bounciness = 0f };
        return slipperyMaterial;
    }
}
