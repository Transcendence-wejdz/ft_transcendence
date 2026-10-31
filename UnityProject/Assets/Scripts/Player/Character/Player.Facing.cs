using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Owner-side aim (mouse, stick or placed zone), replicated aim angle and 8-way facing.
public partial class Player
{
    private const float FacingSwitchDeadzoneDeg = 10f;
    private const float BackwardDotThreshold = -0.1f;
    private const float OctantDegrees = 45f;

    private static readonly Vector2[] FacingOctants =
    {
        new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-1f, 1f),
        new Vector2(-1f, 0f), new Vector2(-1f, -1f), new Vector2(0f, -1f), new Vector2(1f, -1f),
    };

    private static readonly int MoveSpeedMultHash = Animator.StringToHash("MoveSpeedMult");
    private static readonly int LastInputYHash = Animator.StringToHash("LastInputY");

    private readonly NetworkVariable<float> aimAngle = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private Camera cam;
    private Vector2 facing = Vector2.down;
    private Vector2 stickAim = Vector2.down;
    private InputAction aimAction;
    private bool facingApplied;
    private float moveSpeedMult = 1f;

    public float AimAngleDegrees => aimAngle.Value;
    public Vector2 Facing => facing;

    // Physical tilt from a stickDeadzone-processed magnitude, which is rescaled from the deadzone min..max range to 0..1.
    private static float RawStickTilt(float processed)
    {
        return Mathf.Lerp(InputSystem.settings.defaultDeadzoneMin, InputSystem.settings.defaultDeadzoneMax, processed);
    }

    private static float AimAngleOf(Vector2 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private Vector2 GetMovementFacingDirection()
    {
        if (HasMoveInput)
            return moveInput.normalized;
        return facing;
    }

    // Cursor position on the world plane (virtual cursor when the mouse sensitivity is set); false without a camera or a mouse.
    private bool TryGetMouseWorld(out Vector2 world)
    {
        Vector3 mouseScreen;

        world = Vector2.zero;
        if (cam == null)
            cam = Camera.main;
        if (cam == null || Mouse.current == null)
            return false;
        mouseScreen = CursorManager.PointerPosition;
        mouseScreen.z = -cam.transform.position.z;
        world = cam.ScreenToWorldPoint(mouseScreen);
        return true;
    }

    // Mouse-to-world aim, evaluated every frame.
    private Vector2 GetMouseAimDirection()
    {
        Vector2 mouseWorld;
        Vector2 dir;

        if (!TryGetMouseWorld(out mouseWorld))
            return facing;
        dir = mouseWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude <= MinMoveInputSqr)
            return facing;
        return dir.normalized;
    }

    // Right stick after the stickDeadzone processor (already zero inside the deadzone, rescaled to 0-1 past it), used to move free-aimed ability zones.
    private Vector2 AimStickVector()
    {
        if (aimAction == null && playerInput != null)
            aimAction = playerInput.actions.FindAction("Aim");
        if (aimAction == null)
            return Vector2.zero;
        return aimAction.ReadValue<Vector2>();
    }

    // Aim action (right stick, stick deadzone then aim deadzone from GameSettings); falls back to the move direction, then keeps the last aim, once released.
    private Vector2 GetStickAimDirection()
    {
        Vector2 stick = AimStickVector();

        if (stick.sqrMagnitude > MinMoveInputSqr)
        {
            if (RawStickTilt(stick.magnitude) >= GameSettings.AimDeadzone)
                stickAim = stick.normalized;
            return stickAim;
        }
        if (HasMoveInput)
            stickAim = moveInput.normalized;
        return stickAim;
    }

    private Vector2 GetGamepadAimDirection()
    {
        Vector2 zoneAim;

        if (TryGetZoneAimDirection(out zoneAim))
            return zoneAim;
        return GetStickAimDirection();
    }

    // A pig faces where it walks; otherwise the gamepad stick (or placed zone) or the mouse.
    private Vector2 ComputeAimDirection()
    {
        if (IsDead)
            return GetMovementFacingDirection();
        if (InputModeTracker.UsingGamepad)
            return GetGamepadAimDirection();
        return GetMouseAimDirection();
    }

    private void ApplyFacing(Vector2 next)
    {
        facing = next;
        facingApplied = true;
        animator.SetFloat(LastInputXHash, facing.x);
        animator.SetFloat(LastInputYHash, facing.y);
    }

    // Snaps the aim to one of 8 directions, with an angular deadzone so adjacent directions don't flicker.
    private void UpdateFacing(Vector2 aim)
    {
        float angle;
        int octant;

        if (aim.sqrMagnitude < MinMoveInputSqr)
            return;
        angle = AimAngleOf(aim);
        if (facingApplied && Mathf.Abs(Mathf.DeltaAngle(angle, AimAngleOf(facing))) < FacingSwitchDeadzoneDeg)
            return;
        octant = ((Mathf.RoundToInt(angle / OctantDegrees) % 8) + 8) % 8;
        if (facingApplied && FacingOctants[octant] == facing)
            return;
        ApplyFacing(FacingOctants[octant]);
    }

    // Reverses the walk cycle while moving away from the facing.
    private void UpdateMoveSpeedMult()
    {
        bool backward = HasMoveInput && Vector2.Dot(moveInput.normalized, facing) < BackwardDotThreshold;
        float next = 1f;

        if (backward)
            next = -1f;
        if (Mathf.Approximately(next, moveSpeedMult))
            return;
        moveSpeedMult = next;
        animator.SetFloat(MoveSpeedMultHash, moveSpeedMult);
    }

    // Facing stays locked during an ability's move-lock.
    private void UpdateAimAndFacing()
    {
        Vector2 aimDir = ComputeAimDirection();

        aimAngle.Value = AimAngleOf(aimDir);
        if (Time.time >= moveLockEndTime)
            UpdateFacing(aimDir);
        UpdateMoveSpeedMult();
    }

    // Takes over another instance's look (offline player swapped for the networked one), so the swap is invisible; owner only.
    public void AdoptPose(Vector2 newFacing, float newAimAngle)
    {
        if (!this.IsLocallyControlled())
            return;
        facingApplied = false;
        UpdateFacing(newFacing);
        aimAngle.Value = newAimAngle;
    }
}
