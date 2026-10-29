using Unity.Netcode;
using UnityEngine;

// Attack windup, hit resolution and its telegraph or projectile, replayed on every peer.
public partial class Enemy
{
    private Player swingTarget;
    private float pendingHitTime;
    private float swingAngleDegrees;

    private bool IsWindingUp => pendingHitTime > 0f;

    private Vector2 SwingDirection
    {
        get
        {
            float radians = swingAngleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }
    }

    // Melee damage lands AttackHitDelay later; ranged shots hit on contact, the delay only holds the shooter still.
    private void StartAttack(Player attackTarget)
    {
        swingTarget = attackTarget;
        pendingHitTime = Time.time + Kit.AttackHitDelay;

        Vector2 direction = (attackTarget.Center - BodyCenter).normalized;
        swingAngleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (IsSpawned)
            PlaySwingRpc(swingAngleDegrees);
        PlayAttack();
    }

    private void TickAttack()
    {
        if (IsWindingUp && Time.time >= pendingHitTime)
            ResolveHit();
    }

    // The angle travels with the trigger, so remote telegraphs never reuse the previous swing's direction.
    [Rpc(SendTo.NotServer)]
    private void PlaySwingRpc(float angleDegrees)
    {
        swingAngleDegrees = angleDegrees;
        PlayAttack();
    }

    // Shields resolve on the server; a remote victim then judges the hit itself, since only it knows where it really stands.
    private void ResolveHit()
    {
        pendingHitTime = 0f;
        if (Kit.IsRanged || swingTarget == null || swingTarget.IsDead)
            return;

        Vector2 center = BodyCenter;
        Vector2 direction = SwingDirection;
        bool inZone = Kit.HitZoneContains(center, direction, swingTarget.Center);
        if (inZone && Player.TryAbsorbWithShield(center, swingTarget.Center, Kit.Damage))
            return;

        if (IsSpawned && swingTarget.OwnerClientId != NetworkManager.LocalClientId)
            ResolveHitOnVictimRpc(center, direction, swingTarget.NetworkObject, RpcTarget.Single(swingTarget.OwnerClientId, RpcTargetUse.Temp));
        else if (inZone)
            swingTarget.RequestDamage(Kit.Damage);
    }

    // Runs on the victim's own machine, against its true (not server-delayed) position.
    [Rpc(SendTo.SpecifiedInParams)]
    private void ResolveHitOnVictimRpc(Vector2 center, Vector2 direction, NetworkObjectReference targetRef, RpcParams rpcParams)
    {
        if (!targetRef.TryGet(out NetworkObject targetObject) || !targetObject.TryGetComponent(out Player victim) || victim.IsDead)
            return;
        if (Kit.HitZoneContains(center, direction, victim.Center))
            victim.TakeDamage(Kit.Damage);
    }

    private void PlayAttack()
    {
        PlayAttackAnimation(Kit.AttackAnimationDuration);
        if (Kit.IsRanged)
            EnemyProjectile.Fire(Kit.ProjectilePrefab, BodyCenter, SwingDirection, Kit.ProjectileSpeed, Kit.ProjectileMaxDistance, Kit.ProjectileArcHeight, Kit.Damage, this.HasServerAuthority());
        else
            Kit.ShowTelegraph(transform, BodyCenter, SwingDirection, Kit.AttackHitDelay);
    }
}
