using Unity.Netcode;
using UnityEngine;

// Replicated facing, phase telegraphs and held animation states, with Walk/Idle in between.
public partial class Boss
{
    private Vector2 lastPosition;
    private float lastMovedTime;
    private float heldStateEndTime;
    private int currentStateHash;

    private readonly NetworkVariable<bool> facingLeft = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool offlineFacingLeft;

    private bool FacingLeft => IsSpawned ? facingLeft.Value : offlineFacingLeft;

    // Server/offline only; ignores tiny horizontal offsets so the boss doesn't flicker when a target is straight above or below.
    private void SetFacing(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.1f)
            return;

        bool left = directionX < 0f;
        if (!IsSpawned)
            offlineFacingLeft = left;
        else if (facingLeft.Value != left)
            facingLeft.Value = left;
    }

    // Telegraphs and animations only; positions come from this peer's own transform.
    private void PlayPhaseVisual(BossPattern p, BossPhase ph, Vector2 direction, float duration, float laneLength)
    {
        if (ph == BossPhase.Windup)
        {
            HoldState(kit.WindupState, duration, FitSpeed(kit.WindupState, duration));
            if (p == BossPattern.Charge)
                AttackTelegraph.ShowLane(transform, BodyCenter, direction, laneLength, ChargeHitWidth, duration);
            else if (p == BossPattern.Slam)
                AttackTelegraph.Show(transform, BodyCenter, Vector2.right, 360f, kit.SlamRadius, duration);
        }
        else if (ph == BossPhase.Active && p == BossPattern.Charge)
        {
            HoldState(kit.WalkState, duration, kit.ChargeAnimationSpeed);
        }
        else
        {
            string state = ph == BossPhase.Active ? kit.AttackState : kit.RecoveryState;
            HoldState(state, duration, FitSpeed(state, duration));
        }
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

    private void HoldState(string state, float duration, float speed)
    {
        if (animator == null)
            return;

        heldStateEndTime = Time.time + duration;
        currentStateHash = Animator.StringToHash(state);
        animator.speed = speed;
        animator.Play(currentStateHash, 0, 0f);
    }

    // Applies the replicated facing and falls back to Walk/Idle once no pattern state is held.
    private void UpdateVisuals()
    {
        if (spriteRenderer != null && spriteRenderer.flipX != FacingLeft)
            spriteRenderer.flipX = FacingLeft;

        Vector2 position = transform.position;
        Vector2 delta = position - lastPosition;
        lastPosition = position;

        if (animator == null || Time.time < heldStateEndTime)
            return;

        float minStep = 0.3f * Time.deltaTime;
        if (delta.sqrMagnitude > minStep * minStep)
            lastMovedTime = Time.time;

        int desired = Animator.StringToHash(Time.time - lastMovedTime < 0.15f ? kit.WalkState : kit.IdleState);
        if (desired == currentStateHash)
            return;

        currentStateHash = desired;
        animator.speed = 1f;
        animator.Play(desired, 0, 0f);
    }
}
