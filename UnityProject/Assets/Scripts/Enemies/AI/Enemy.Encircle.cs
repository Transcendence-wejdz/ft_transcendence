using UnityEngine;

// Encircle behaviour, three states: Chasing (far away), Flanking (orbits to its ring place), Engaged (spirals in and attacks).
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

    // Read and written by EnemyFlowFieldManager.Rings, which hands out the ring places.
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

    // State transitions happen here, then the current state gives the heading.
    private Vector2 EncircleDirection(Vector2 toTarget)
    {
        float distance = toTarget.magnitude;

        if (distance > Kit.EncircleJoinDistance)
        {
            ResetEncircle();
            return ChaseDirection(toTarget);
        }
        if (ShouldRegroup(distance))
            ResetEncircle();
        if (encircleState == EncircleState.Chasing)
            StartFlanking();
        if (encircleState == EncircleState.Flanking && hasRingSlot && IsReadyToEngage(SlotAngleError(toTarget)))
            Engage();
        if (encircleState == EncircleState.Engaged)
            return AssaultDirection(toTarget, distance);
        return FlankDirection(toTarget, distance);
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

    private void OnEncircleAttack()
    {
        hasHitWhileEngaged = true;
        if (Random.value < Kit.EncircleRegroupChance)
            regroupAfterAttack = true;
    }

    // After an attack it circles again on a Regroup Chance roll, or once the target broke past the radius.
    private bool ShouldRegroup(float distance)
    {
        if (encircleState != EncircleState.Engaged || !hasHitWhileEngaged)
            return false;
        return regroupAfterAttack || distance > Kit.EncircleRadius;
    }

    // Charges once close enough to its place, or anyway after circling too long.
    private bool IsReadyToEngage(float angleError)
    {
        if (Mathf.Abs(angleError) <= Kit.EncircleSlotTolerance)
            return true;
        return Time.time - flankStartTime >= Kit.EncircleMaxFlankTime;
    }

    // Orbits toward its place; with no place yet, backs out to the radius when too close, else keeps chasing.
    private Vector2 FlankDirection(Vector2 toTarget, float distance)
    {
        if (!hasRingSlot)
        {
            if (distance < Kit.EncircleRadius)
                return -toTarget / Mathf.Max(distance, MinDistance);
            return ChaseDirection(toTarget);
        }
        UpdateOrbitSign(SlotAngleError(toTarget));
        return HeadingOrChase(OrbitHeading(toTarget, distance), toTarget);
    }

    // Heads in while still turning toward its place; straight in once aligned or about to strike.
    private Vector2 AssaultDirection(Vector2 toTarget, float distance)
    {
        float angleError;
        float turnWeight;
        Vector2 inward;

        if (!hasRingSlot || distance <= Kit.AttackRange * AssaultStraightRangeFactor)
            return ChaseDirection(toTarget);
        angleError = SlotAngleError(toTarget);
        if (Mathf.Abs(angleError) <= AssaultAlignedAngle)
            return ChaseDirection(toTarget);
        UpdateOrbitSign(angleError);
        inward = toTarget / Mathf.Max(distance, MinDistance);
        turnWeight = Mathf.Clamp(Mathf.Abs(angleError) / AssaultTurnScale, 0f, AssaultMaxTurnWeight);
        return HeadingOrChase((inward + OrbitTangent(inward) * turnWeight).normalized, toTarget);
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
    private Vector2 OrbitHeading(Vector2 toTarget, float distance)
    {
        Vector2 inward = toTarget / Mathf.Max(distance, MinDistance);
        float radial = Mathf.Clamp(Kit.EncircleRadius - distance, -1f, 1f);

        return (OrbitTangent(inward) - inward * radial).normalized;
    }
}
