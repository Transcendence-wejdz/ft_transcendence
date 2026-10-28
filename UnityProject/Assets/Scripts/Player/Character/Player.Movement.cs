using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public partial class Player
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private Camera cam;

    private readonly NetworkVariable<float> aimAngle = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float AimAngleDegrees => aimAngle.Value;
    public Vector2 Facing => facing;

    private Vector2 moveInput;
    private Vector2 facing = Vector2.down;
    private Vector2 stickAim = Vector2.down;
    private InputAction aimAction;
    private bool facingApplied;
    private float moveSpeedMult = 1f;

    private PauseManager pauseManager;

    private const float FacingSwitchDeadzoneDeg = 10f;
    private const float BackwardDotThreshold = -0.1f;
    private const float SoftCollisionShare = 0.5f;
    private const float MaxPushSpeed = 8f;

    private Vector2 pendingPush;

    private static readonly Vector2[] FacingOctants =
    {
        new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(-1f, 1f),
        new Vector2(-1f, 0f), new Vector2(-1f, -1f), new Vector2(0f, -1f), new Vector2(1f, -1f),
    };

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int MoveSpeedMultHash = Animator.StringToHash("MoveSpeedMult");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");
    private static readonly int LastInputYHash = Animator.StringToHash("LastInputY");

    // PlayerInput ships disabled so a remote player's copy never grabs devices; the local (or offline) player turns it on here.
    private void StartMovement()
    {
        if (playerInput == null)
            return;

        KeybindOverrides.Apply(playerInput.actions);
        if (this.IsLocallyControlled())
        {
            playerInput.enabled = true;
            BindDodgeInput();
        }
    }

    // Only the owner reads input and renders a camera; other copies go kinematic so no peer's physics can shove them off the owner's position.
    private void SpawnMovement()
    {
        if (!IsOwner)
        {
            if (playerInput != null)
                playerInput.enabled = false;
            if (rb != null)
                rb.bodyType = RigidbodyType2D.Kinematic;

            PlayerCameraRig.SetActive(transform, active: false);
            return;
        }

        if (playerInput != null)
        {
            KeybindOverrides.Apply(playerInput.actions);
            playerInput.enabled = true;
            BindDodgeInput();
        }
        PlayerCameraRig.ClaimAudioListener(transform);

        if (NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadComplete += HandleSceneLoadComplete;
    }

    private void DespawnMovement()
    {
        if (IsOwner && NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadComplete -= HandleSceneLoadComplete;
    }

    // Moves the local player onto its spawner glyph (one per client slot) after a scene load.
    private void HandleSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (NetworkManager == null || clientId != NetworkManager.LocalClientId)
            return;

        if (!PlayerSpawner.TryGetSpawnPosition(OwnerClientId, out Vector3 position))
            return;

        rb.position = position;
        rb.linearVelocity = Vector2.zero;
        transform.position = position;
    }

    private bool wasBlocked;

    // Re-samples Move input after unblocking (it only fires on change); facing stays locked during an ability's move-lock.
    private void UpdateMovement()
    {
        FlipTowards(animator.GetFloat(LastInputXHash));

        if (!this.IsLocallyControlled())
            return;

        Vector2 pushVelocity = ConsumePushVelocity();

        if (IsBlocked)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool(IsWalkingHash, false);
            wasBlocked = true;
            return;
        }

        if (wasBlocked)
        {
            wasBlocked = false;
            InputAction moveAction = playerInput != null ? playerInput.actions.FindAction("Move") : null;
            if (moveAction != null)
                moveInput = moveAction.ReadValue<Vector2>();
        }

        if (ApplyKnockbackMovement() || ApplyDodgeMovement())
            return;

        Vector2 aimDir = IsDead ? GetMovementFacingDirection() : InputModeTracker.UsingGamepad ? GetStickAimDirection() : GetMouseAimDirection();
        aimAngle.Value = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

        if (Time.time >= moveLockEndTime)
            UpdateFacing(aimDir);

        UpdateMoveSpeedMult();

        if (isMounted)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool(IsWalkingHash, false);
            return;
        }

        float speedMult = MovementSpeedMultiplier;
        if (speedMult <= 0f)
        {
            rb.linearVelocity = pushVelocity;
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed * speedMult * MoveSpeedBonusMultiplier + pushVelocity;
    }

    // Queues a displacement (soft collision, shield) that the next movement step turns into velocity, so walls still stop it.
    public void AddPush(Vector2 displacement)
    {
        pendingPush += displacement;
    }

    private Vector2 ConsumePushVelocity()
    {
        Vector2 push = pendingPush;
        pendingPush = Vector2.zero;
        if (push == Vector2.zero || Time.deltaTime <= 0f)
            return Vector2.zero;
        return Vector2.ClampMagnitude(push / Time.deltaTime, MaxPushSpeed);
    }

    // Players never block each other physically; the local player slides out of half of any overlap instead.
    private void UpdateSoftPlayerCollision()
    {
        if (!this.IsLocallyControlled() || bodyCollider == null || rb == null)
            return;

        Vector2 center = bodyCollider.bounds.center;
        float radius = Mathf.Min(bodyCollider.bounds.extents.x, bodyCollider.bounds.extents.y);
        Vector2 separation = Vector2.zero;

        foreach (Player other in AllActiveInstances)
        {
            if (other == this || other.bodyCollider == null)
                continue;

            Physics2D.IgnoreCollision(bodyCollider, other.bodyCollider, true);
            if (isMounted || other.isMounted || transform.parent == other.transform || other.transform.parent == transform)
                continue;

            Vector2 offset = center - (Vector2)other.bodyCollider.bounds.center;
            float minDistance = radius + Mathf.Min(other.bodyCollider.bounds.extents.x, other.bodyCollider.bounds.extents.y);
            float distance = offset.magnitude;
            if (distance >= minDistance)
                continue;

            Vector2 away = distance > 0.0001f ? offset / distance : Vector2.right;
            separation += away * ((minDistance - distance) * SoftCollisionShare);
        }

        if (separation != Vector2.zero)
            AddPush(separation);
    }

    public void Move(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;

        moveInput = ctx.ReadValue<Vector2>();
        bool hasDirection = moveInput.sqrMagnitude > 0.0001f;

        if (IsBlocked)
            return;

        animator.SetBool(IsWalkingHash, hasDirection);
    }

    public void OnPause(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;

        if (pauseManager == null)
            pauseManager = FindAnyObjectByType<PauseManager>();

        pauseManager?.Pause(ctx);
    }

    private Vector2 GetMovementFacingDirection()
    {
        return moveInput.sqrMagnitude > 0.0001f ? moveInput.normalized : facing;
    }

    // Mouse-to-world aim, evaluated every frame.
    private Vector2 GetMouseAimDirection()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null || Mouse.current == null)
            return facing;

        Vector3 mouseScreen = Mouse.current.position.ReadValue();
        mouseScreen.z = -cam.transform.position.z;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);

        Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : facing;
    }

    // Aim action (right stick, deadzone from GameSettings); falls back to the move direction, then keeps the last aim, once released.
    private Vector2 GetStickAimDirection()
    {
        if (aimAction == null && playerInput != null)
            aimAction = playerInput.actions.FindAction("Aim");

        Vector2 stick = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;
        if (stick.sqrMagnitude > 0.0001f)
            stickAim = stick.normalized;
        else if (moveInput.sqrMagnitude > 0.0001f)
            stickAim = moveInput.normalized;
        return stickAim;
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

    // Snaps the aim to one of 8 directions, with an angular deadzone so adjacent directions don't flicker.
    private void UpdateFacing(Vector2 aim)
    {
        if (aim.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        if (facingApplied)
        {
            float currentAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, currentAngle)) < FacingSwitchDeadzoneDeg)
                return;
        }

        int octant = ((Mathf.RoundToInt(angle / 45f) % 8) + 8) % 8;
        Vector2 next = FacingOctants[octant];

        if (facingApplied && next == facing)
            return;

        facing = next;
        facingApplied = true;
        animator.SetFloat(LastInputXHash, facing.x);
        animator.SetFloat(LastInputYHash, facing.y);
    }

    // Reverses the walk cycle while moving away from the facing.
    private void UpdateMoveSpeedMult()
    {
        bool hasDirection = moveInput.sqrMagnitude > 0.0001f;
        bool backward = hasDirection && Vector2.Dot(moveInput.normalized, facing) < BackwardDotThreshold;
        float next = backward ? -1f : 1f;

        if (Mathf.Approximately(next, moveSpeedMult))
            return;

        moveSpeedMult = next;
        animator.SetFloat(MoveSpeedMultHash, moveSpeedMult);
    }
}
