using UnityEngine;

public partial class Player
{
    [Header("Aim")]
    [Tooltip("Units per second the right stick moves a Hold Free zone at full tilt, before the player's Aim Zone Speed setting.")]
    [SerializeField] private float gamepadAimSpeed = 8f;

    [Tooltip("Gamepad aim assist reach for melee attacks; projectiles use their max distance.")]
    [SerializeField] private float aimAssistMeleeRange = 3f;

    private static readonly Collider2D[] aimAssistBuffer = new Collider2D[64];
    private AbilityType? aimingAbility;
    private float aimDistance = -1f;
    private Vector2 aimTarget;
    private AbilityAimPreview aimPreview;

    private AbilityAimPreview AimPreview
    {
        get
        {
            if (aimPreview == null)
                aimPreview = GetComponentInChildren<AbilityAimPreview>(true);
            return aimPreview;
        }
    }

    private bool IsHoldAim(AbilityType ability) => activeKit.AimMode(ability) != AbilityAimMode.Instant;

    private bool IsFreeAim(AbilityType ability) => activeKit.AimMode(ability) == AbilityAimMode.HoldFree;

    private bool IsProjectile(AbilityType ability)
    {
        GameObject prefab = GetFxPrefab(ability);
        return prefab != null && prefab.GetComponent<ArrowProjectile>() != null;
    }

    private Vector2 AimDirection => new Vector2(Mathf.Cos(AimAngleDegrees * Mathf.Deg2Rad), Mathf.Sin(AimAngleDegrees * Mathf.Deg2Rad));

    private bool GamepadZoneAiming => aimingAbility is AbilityType ability && IsFreeAim(ability) && InputModeTracker.UsingGamepad;

    // While a gamepad places a Hold Free zone, the aim points at the zone instead of following the stick.
    private bool TryGetZoneAimDirection(out Vector2 direction)
    {
        direction = Vector2.zero;
        if (!GamepadZoneAiming)
            return false;

        Vector2 toTarget = aimTarget - FxOffset(aimingAbility.Value, AimDirection, 0f);
        if (toTarget.sqrMagnitude < 0.0001f)
            return false;
        direction = toTarget.normalized;
        return true;
    }

    // Placed distance of an action currently aimed in Hold Free, -1 (the kit's FX distance) otherwise.
    private float AimedDistance(AbilityType ability) => aimingAbility == ability && IsFreeAim(ability) ? aimDistance : -1f;

    // Special and Ultimate only start when they could be cast right now, so the preview never promises a cast that release would refuse; the Attack keeps its preview between autofire shots.
    private void BeginAim(AbilityType ability)
    {
        if (!CanFight)
            return;
        if (ability == AbilityType.Special && (Time.time < specialReadyTime || CurrentMana < activeKit.SpecialManaCost))
            return;
        if (ability == AbilityType.Ultimate && (Time.time < ultimateReadyTime || CurrentMana < activeKit.UltimateManaCost))
            return;

        CancelAim();
        aimingAbility = ability;
        aimDistance = ability switch
        {
            AbilityType.Special => activeKit.SpecialFxDistance,
            AbilityType.Ultimate => activeKit.UltimateFxDistance,
            _ => activeKit.AttackFxDistance,
        };
        aimTarget = FxOffset(ability, AimDirection, aimDistance);
    }

    // Casts the aimed Special or Ultimate on release; the Attack just stops showing its preview.
    private void ReleaseAim(AbilityType ability)
    {
        if (aimingAbility != ability)
            return;

        float distance = AimedDistance(ability);
        CancelAim();
        if (ability == AbilityType.Special)
            CastSpecial(distance);
        else if (ability == AbilityType.Ultimate)
            CastUltimate(distance);
    }

    // A gamepad zone leaves the stick aim where the zone pointed, so the aim does not snap back after the cast.
    private void CancelAim()
    {
        if (GamepadZoneAiming)
            stickAim = AimDirection;
        aimingAbility = null;
        if (AimPreview != null)
            AimPreview.Hide();
    }

    // Follows the aim every frame while held (a projectile shows its straight path to max distance, nothing when the setting hides it); dying, pausing or switching to an instant mode drops the aim without casting.
    private void UpdateAim()
    {
        if (aimingAbility is not AbilityType ability)
            return;
        if (!CanFight || !this.IsLocallyControlled() || !IsHoldAim(ability))
        {
            CancelAim();
            return;
        }

        if (IsFreeAim(ability))
            aimDistance = PlacedDistance(ability);

        if (AimPreview == null)
            return;
        if (!GameSettings.ShowAimPreview(ability))
        {
            AimPreview.Hide();
            return;
        }
        FxPose(ability, AssistedAimAngle(ability), AimedDistance(ability), out Vector3 position, out Quaternion rotation);
        if (IsProjectile(ability))
            AimPreview.ShowLine(position, position + rotation * Vector3.right * activeKit.ProjectileMaxDistance);
        else
            AimPreview.Show(GetFxPrefab(ability), position, rotation, FxScaleMultiplier(ability));
    }

    // Gamepad Attack with aim assist on (kit and setting): bends the aim toward the target closest to it within the assist cone and reach; otherwise the plain aim.
    private float AssistedAimAngle(AbilityType ability)
    {
        float angle = AimAngleDegrees;
        if (ability != AbilityType.Attack || !activeKit.AttackAimAssist || !InputModeTracker.UsingGamepad || !GameSettings.AimAssist || GamepadZoneAiming)
            return angle;

        float range = IsProjectile(ability) ? activeKit.ProjectileMaxDistance : aimAssistMeleeRange;
        Vector2 origin = (Vector2)transform.position + FxOffset(ability, AimDirection, 0f);

        int count = Physics2D.OverlapCircle(origin, range, ContactFilter2D.noFilter, aimAssistBuffer);
        float bestDelta = activeKit.AttackAimAssistAngle;
        float bestAngle = angle;
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = aimAssistBuffer[i];
            if (hit.isTrigger || hit.GetComponentInParent<IDamageable>() is not Component target || target is Player)
                continue;

            Vector2 toTarget = (Vector2)hit.bounds.center - origin;
            if (toTarget.sqrMagnitude < 0.0001f)
                continue;
            float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float delta = Mathf.Abs(Mathf.DeltaAngle(angle, targetAngle));
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestAngle = targetAngle;
            }
        }
        return bestAngle;
    }

    // Mouse: distance from the FX origin to the cursor; gamepad: the right stick moves the zone at a set speed and it stays put once released. Both capped at Max Range.
    private float PlacedDistance(AbilityType ability)
    {
        float maxRange = activeKit.MaxRange(ability);
        if (InputModeTracker.UsingGamepad)
        {
            Vector2 stick = AimStickVector();
            if (stick.sqrMagnitude > 0.0001f)
                aimTarget += Vector2.ClampMagnitude(stick, 1f) * (gamepadAimSpeed * GameSettings.GamepadAimSpeed * Time.deltaTime);

            Vector2 origin = FxOffset(ability, AimDirection, 0f);
            Vector2 toTarget = Vector2.ClampMagnitude(aimTarget - origin, maxRange);
            aimTarget = origin + toTarget;
            return toTarget.magnitude;
        }

        if (!TryGetMouseWorld(out Vector2 mouse))
            return Mathf.Min(aimDistance, maxRange);

        Vector2 mouseOrigin = (Vector2)transform.position + FxOffset(ability, AimDirection, 0f);
        return Mathf.Min(Vector2.Distance(mouseOrigin, mouse), maxRange);
    }
}
