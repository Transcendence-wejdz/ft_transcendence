using UnityEngine;

// Ranged behaviour: backs off between shots when too close and spreads out from other shooters.
public partial class Enemy
{
    private const float SpacingIdleSpeed = 0.6f;
    private const float SpacingOuterRangeFactor = 0.9f;
    private const float SpacingKeepAwayMargin = 1f;

    private static readonly float[] RetreatAngles = { 0f, 45f, -45f, 90f, -90f };
    private static readonly Collider2D[] spacingBuffer = new Collider2D[16];
    private static int enemyLayerMask = -1;

    private bool isRetreating;
    private float nextRetreatProbeTime;
    private Vector2 retreatDirection;
    private Vector2 spacingPush;
    private float nextSpacingTime;

    private static ContactFilter2D GetEnemyFilter()
    {
        if (enemyLayerMask < 0)
            enemyLayerMask = LayerMask.GetMask("Enemy");
        return new ContactFilter2D { useLayerMask = true, layerMask = enemyLayerMask };
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }

    // Away from the target first, then the two diagonals and sides, keeping the first one with a clear step.
    private bool FindRetreatDirection(Vector2 toTarget, out Vector2 direction)
    {
        Vector2 away = -toTarget.normalized;

        foreach (float angle in RetreatAngles)
        {
            direction = Rotate(away, angle);
            if (IsClearAhead(direction))
                return true;
        }
        direction = Vector2.zero;
        return false;
    }

    // Too close to its target with room behind, a ranged enemy backs off; re-probed on the steer interval.
    private bool UpdateRetreat(Vector2 toTarget)
    {
        if (toTarget.sqrMagnitude >= Kit.KeepAwayDistance * Kit.KeepAwayDistance)
        {
            isRetreating = false;
            return false;
        }
        if (Time.time >= nextRetreatProbeTime)
        {
            nextRetreatProbeTime = Time.time + Kit.SteerInterval;
            isRetreating = FindRetreatDirection(toTarget, out retreatDirection);
        }
        return isRetreating;
    }

    // Only between shots: a ranged enemy never backs off while its attack is ready.
    private bool IsRetreatingBetweenShots(Vector2 toTarget)
    {
        if (Kit.Behaviour != EnemyBehaviour.Ranged)
            return false;
        if (Time.time >= nextAttackTime)
            return false;
        return UpdateRetreat(toTarget);
    }

    // Away from a neighbour, stronger the closer it is; a random push when both stand on the same spot.
    private Vector2 PushAwayFrom(Vector2 center, Vector2 otherCenter)
    {
        Vector2 away = center - otherCenter;
        float distance = away.magnitude;

        if (distance < MinDistance)
            return Random.insideUnitCircle.normalized;
        return away / distance * (1f - distance / Kit.RangedSpacing);
    }

    private Vector2 SumSpacingPushes()
    {
        Vector2 center = BodyCenter;
        Vector2 push = Vector2.zero;
        int count = Physics2D.OverlapCircle(center, Kit.RangedSpacing, GetEnemyFilter(), spacingBuffer);
        Enemy other;

        for (int i = 0; i < count; i++)
        {
            other = spacingBuffer[i].GetComponentInParent<Enemy>();
            if (other == null || other == this)
                continue;
            push += PushAwayFrom(center, other.BodyCenter);
        }
        return push;
    }

    // Pushes away from enemies inside Ranged Spacing, capped at 1; refreshed on the steer interval.
    private Vector2 SpacingPush()
    {
        if (Time.time < nextSpacingTime || Kit.RangedSpacing <= 0f)
            return spacingPush;
        nextSpacingTime = Time.time + Kit.SteerInterval;
        spacingPush = Vector2.ClampMagnitude(SumSpacingPushes(), 1f);
        return spacingPush;
    }

    // True when the push would take the shooter out past its range or in toward its keep-away distance.
    private bool LeavesRangeBand(float outwardAmount, float distance)
    {
        if (outwardAmount > 0f && distance > Kit.AttackRange * SpacingOuterRangeFactor)
            return true;
        return outwardAmount < 0f && distance < Kit.KeepAwayDistance + SpacingKeepAwayMargin;
    }

    // In range between shots: drifts sideways away from nearby enemies, so shooters spread along their range.
    private Vector2 IdleSpacingVelocity(Vector2 toTarget)
    {
        Vector2 push = SpacingPush();
        float distance = toTarget.magnitude;
        Vector2 outward;
        float outwardAmount;

        if (distance < MinDistance)
            return Vector2.zero;
        outward = -toTarget / distance;
        outwardAmount = Vector2.Dot(push, outward);
        if (LeavesRangeBand(outwardAmount, distance))
            push -= outward * outwardAmount;
        return push * Kit.MoveSpeed * SpacingIdleSpeed;
    }

    // Backs away (or sideways) under Keep Away Distance, otherwise chases until in range.
    private Vector2 ComputeRangedDirection(EnemyFlowFieldManager manager, Vector2 toTarget, bool retreating)
    {
        Vector2 chase;

        if (retreating)
        {
            steerSpeedMultiplier = Kit.RetreatSpeedMultiplier;
            return retreatDirection;
        }
        chase = ComputeSteerDirection(manager, toTarget) + SpacingPush();
        if (chase.sqrMagnitude <= MinDirectionSqr)
            return chase;
        return chase.normalized;
    }
}
