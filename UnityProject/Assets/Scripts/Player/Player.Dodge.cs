using UnityEngine;
using UnityEngine.InputSystem;

public partial class Player
{
    private int dodgeCharges = -1;
    private float dodgeRechargeElapsed;
    private float dodgeEndTime;
    private Vector2 dodgeVelocity;
    private Vector2 lastMoveDirection = Vector2.down;
    private InputAction dodgeAction;

    public int MaxDodgeCharges => activeKit != null ? Mathf.Max(0, activeKit.DodgeCharges) : 0;
    public int DodgeCharges => Mathf.Clamp(dodgeCharges < 0 ? MaxDodgeCharges : dodgeCharges, 0, MaxDodgeCharges);
    public float DodgeRecharge01 => activeKit != null && DodgeCharges < MaxDodgeCharges && activeKit.DodgeRechargeSeconds > 0f
        ? Mathf.Clamp01(dodgeRechargeElapsed / activeKit.DodgeRechargeSeconds) : 0f;
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
    private void TryDodge()
    {
        if (!this.IsLocallyControlled() || IsBlocked || IsDead || isMounted || IsDodging || activeKit == null)
            return;
        if (DodgeCharges <= 0 || activeKit.DodgeDuration <= 0f)
            return;

        Vector2 direction = moveInput.sqrMagnitude > 0.0001f ? moveInput.normalized : lastMoveDirection;
        dodgeCharges = DodgeCharges - 1;
        dodgeEndTime = Time.time + activeKit.DodgeDuration;
        dodgeVelocity = direction * (activeKit.DodgeDistance / activeKit.DodgeDuration);
    }

    // Owner only: remembers the last keyboard direction and refills one charge at a time, every DodgeRechargeSeconds.
    private void UpdateDodge()
    {
        if (!this.IsLocallyControlled() || activeKit == null)
            return;

        if (moveInput.sqrMagnitude > 0.0001f)
            lastMoveDirection = moveInput.normalized;

        int max = MaxDodgeCharges;
        int current = DodgeCharges;
        if (current >= max)
        {
            dodgeCharges = max;
            dodgeRechargeElapsed = 0f;
            return;
        }

        dodgeRechargeElapsed += Time.deltaTime;
        if (dodgeRechargeElapsed >= activeKit.DodgeRechargeSeconds)
        {
            dodgeCharges = current + 1;
            dodgeRechargeElapsed = 0f;
        }
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
