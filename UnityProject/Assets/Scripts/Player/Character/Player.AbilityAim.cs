using UnityEngine;

// Hold-to-aim for actions: preview while held, placed distance for Hold Free zones, gamepad aim assist.
public partial class Player
{
    private const float MinAimVectorSqr = 0.0001f;

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

    private Vector2 AimDirection => new Vector2(Mathf.Cos(AimAngleDegrees * Mathf.Deg2Rad), Mathf.Sin(AimAngleDegrees * Mathf.Deg2Rad));

    private bool GamepadZoneAiming
    {
        get
        {
            if (!aimingAbility.HasValue || !InputModeTracker.UsingGamepad)
                return false;
            return IsFreeAim(aimingAbility.Value);
        }
    }

    private bool IsAimingSpecialOrUltimate => aimingAbility == AbilityType.Special || aimingAbility == AbilityType.Ultimate;

    private bool IsHoldAim(AbilityType ability)
    {
        AbilityKit kit = Ability(ability);

        return kit != null && kit.AimMode != AbilityAimMode.Instant;
    }

    private bool IsFreeAim(AbilityType ability)
    {
        AbilityKit kit = Ability(ability);

        return kit != null && kit.AimMode == AbilityAimMode.HoldFree;
    }

    private bool IsProjectile(AbilityType ability)
    {
        AbilityKit kit = Ability(ability);

        return kit != null && kit.Kind == AbilityKind.Projectile;
    }

    // World point the action's FX starts from, before any distance along the aim.
    private Vector2 FxOriginWorld(AbilityType ability)
    {
        return (Vector2)transform.position + FxOffset(ability, AimDirection, 0f);
    }

    private static float AngleOf(Vector2 vector)
    {
        return Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
    }

    // While a gamepad places a Hold Free zone, the aim points at the zone instead of following the stick.
    private bool TryGetZoneAimDirection(out Vector2 direction)
    {
        Vector2 toTarget;

        direction = Vector2.zero;
        if (!GamepadZoneAiming)
            return false;
        toTarget = aimTarget - FxOffset(aimingAbility.Value, AimDirection, 0f);
        if (toTarget.sqrMagnitude < MinAimVectorSqr)
            return false;
        direction = toTarget.normalized;
        return true;
    }

    // Placed distance of an action currently aimed in Hold Free, -1 (the kit's FX distance) otherwise.
    private float AimedDistance(AbilityType ability)
    {
        if (aimingAbility != ability || !IsFreeAim(ability))
            return -1f;
        return aimDistance;
    }

    // A Special or Ultimate only aims while it is off cooldown and affordable.
    private bool IsCastableNow(AbilityType ability, AbilityKit kit)
    {
        if (ability == AbilityType.Special)
            return Time.time >= specialReadyTime && CurrentMana >= kit.ManaCost;
        if (ability == AbilityType.Ultimate)
            return Time.time >= ultimateReadyTime && CurrentMana >= kit.ManaCost;
        return true;
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

    // Special and Ultimate only start when castable now; the Attack preview never replaces a Special/Ultimate aim in progress (it keeps autofiring without one).
    private void BeginAim(AbilityType ability)
    {
        AbilityKit kit;

        if (!CanFight)
            return;
        if (ability == AbilityType.Attack && IsAimingSpecialOrUltimate)
            return;
        kit = Ability(ability);
        if (kit == null || !IsCastableNow(ability, kit))
            return;
        CancelAim();
        aimingAbility = ability;
        aimDistance = kit.FxDistance;
        aimTarget = FxOffset(ability, AimDirection, aimDistance);
    }

    // Casts the aimed Special or Ultimate on release, then brings back the Attack preview if Attack is still held; the Attack just stops showing its preview.
    private void ReleaseAim(AbilityType ability)
    {
        float distance;

        if (aimingAbility != ability)
            return;
        distance = AimedDistance(ability);
        CancelAim();
        if (ability == AbilityType.Special)
            CastSpecial(distance);
        else if (ability == AbilityType.Ultimate)
            CastUltimate(distance);
        if (ability != AbilityType.Attack && isAttackHeld && IsHoldAim(AbilityType.Attack))
            BeginAim(AbilityType.Attack);
    }

    // A projectile shows its straight path to max distance, other actions their FX at the aimed spot.
    private void ShowAimPreview(AbilityType ability)
    {
        Vector3 position;
        Quaternion rotation;

        FxPose(ability, AssistedAimAngle(ability), AimedDistance(ability), out position, out rotation);
        if (IsProjectile(ability))
            AimPreview.ShowLine(position, position + rotation * Vector3.right * Ability(ability).ProjectileMaxDistance);
        else
            AimPreview.Show(GetFxPrefab(ability), position, rotation, FxScaleMultiplier(ability));
    }

    // Follows the aim every frame while held (nothing when the setting hides it); dying, pausing or switching to an instant mode drops the aim without casting.
    private void UpdateAim()
    {
        AbilityType ability;

        if (!aimingAbility.HasValue)
            return;
        ability = aimingAbility.Value;
        if (!CanFight || !this.IsLocallyControlled() || !IsHoldAim(ability))
        {
            CancelAim();
            return;
        }
        if (IsFreeAim(ability))
            aimDistance = PlacedDistance(ability);
        if (AimPreview == null)
            return;
        if (GameSettings.ShowAimPreview(ability))
            ShowAimPreview(ability);
        else
            AimPreview.Hide();
    }

}
