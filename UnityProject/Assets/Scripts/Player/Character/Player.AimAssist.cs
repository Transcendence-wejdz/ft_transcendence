using UnityEngine;

// Gamepad aim assist and Hold Free placement: the angle bent toward a target, the zone moved by stick or mouse.
public partial class Player
{
    // Mouse: angle from where the action's FX starts (e.g. the aim ring, below the feet) to the cursor, so shots pass under the crosshair; the player's aim is measured from its feet. Gamepad keeps the stick angle.
    private float ActionAimAngle(AbilityType ability)
    {
        Vector2 mouse;
        Vector2 toMouse;

        if (InputModeTracker.UsingGamepad || !TryGetMouseWorld(out mouse))
            return AimAngleDegrees;
        toMouse = mouse - FxOriginWorld(ability);
        if (toMouse.sqrMagnitude < MinAimVectorSqr)
            return AimAngleDegrees;
        return AngleOf(toMouse);
    }

    private bool UsesAimAssist(AbilityKit kit)
    {
        if (kit == null || !kit.AimAssist)
            return false;
        if (!InputModeTracker.UsingGamepad || !GameSettings.AimAssist)
            return false;
        return !GamepadZoneAiming;
    }

    private float AimAssistRange(AbilityKit kit)
    {
        if (kit.Kind == AbilityKind.Projectile)
            return kit.ProjectileMaxDistance;
        return aimAssistMeleeRange;
    }

    // Only solid damageable non-player targets can pull the aim.
    private static bool IsAssistTarget(Collider2D hit)
    {
        Component target;

        if (hit.isTrigger)
            return false;
        target = hit.GetComponentInParent<IDamageable>() as Component;
        return target != null && !(target is Player);
    }

    // Bends the aim toward the target closest to it within the assist cone and reach.
    private float ClosestAssistAngle(Vector2 origin, float range, float angle, float maxDelta)
    {
        int count = Physics2D.OverlapCircle(origin, range, ContactFilter2D.noFilter, aimAssistBuffer);
        float bestDelta = maxDelta;
        float bestAngle = angle;
        Vector2 toTarget;
        float targetAngle;
        float delta;

        for (int i = 0; i < count; i++)
        {
            if (!IsAssistTarget(aimAssistBuffer[i]))
                continue;
            toTarget = (Vector2)aimAssistBuffer[i].bounds.center - origin;
            if (toTarget.sqrMagnitude < MinAimVectorSqr)
                continue;
            targetAngle = AngleOf(toTarget);
            delta = Mathf.Abs(Mathf.DeltaAngle(angle, targetAngle));
            if (delta >= bestDelta)
                continue;
            bestDelta = delta;
            bestAngle = targetAngle;
        }
        return bestAngle;
    }

    // Gamepad action with aim assist on (ability and setting): the assisted angle; otherwise the plain aim.
    private float AssistedAimAngle(AbilityType ability)
    {
        float angle = ActionAimAngle(ability);
        AbilityKit kit = Ability(ability);

        if (!UsesAimAssist(kit))
            return angle;
        return ClosestAssistAngle(FxOriginWorld(ability), AimAssistRange(kit), angle, kit.AimAssistAngle);
    }

    // The right stick moves the zone at a set speed and it stays put once released.
    private float PlacedDistanceGamepad(AbilityType ability, float maxRange)
    {
        Vector2 stick = AimStickVector();
        Vector2 origin;
        Vector2 toTarget;

        if (stick.sqrMagnitude > MinAimVectorSqr)
            aimTarget += Vector2.ClampMagnitude(stick, 1f) * (gamepadAimSpeed * GameSettings.GamepadAimSpeed * Time.deltaTime);
        origin = FxOffset(ability, AimDirection, 0f);
        toTarget = Vector2.ClampMagnitude(aimTarget - origin, maxRange);
        aimTarget = origin + toTarget;
        return toTarget.magnitude;
    }

    // Mouse: distance from the FX origin to the cursor; gamepad: see PlacedDistanceGamepad. Both capped at Max Range.
    private float PlacedDistance(AbilityType ability)
    {
        float maxRange = Ability(ability).MaxRange;
        Vector2 mouse;

        if (InputModeTracker.UsingGamepad)
            return PlacedDistanceGamepad(ability, maxRange);
        if (!TryGetMouseWorld(out mouse))
            return Mathf.Min(aimDistance, maxRange);
        return Mathf.Min(Vector2.Distance(FxOriginWorld(ability), mouse), maxRange);
    }
}
