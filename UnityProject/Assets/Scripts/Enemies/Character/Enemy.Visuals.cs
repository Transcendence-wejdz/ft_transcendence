using UnityEngine;

// Sprite facing and Idle/Run/Attack animation, derived from real movement on every peer.
public partial class Enemy
{
    private const float FlipDistance = 0.06f;
    private const float FlipCooldown = 0.2f;
    private const float TeleportStepSqr = 1f;
    private const float MinRunSpeed = 0.3f;
    private const float RunHoldTime = 0.15f;

    private Vector2 lastPosition;
    private float facingDrift;
    private float lastFlipTime = -1f;
    private float lastMovedTime;

    private float attackVisualEndTime;
    private int currentAnimationHash;
    private int idleHash;
    private int runHash;
    private int attackHash;

    // Aseprite-generated controllers don't all share the same state names, so they come from the kit.
    private void CacheAnimationHashes()
    {
        idleHash = Animator.StringToHash(Kit.IdleStateName);
        runHash = Animator.StringToHash(Kit.RunStateName);
        attackHash = Animator.StringToHash(Kit.AttackStateName);
    }

    private void PlayAnimation(int hash)
    {
        currentAnimationHash = hash;
        animator.Play(hash, 0, 0f);
    }

    private void PlayAttackAnimation(float duration)
    {
        if (animator == null)
            return;
        attackVisualEndTime = Time.time + duration;
        PlayAnimation(attackHash);
    }

    // Hysteresis (drift saturates at FlipDistance) so interpolation and crowd jitter don't flip the sprite every frame.
    private void UpdateFacing(Vector2 delta)
    {
        bool faceLeft;

        if (delta.sqrMagnitude > TeleportStepSqr)
            facingDrift = 0f;
        else
            facingDrift = Mathf.Clamp(facingDrift + delta.x, -FlipDistance, FlipDistance);
        faceLeft = facingDrift < 0f;
        if (spriteRenderer == null || spriteRenderer.flipX == faceLeft)
            return;
        if (Mathf.Abs(facingDrift) < FlipDistance || Time.time - lastFlipTime < FlipCooldown)
            return;
        spriteRenderer.flipX = faceLeft;
        lastFlipTime = Time.time;
    }

    private int ChooseAnimation(Vector2 delta)
    {
        float minStep = MinRunSpeed * Time.deltaTime;

        if (delta.sqrMagnitude > minStep * minStep)
            lastMovedTime = Time.time;
        if (Time.time < attackVisualEndTime)
            return attackHash;
        if (Time.time - lastMovedTime < RunHoldTime)
            return runHash;
        return idleHash;
    }

    private void UpdateVisuals()
    {
        Vector2 position = transform.position;
        Vector2 delta = position - lastPosition;
        int desired;

        lastPosition = position;
        UpdateFacing(delta);
        if (animator == null)
            return;
        desired = ChooseAnimation(delta);
        if (desired != currentAnimationHash)
            PlayAnimation(desired);
    }
}
