using UnityEngine;

// Ranged behaviour: chases until in range, backs off between shots when too close, spreads out from other enemies.
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

    // Only between shots and under Keep Away Distance, with room behind; re-probed on the steer interval.
    private void UpdateRetreat(Vector2 toTarget)
    {
        if (Kit.Behaviour != EnemyBehaviour.Ranged || Time.time >= nextAttackTime
            || toTarget.sqrMagnitude >= Kit.KeepAwayDistance * Kit.KeepAwayDistance)
        {
            isRetreating = false;
            return;
        }
        if (Time.time < nextRetreatProbeTime)
            return;
        nextRetreatProbeTime = Time.time + Kit.SteerInterval;
        isRetreating = FindRetreatDirection(toTarget, out retreatDirection);
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

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }

    private Vector2 RangedChaseDirection(Vector2 toTarget)
    {
        Vector2 chase = ChaseDirection(toTarget) + SpacingPush();

        if (chase.sqrMagnitude <= MinDirectionSqr)
            return chase;
        return chase.normalized;
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

    // True when the push would take the shooter out past its range or in toward its keep-away distance.
    private bool LeavesRangeBand(float outwardAmount, float distance)
    {
        if (outwardAmount > 0f && distance > Kit.AttackRange * SpacingOuterRangeFactor)
            return true;
        return outwardAmount < 0f && distance < Kit.KeepAwayDistance + SpacingKeepAwayMargin;
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

    // Away from a neighbour, stronger the closer it is; a random push when both stand on the same spot.
    private Vector2 PushAwayFrom(Vector2 center, Vector2 otherCenter)
    {
        Vector2 away = center - otherCenter;
        float distance = away.magnitude;

        if (distance < MinDistance)
            return Random.insideUnitCircle.normalized;
        return away / distance * (1f - distance / Kit.RangedSpacing);
    }

    private static ContactFilter2D GetEnemyFilter()
    {
        if (enemyLayerMask < 0)
            enemyLayerMask = LayerMask.GetMask("Enemy");
        return new ContactFilter2D { useLayerMask = true, layerMask = enemyLayerMask };
    }
}
