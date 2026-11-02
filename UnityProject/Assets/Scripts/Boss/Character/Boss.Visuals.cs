using Unity.Netcode;
using UnityEngine;

// Replicated facing, phase telegraphs and held animation states, with Walk/Idle in between.
public partial class Boss
{
    private const float FacingDeadZone = 0.1f;
    private const float MinWalkSpeed = 0.3f;
    private const float WalkHoldTime = 0.15f;

    private Vector2 lastPosition;
    private float lastMovedTime;
    private float heldStateEndTime;
    private int currentStateHash;

    private readonly NetworkVariable<bool> facingLeft = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool offlineFacingLeft;

    private bool FacingLeft
    {
        get
        {
            if (IsSpawned)
                return facingLeft.Value;
            return offlineFacingLeft;
        }
    }

    // Server/offline only; ignores tiny horizontal offsets so the boss doesn't flicker when a target is straight above or below.
    private void SetFacing(float directionX)
    {
        bool left = directionX < 0f;

        if (Mathf.Abs(directionX) < FacingDeadZone)
            return;
        if (!IsSpawned)
            offlineFacingLeft = left;
        else if (facingLeft.Value != left)
            facingLeft.Value = left;
    }

    // Playback speed that makes the clip play exactly once over duration.
    private float FitSpeed(string clipName, float duration)
    {
        if (animator == null || animator.runtimeAnimatorController == null || duration <= 0f)
            return 1f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length / duration;
        }
        return 1f;
    }

    private void PlayState(int hash, float speed)
    {
        currentStateHash = hash;
        animator.speed = speed;
        animator.Play(hash, 0, 0f);
    }

    private void HoldState(string state, float duration, float speed)
    {
        if (animator == null)
            return;
        heldStateEndTime = Time.time + duration;
        PlayState(Animator.StringToHash(state), speed);
    }

    private void HoldFittedState(string state, float duration)
    {
        HoldState(state, duration, FitSpeed(state, duration));
    }

    private void ShowWindupTelegraph(BossPattern p, Vector2 direction, float duration, float laneLength)
    {
        if (p == BossPattern.Charge)
            AttackTelegraph.ShowLane(transform, BodyCenter, direction, laneLength, ChargeHitWidth, duration);
        else if (p == BossPattern.Slam)
            AttackTelegraph.Show(transform, BodyCenter, Vector2.right, 360f, kit.SlamRadius, duration);
    }

    // Telegraphs and animations only; positions come from this peer's own transform.
    private void PlayPhaseVisual(BossPattern p, BossPhase ph, Vector2 direction, float duration, float laneLength)
    {
        if (ph == BossPhase.Windup)
        {
            HoldFittedState(kit.WindupState, duration);
            ShowWindupTelegraph(p, direction, duration, laneLength);
        }
        else if (ph == BossPhase.Active && p == BossPattern.Charge)
            HoldState(kit.WalkState, duration, kit.ChargeAnimationSpeed);
        else if (ph == BossPhase.Active)
            HoldFittedState(kit.AttackState, duration);
        else
            HoldFittedState(kit.RecoveryState, duration);
    }

    private void UpdateFacing()
    {
        if (spriteRenderer != null && spriteRenderer.flipX != FacingLeft)
            spriteRenderer.flipX = FacingLeft;
    }

    private string ChooseFreeState(Vector2 delta)
    {
        float minStep = MinWalkSpeed * Time.deltaTime;

        if (delta.sqrMagnitude > minStep * minStep)
            lastMovedTime = Time.time;
        if (Time.time - lastMovedTime < WalkHoldTime)
            return kit.WalkState;
        return kit.IdleState;
    }

    // Applies the replicated facing and falls back to Walk/Idle once no pattern state is held.
    private void UpdateVisuals()
    {
        Vector2 position = transform.position;
        Vector2 delta = position - lastPosition;
        int desired;

        UpdateFacing();
        lastPosition = position;
        if (animator == null || Time.time < heldStateEndTime)
            return;
        desired = Animator.StringToHash(ChooseFreeState(delta));
        if (desired != currentStateHash)
            PlayState(desired, 1f);
    }
}
