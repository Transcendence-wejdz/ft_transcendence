using UnityEngine;

// Encircle behaviour: circles to a ring place around the target (side or back), then spirals in and attacks.
public partial class Enemy
{
    private enum EncircleState { Chasing, Flanking, Engaged }

    private const float AssaultAlignedAngle = 10f;
    private const float AssaultStraightRangeFactor = 1.5f;
    private const float AssaultTurnScale = 30f;
    private const float AssaultMaxTurnWeight = 2f;
    private const float OrbitKeepSideAngle = 150f;

    private EncircleState encircleState;
    private bool hasHitWhileEngaged;
    private bool regroupAfterAttack;
    private float flankStartTime = -1f;
    private bool hasRingSlot;
    private float ringSlotAngle;
    private float orbitSign = 1f;

    // Encircle only: circling to its ring place, set by EnemyFlowFieldManager.
    public bool IsFlanking => encircleState == EncircleState.Flanking;
    public float RingAngle { get; set; }
    public float RingSlotAngle
    {
        get => ringSlotAngle;
        set
        {
            ringSlotAngle = value;
            hasRingSlot = true;
        }
    }

    // Encircle enemies only strike once engaged, and not after deciding to regroup.
    private bool IsEncircleHoldingFire
    {
        get
        {
            if (Kit.Behaviour != EnemyBehaviour.Encircle)
                return false;
            return encircleState != EncircleState.Engaged || regroupAfterAttack;
        }
    }

    private void ResetEncircle()
    {
        encircleState = EncircleState.Chasing;
        hasHitWhileEngaged = false;
        regroupAfterAttack = false;
        flankStartTime = -1f;
        hasRingSlot = false;
    }

    private void StartFlanking()
    {
        encircleState = EncircleState.Flanking;
        flankStartTime = Time.time;
    }

    private void Engage()
    {
        encircleState = EncircleState.Engaged;
        hasHitWhileEngaged = false;
        flankStartTime = -1f;
    }

    // After an attack it circles again on a Regroup Chance roll, or once the target broke past the radius.
    private bool ShouldRegroup(float distance)
    {
        if (encircleState != EncircleState.Engaged || !hasHitWhileEngaged)
            return false;
        return regroupAfterAttack || distance > Kit.EncircleRadius;
    }

    // Signed degrees between where the enemy stands around its target and its ring place.
    private float SlotAngleError(Vector2 toTarget)
    {
        float currentAngle = Mathf.Atan2(-toTarget.y, -toTarget.x) * Mathf.Rad2Deg;

        return Mathf.DeltaAngle(currentAngle, RingSlotAngle);
    }

    // The orbit side only flips when the place is not almost straight opposite, so it never dithers.
    private void UpdateOrbitSign(float angleError)
    {
        if (Mathf.Abs(angleError) < OrbitKeepSideAngle)
            orbitSign = Mathf.Sign(angleError);
    }

    private Vector2 OrbitTangent(Vector2 inward)
    {
        return new Vector2(inward.y, -inward.x) * orbitSign;
    }

    // Circles around the target while pulling back onto the Encircle Radius.
    private Vector2 ComputeOrbitHeading(Vector2 toTarget, float distance)
    {
        Vector2 inward = toTarget / Mathf.Max(distance, MinDistance);
        float radial = Mathf.Clamp(Kit.EncircleRadius - distance, -1f, 1f);

        return (OrbitTangent(inward) - inward * radial).normalized;
    }

    // Engaged: heads in while still turning toward its place; straight in once aligned or about to strike.
    private Vector2 ComputeAssaultDirection(EnemyFlowFieldManager manager, Vector2 toTarget, float distance)
    {
        float angleError;
        float turnWeight;
        Vector2 inward;

        if (!hasRingSlot || distance <= Kit.AttackRange * AssaultStraightRangeFactor)
            return ComputeSteerDirection(manager, toTarget);
        angleError = SlotAngleError(toTarget);
        if (Mathf.Abs(angleError) <= AssaultAlignedAngle)
            return ComputeSteerDirection(manager, toTarget);
        UpdateOrbitSign(angleError);
        inward = toTarget / Mathf.Max(distance, MinDistance);
        turnWeight = Mathf.Clamp(Mathf.Abs(angleError) / AssaultTurnScale, 0f, AssaultMaxTurnWeight);
        return KeepIfClear((inward + OrbitTangent(inward) * turnWeight).normalized, manager, toTarget);
    }

    // No place given yet: backs out to the radius when too close, else keeps chasing.
    private Vector2 ComputeSlotlessDirection(EnemyFlowFieldManager manager, Vector2 toTarget, float distance)
    {
        if (distance < Kit.EncircleRadius)
            return -toTarget / Mathf.Max(distance, MinDistance);
        return ComputeSteerDirection(manager, toTarget);
    }

    // Charges once close enough to its place, or anyway after circling too long.
    private bool IsReadyToEngage(float angleError)
    {
        if (Mathf.Abs(angleError) <= Kit.EncircleSlotTolerance)
            return true;
        return Time.time - flankStartTime >= Kit.EncircleMaxFlankTime;
    }

    private Vector2 ComputeFlankDirection(EnemyFlowFieldManager manager, Vector2 toTarget, float distance)
    {
        float angleError;

        if (encircleState != EncircleState.Flanking)
            StartFlanking();
        if (!hasRingSlot)
            return ComputeSlotlessDirection(manager, toTarget, distance);
        angleError = SlotAngleError(toTarget);
        if (IsReadyToEngage(angleError))
        {
            Engage();
            return ComputeAssaultDirection(manager, toTarget, distance);
        }
        UpdateOrbitSign(angleError);
        return KeepIfClear(ComputeOrbitHeading(toTarget, distance), manager, toTarget);
    }

    // Chases like the horde until Join Distance, circles to its place (side or back), then spirals in and attacks.
    private Vector2 ComputeEncircleDirection(EnemyFlowFieldManager manager, Vector2 toTarget)
    {
        float distance = toTarget.magnitude;

        if (distance > Kit.EncircleJoinDistance)
        {
            ResetEncircle();
            return ComputeSteerDirection(manager, toTarget);
        }
        if (ShouldRegroup(distance))
            ResetEncircle();
        if (encircleState == EncircleState.Engaged)
            return ComputeAssaultDirection(manager, toTarget, distance);
        return ComputeFlankDirection(manager, toTarget, distance);
    }
}
