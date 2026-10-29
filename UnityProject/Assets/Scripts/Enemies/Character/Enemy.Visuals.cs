using UnityEngine;

// Sprite facing and Idle/Run/Attack animation, derived from real movement on every peer.
public partial class Enemy
{
    private const float FlipDistance = 0.06f;
    private const float FlipCooldown = 0.2f;

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

    private void PlayAttackAnimation(float duration)
    {
        if (animator == null)
            return;

        attackVisualEndTime = Time.time + duration;
        currentAnimationHash = attackHash;
        animator.Play(attackHash, 0, 0f);
    }

    // Facing uses hysteresis (drift saturates at FlipDistance) so interpolation and crowd jitter don't flip the sprite every frame.
    private void UpdateVisuals()
    {
        Vector2 position = transform.position;
        Vector2 delta = position - lastPosition;
        lastPosition = position;

        facingDrift = delta.sqrMagnitude > 1f ? 0f : Mathf.Clamp(facingDrift + delta.x, -FlipDistance, FlipDistance);
        bool faceLeft = facingDrift < 0f;
        if (spriteRenderer != null && Mathf.Abs(facingDrift) >= FlipDistance && Time.time - lastFlipTime >= FlipCooldown && spriteRenderer.flipX != faceLeft)
        {
            spriteRenderer.flipX = faceLeft;
            lastFlipTime = Time.time;
        }

        if (animator == null)
            return;

        float minStep = 0.3f * Time.deltaTime;
        if (delta.sqrMagnitude > minStep * minStep)
            lastMovedTime = Time.time;

        int desired = Time.time < attackVisualEndTime ? attackHash
            : Time.time - lastMovedTime < 0.15f ? runHash : idleHash;
        if (desired == currentAnimationHash)
            return;

        currentAnimationHash = desired;
        animator.Play(desired, 0, 0f);
    }
}
