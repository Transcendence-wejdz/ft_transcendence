using UnityEngine;

// Server-side positioning between combos: sticky targeting, target velocity estimate, strafing and flow-field approach.
public partial class Boss
{
    private const float HoldRangeFraction = 0.8f;
    private const float HoldDistanceFraction = 0.85f;
    private const float RetargetHysteresis = 0.7f;
    private const float VelocitySmoothing = 0.2f;
    private const float StrafeSpeedFraction = 0.6f;
    private const float StrafeFlipMinTime = 1.5f;
    private const float StrafeFlipMaxTime = 3f;
    private const float MinDistance = 0.01f;

    private Player target;
    private Vector2 lastTargetPosition;
    private Vector2 targetVelocity;
    private float nextRetargetTime;

    private float nextSteerTime;
    private Vector2 steerDirection;
    private float strafeSign = 1f;
    private float nextStrafeFlipTime;

    // Flips the strafe direction now and then, so the boss doesn't circle one way forever.
    private void TickStrafeFlip()
    {
        if (Time.time < nextStrafeFlipTime)
            return;
        strafeSign = -strafeSign;
        nextStrafeFlipTime = Time.time + Random.Range(StrafeFlipMinTime, StrafeFlipMaxTime);
    }

    // Circles the target just inside hold range, pulling back onto the hold distance.
    private Vector2 Strafe(Vector2 toTarget, float distance, float holdRange)
    {
        Vector2 toward = Vector2.right;
        float holdDistance = holdRange * HoldDistanceFraction;
        Vector2 tangent;
        Vector2 radial;

        if (distance > MinDistance)
            toward = toTarget / distance;
        TickStrafeFlip();
        tangent = new Vector2(-toward.y, toward.x) * strafeSign;
        radial = toward * Mathf.Clamp(distance - holdDistance, -1f, 1f);
        return (tangent + radial).normalized * kit.MoveSpeed * StrafeSpeedFraction * Tempo;
    }

    private Vector2 ComputeSteerDirection(EnemyFlowFieldManager manager, Vector2 toTarget)
    {
        Vector2 fieldDirection;

        if (manager.TryGetDirection(target, BodyCenter, out fieldDirection))
            return fieldDirection;
        return toTarget.normalized;
    }

    // Strafes just inside slam range, otherwise walks toward the target along the flow field.
    private void Reposition(EnemyFlowFieldManager manager)
    {
        Vector2 toTarget = target.Center - BodyCenter;
        float distance = toTarget.magnitude;
        float holdRange = kit.SlamMaxRange * HoldRangeFraction;

        SetFacing(toTarget.x);
        if (distance <= holdRange)
        {
            rb.linearVelocity = Strafe(toTarget, distance, holdRange);
            return;
        }
        if (Time.time >= nextSteerTime)
        {
            nextSteerTime = Time.time + kit.SteerInterval;
            steerDirection = ComputeSteerDirection(manager, toTarget);
        }
        rb.linearVelocity = steerDirection * kit.MoveSpeed * Tempo;
    }

    // The current target is kept unless the other player is clearly closer, so the boss doesn't dither between two.
    private bool ShouldKeepTarget(Player nearest)
    {
        float nearestSqr;
        float currentSqr;

        if (target == null || target.IsDead)
            return false;
        nearestSqr = (nearest.Center - BodyCenter).sqrMagnitude;
        currentSqr = (target.Center - BodyCenter).sqrMagnitude;
        return nearestSqr >= currentSqr * RetargetHysteresis * RetargetHysteresis;
    }

    private void ChooseTarget(EnemyFlowFieldManager manager)
    {
        Player nearest = manager.FindNearestLivingPlayer(BodyCenter);

        nextRetargetTime = Time.time + kit.RetargetInterval;
        if (nearest == null || nearest == target)
        {
            target = nearest;
            return;
        }
        if (ShouldKeepTarget(nearest))
            return;
        target = nearest;
        lastTargetPosition = nearest.Center;
        targetVelocity = Vector2.zero;
    }

    // Estimated from position deltas, since remote players' rigidbodies don't carry velocity on the server.
    private void TrackTargetVelocity()
    {
        Vector2 position = target.Center;
        Vector2 sample = (position - lastTargetPosition) / Time.fixedDeltaTime;

        lastTargetPosition = position;
        targetVelocity = Vector2.Lerp(targetVelocity, sample, VelocitySmoothing);
    }
}
