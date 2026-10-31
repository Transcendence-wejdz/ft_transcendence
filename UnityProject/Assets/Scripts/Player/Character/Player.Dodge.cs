using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Dodge: a short invulnerable dash on charges that refill over time, with dash dust on every peer.
public partial class Player
{
    private const float RemoteDashTailSeconds = 0.15f;
    private const float MinMoveInputSqr = 0.0001f;

    [Header("Dodge")]
    [SerializeField] private ParticleSystem dashDust;

    private float dashDustEndTime;
    private float remoteDashStartTime;
    private float remoteDashEndTime;
    private int dodgeCharges = -1;
    private float dodgeRechargeElapsed;
    private float dodgeEndTime;
    private Vector2 dodgeVelocity;
    private Vector2 lastMoveDirection = Vector2.down;
    private InputAction dodgeAction;

    public int MaxDodgeCharges
    {
        get
        {
            if (activeKit == null)
                return 0;
            return Mathf.Max(0, activeKit.DodgeCharges);
        }
    }

    // Unset (-1) until the first dodge, which reads as a full stock.
    public int DodgeCharges
    {
        get
        {
            if (dodgeCharges < 0)
                return MaxDodgeCharges;
            return Mathf.Clamp(dodgeCharges, 0, MaxDodgeCharges);
        }
    }

    public float DodgeRecharge01
    {
        get
        {
            if (activeKit == null || DodgeCharges >= MaxDodgeCharges || activeKit.DodgeRechargeSeconds <= 0f)
                return 0f;
            return Mathf.Clamp01(dodgeRechargeElapsed / activeKit.DodgeRechargeSeconds);
        }
    }
    public bool IsDodging => Time.time < dodgeEndTime;

    // Hooked up wherever this player's PlayerInput is turned on, so only the local (or offline) player listens.
    private void BindDodgeInput()
    {
        if (dodgeAction != null || playerInput == null)
            return;
        dodgeAction = playerInput.actions.FindAction("Dodge");
        if (dodgeAction != null)
            dodgeAction.performed += OnDodgePerformed;
    }

    private void UnbindDodgeInput()
    {
        if (dodgeAction != null)
            dodgeAction.performed -= OnDodgePerformed;
        dodgeAction = null;
    }

    private void OnDodgePerformed(InputAction.CallbackContext ctx)
    {
        TryDodge();
    }

    // Always keyboard-driven: the held direction, else the last one moved in (never the mouse); the owner moves itself, NetworkTransform replicates it.
    private bool CanDodge()
    {
        if (!this.IsLocallyControlled() || IsBlocked || IsDead || isMounted || IsDodging || activeKit == null)
            return false;
        return DodgeCharges > 0 && activeKit.DodgeDuration > 0f;
    }

    private Vector2 DodgeDirection()
    {
        if (moveInput.sqrMagnitude > MinMoveInputSqr)
            return moveInput.normalized;
        return lastMoveDirection;
    }

    private void TryDodge()
    {
        Vector2 direction;

        if (!CanDodge())
            return;
        direction = DodgeDirection();
        dodgeCharges = DodgeCharges - 1;
        dodgeEndTime = Time.time + activeKit.DodgeDuration;
        dodgeVelocity = direction * (activeKit.DodgeDistance / activeKit.DodgeDuration);
        dashDustEndTime = dodgeEndTime;
        if (NetworkObject.IsSpawned)
            DashDustServerRpc(activeKit.DodgeDuration);
    }

    [ServerRpc]
    private void DashDustServerRpc(float duration)
    {
        DashDustClientRpc(duration);
    }

    // Remote copies display the dodge late, so the window is shifted by this peer's own interpolation delay.
    [ClientRpc]
    private void DashDustClientRpc(float duration)
    {
        if (IsOwner)
            return;
        remoteDashStartTime = Time.time + Unity.Netcode.Components.NetworkTransform.GetTickLatencyInSeconds();
        remoteDashEndTime = remoteDashStartTime + duration + RemoteDashTailSeconds;
    }

    // Owner emits during its dodge window, remote copies during the shifted one; emission is per distance, so it follows the displayed dash.
    private bool IsDashDustActive()
    {
        if (IsOwner || !NetworkObject.IsSpawned)
            return Time.time < dashDustEndTime;
        return Time.time >= remoteDashStartTime && Time.time < remoteDashEndTime;
    }

    private void UpdateDashDust()
    {
        bool active;
        ParticleSystem.EmissionModule emission;

        if (dashDust == null)
            return;
        active = IsDashDustActive();
        emission = dashDust.emission;
        if (emission.enabled != active)
            emission.enabled = active;
    }

    // Owner only: remembers the last keyboard direction and refills one charge at a time, every DodgeRechargeSeconds.
    private void RechargeDodge()
    {
        int max = MaxDodgeCharges;
        int current = DodgeCharges;

        if (current >= max)
        {
            dodgeCharges = max;
            dodgeRechargeElapsed = 0f;
            return;
        }
        dodgeRechargeElapsed += Time.deltaTime;
        if (dodgeRechargeElapsed < activeKit.DodgeRechargeSeconds)
            return;
        dodgeCharges = current + 1;
        dodgeRechargeElapsed = 0f;
    }

    private void UpdateDodge()
    {
        if (!this.IsLocallyControlled() || activeKit == null)
            return;
        if (moveInput.sqrMagnitude > MinMoveInputSqr)
            lastMoveDirection = moveInput.normalized;
        RechargeDodge();
    }

    // True while the dodge owns this frame's movement.
    private bool ApplyDodgeMovement()
    {
        if (!IsDodging)
            return false;
        rb.linearVelocity = dodgeVelocity;
        return true;
    }
}
