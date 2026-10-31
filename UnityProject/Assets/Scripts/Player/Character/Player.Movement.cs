using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Owner-side walking, input setup, soft player collision and pushes; aim and facing live in Player.Facing.cs.
public partial class Player
{
    private const float SoftCollisionShare = 0.5f;
    private const float MaxPushSpeed = 8f;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private PauseManager pauseManager;

    private Vector2 moveInput;
    private bool wasBlocked;
    private Vector2 pendingPush;

    private bool HasMoveInput => moveInput.sqrMagnitude > MinMoveInputSqr;

    private void EnableOwnInput()
    {
        KeybindOverrides.Apply(playerInput.actions);
        playerInput.enabled = true;
        BindDodgeInput();
    }

    // PlayerInput ships disabled so a remote player's copy never grabs devices; the local (or offline) player turns it on here.
    private void StartMovement()
    {
        if (playerInput == null)
            return;
        KeybindOverrides.Apply(playerInput.actions);
        if (!this.IsLocallyControlled())
            return;
        playerInput.enabled = true;
        BindDodgeInput();
    }

    // Other copies go kinematic so no peer's physics can shove them off the owner's position.
    private void SetupRemoteCopy()
    {
        if (playerInput != null)
            playerInput.enabled = false;
        if (rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;
        PlayerCameraRig.SetActive(transform, active: false);
    }

    // Moves the local player onto its spawner glyph (one per client slot) after a scene load.
    private void HandleSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Vector3 position;

        if (NetworkManager == null || clientId != NetworkManager.LocalClientId)
            return;
        if (!PlayerSpawner.TryGetSpawnPosition(OwnerClientId, out position))
            return;
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
        transform.position = position;
    }

    // Only the owner reads input and renders a camera.
    private void SpawnMovement()
    {
        if (!IsOwner)
        {
            SetupRemoteCopy();
            return;
        }
        if (playerInput != null)
            EnableOwnInput();
        PlayerCameraRig.ClaimAudioListener(transform);
        if (NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadComplete += HandleSceneLoadComplete;
    }

    private void DespawnMovement()
    {
        if (IsOwner && NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadComplete -= HandleSceneLoadComplete;
    }

    private Vector2 ConsumePushVelocity()
    {
        Vector2 push = pendingPush;

        pendingPush = Vector2.zero;
        if (push == Vector2.zero || Time.deltaTime <= 0f)
            return Vector2.zero;
        return Vector2.ClampMagnitude(push / Time.deltaTime, MaxPushSpeed);
    }

    private void StopWalking()
    {
        rb.linearVelocity = Vector2.zero;
        animator.SetBool(IsWalkingHash, false);
    }

    // Move only fires on change, so the held direction is read again once input is unblocked.
    private void ResampleMoveInput()
    {
        InputAction moveAction = null;

        if (playerInput != null)
            moveAction = playerInput.actions.FindAction("Move");
        if (moveAction != null)
            moveInput = moveAction.ReadValue<Vector2>();
    }

    // Rooted (speed 0) still lets pushes through.
    private void ApplyWalkVelocity(Vector2 pushVelocity)
    {
        float speedMult = MovementSpeedMultiplier;

        if (speedMult <= 0f)
            rb.linearVelocity = pushVelocity;
        else
            rb.linearVelocity = moveInput * moveSpeed * speedMult * MoveSpeedBonusMultiplier + pushVelocity;
    }

    private void UpdateMovement()
    {
        Vector2 pushVelocity;

        FlipTowards(animator.GetFloat(LastInputXHash));
        if (!this.IsLocallyControlled())
            return;
        pushVelocity = ConsumePushVelocity();
        if (IsBlocked)
        {
            StopWalking();
            wasBlocked = true;
            return;
        }
        if (wasBlocked)
        {
            wasBlocked = false;
            ResampleMoveInput();
        }
        if (ApplyDodgeMovement())
            return;
        UpdateAimAndFacing();
        if (isMounted)
            StopWalking();
        else
            ApplyWalkVelocity(pushVelocity);
    }

    public void Move(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;
        moveInput = ctx.ReadValue<Vector2>();
        if (!IsBlocked)
            animator.SetBool(IsWalkingHash, HasMoveInput);
    }

    public void OnPause(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;
        if (pauseManager == null)
            pauseManager = FindAnyObjectByType<PauseManager>();
        pauseManager?.Pause(ctx);
    }

}
