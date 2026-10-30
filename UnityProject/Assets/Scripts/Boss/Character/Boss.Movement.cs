using UnityEngine;

// Server-side positioning between combos: sticky targeting, target velocity estimate, strafing and flow-field approach.
public partial class Boss
{
    private const float HoldRangeFraction = 0.8f;
    private const float RetargetHysteresis = 0.7f;
    private const float VelocitySmoothing = 0.2f;
    private const float StrafeSpeedFraction = 0.6f;

    private Player target;
    private Vector2 lastTargetPosition;
    private Vector2 targetVelocity;
    private float nextRetargetTime;
    private float nextSteerTime;
    private Vector2 steerDirection;
    private float strafeSign = 1f;
    private float nextStrafeFlipTime;

    // Strafes just inside slam range, otherwise walks toward the target along the flow field.
    private void Reposition(EnemyFlowFieldManager manager)
    {
        Vector2 toTarget = target.Center - BodyCenter;
        float distance = toTarget.magnitude;
        SetFacing(toTarget.x);

        float holdRange = kit.SlamMaxRange * HoldRangeFraction;
        if (distance <= holdRange)
        {
            rb.linearVelocity = Strafe(toTarget, distance, holdRange);
            return;
        }

        if (Time.time >= nextSteerTime)
        {
            nextSteerTime = Time.time + kit.SteerInterval;
            steerDirection = manager.TryGetDirection(target, BodyCenter, out Vector2 fieldDirection) ? fieldDirection : toTarget.normalized;
        }
        rb.linearVelocity = steerDirection * kit.MoveSpeed * Tempo;
    }

    // Circles the target just inside hold range, flipping direction now and then.
    private Vector2 Strafe(Vector2 toTarget, float distance, float holdRange)
    {
        Vector2 toward = distance > 0.01f ? toTarget / distance : Vector2.right;
        if (Time.time >= nextStrafeFlipTime)
        {
            strafeSign = -strafeSign;
            nextStrafeFlipTime = Time.time + Random.Range(1.5f, 3f);
        }

        float holdDistance = holdRange * 0.85f;
        Vector2 tangent = new Vector2(-toward.y, toward.x) * strafeSign;
        Vector2 radial = toward * Mathf.Clamp(distance - holdDistance, -1f, 1f);
        return (tangent + radial).normalized * kit.MoveSpeed * StrafeSpeedFraction * Tempo;
    }

    // Keeps the current target unless another living player is clearly closer, so the boss doesn't dither between two.
    private void ChooseTarget(EnemyFlowFieldManager manager)
    {
        nextRetargetTime = Time.time + kit.RetargetInterval;
        Player nearest = manager.FindNearestLivingPlayer(BodyCenter);
        if (nearest == null || nearest == target)
        {
            target = nearest;
            return;
        }

        bool keepCurrent = target != null && !target.IsDead
            && (nearest.Center - BodyCenter).sqrMagnitude >= (target.Center - BodyCenter).sqrMagnitude * RetargetHysteresis * RetargetHysteresis;
        if (keepCurrent)
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
