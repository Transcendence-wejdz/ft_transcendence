using UnityEngine;

// Heading of the enemy: recomputed every SteerInterval from its behaviour, then applied every tick by TickMovement.
public partial class Enemy
{
    private const float ProbeDistance = 1f;
    private const float MinDistance = 0.01f;
    private const float MinDirectionSqr = 0.0001f;
    private const float LineOfSightRadiusFactor = 1.15f;

    private Vector2 steerDirection;
    private float steerSpeedFactor = 1f;
    private float nextSteerTime;
    private bool hasLineOfSight;
    private float nextLineOfSightTime;

    private void TickSteering(Vector2 toTarget)
    {
        if (Time.time < nextSteerTime)
            return;
        nextSteerTime = Time.time + Kit.SteerInterval;
        steerSpeedFactor = 1f;
        if (isRetreating)
        {
            steerDirection = retreatDirection;
            steerSpeedFactor = Kit.RetreatSpeedMultiplier;
        }
        else if (Kit.Behaviour == EnemyBehaviour.Ranged)
            steerDirection = RangedChaseDirection(toTarget);
        else if (Kit.Behaviour == EnemyBehaviour.Encircle)
            steerDirection = EncircleDirection(toTarget);
        else
            steerDirection = ChaseDirection(toTarget);
    }

    // Straight at the player with a clear line, otherwise along the flow field.
    private Vector2 ChaseDirection(Vector2 toTarget)
    {
        Vector2 fieldDirection;

        if (toTarget.sqrMagnitude <= Kit.DirectChaseRange * Kit.DirectChaseRange && HasClearLine(toTarget))
            return toTarget.normalized;
        if (EnemyFlowFieldManager.Instance.TryGetPathDirection(target, BodyCenter, bodyRadius, out fieldDirection))
            return fieldDirection;
        return toTarget.normalized;
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

    private bool IsClearAhead(Vector2 direction)
    {
        return ObstacleQuery.IsClear(BodyCenter, BodyCenter + direction * ProbeDistance, bodyRadius);
    }

    // A circling heading is kept only with room ahead, else the enemy falls back to the plain chase.
    private Vector2 HeadingOrChase(Vector2 heading, Vector2 toTarget)
    {
        if (IsClearAhead(heading))
            return heading;
        return ChaseDirection(toTarget);
    }
}
