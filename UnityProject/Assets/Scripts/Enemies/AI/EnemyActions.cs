using Unity.Netcode;
using UnityEngine;

// Attack windup/hit-resolution/telegraph; EnemyAI decides when to attack and calls StartAttack.
public class EnemyActions : NetworkBehaviour
{
    private EnemyKit kit;

    private EnemyAI ai;
    private Player swingTarget;
    private float pendingHitTime;

    public bool IsWindingUp => pendingHitTime > 0f;

    // Assigned at spawn time by whoever instantiates this enemy (see Enemy.Initialize); never baked into the prefab.
    public EnemyKit Kit { get => kit; set => kit = value; }

    private float swingAngleDegrees;

    void Awake()
    {
        ai = GetComponent<EnemyAI>();
    }

    void FixedUpdate()
    {
        if (!this.HasServerAuthority())
            return;

        if (pendingHitTime > 0f && Time.time >= pendingHitTime)
            ResolveHit();
    }

    // One RPC carries the angle with the trigger, so remote telegraphs and shots never use the previous swing's direction.
    [Rpc(SendTo.NotServer)]
    private void PlaySwingRpc(float angleDegrees)
    {
        swingAngleDegrees = angleDegrees;
        PlayAttackAnimation();
    }

    // Melee damage lands attackHitDelay later; ranged shots deal damage on contact (see RangedAttackVisual), the delay only holds the shooter still while it fires.
    public void StartAttack(Player target)
    {
        swingTarget = target;
        pendingHitTime = Time.time + kit.AttackHitDelay;

        Vector2 direction = (EnemyAI.TargetCenter(target) - ai.BodyCenter).normalized;
        swingAngleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (IsSpawned)
            PlaySwingRpc(swingAngleDegrees);
        PlayAttackAnimation(direction);
    }

    // Shields resolve on the server; a remote victim then judges the hit itself, since only it knows where it really stands.
    private void ResolveHit()
    {
        pendingHitTime = 0f;
        if (kit.IsRanged || swingTarget == null || swingTarget.IsDead)
            return;

        Vector2 center = ai.BodyCenter;
        Vector2 direction = SwingDirection();
        bool inZoneHere = kit.HitZoneContains(center, direction, EnemyAI.TargetCenter(swingTarget));

        if (inZoneHere && Player.TryAbsorbWithShield(center, EnemyAI.TargetCenter(swingTarget), kit.Damage))
            return;

        if (IsSpawned && swingTarget.OwnerClientId != NetworkManager.LocalClientId)
        {
            ResolveHitOnVictimRpc(center, direction, swingTarget.NetworkObject, RpcTarget.Single(swingTarget.OwnerClientId, RpcTargetUse.Temp));
            return;
        }

        if (inZoneHere)
            swingTarget.RequestDamage(kit.Damage);
    }

    // Runs on the victim's own machine, against its true (not server-delayed) position.
    [Rpc(SendTo.SpecifiedInParams)]
    private void ResolveHitOnVictimRpc(Vector2 center, Vector2 direction, NetworkObjectReference targetRef, RpcParams rpcParams)
    {
        if (!targetRef.TryGet(out NetworkObject targetObject) || !targetObject.TryGetComponent(out Player target) || target.IsDead)
            return;

        if (kit.HitZoneContains(center, direction, EnemyAI.TargetCenter(target)))
            target.TakeDamage(kit.Damage);
    }

    private Vector2 SwingDirection()
    {
        float radians = swingAngleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    // Remote clients reach this via HandleSwingCountChanged, with no local windup state of their own to use.
    private void PlayAttackAnimation() => PlayAttackAnimation(SwingDirection());

    private void PlayAttackAnimation(Vector2 direction)
    {
        ai.PlayAttackVisual(kit.AttackAnimationDuration);

        if (kit.IsRanged)
        {
            RangedAttackVisual.Show(kit.ProjectilePrefab, ai.BodyCenter, direction, kit.ProjectileSpeed, kit.ProjectileMaxDistance, kit.ProjectileArcHeight, kit.Damage, this.HasServerAuthority());
            return;
        }

        kit.ShowTelegraph(transform, ai.BodyCenter, direction, kit.AttackHitDelay);
    }
}
