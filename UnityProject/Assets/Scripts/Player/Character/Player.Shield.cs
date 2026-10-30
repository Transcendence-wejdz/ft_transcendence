using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Held shield Ultimate: a wall that pushes enemies and absorbs hits aimed behind it, until its HP or timer runs out.
public partial class Player
{
    [Header("Shield")]
    [Tooltip("Extra distance in front of a raised shield at which bodies start being pushed, so they move before the (ping-delayed) real wall reaches them.")]
    [SerializeField] private float shieldPushLead = 0.5f;

    private const float ShieldContactGap = 0.02f;
    private const float TrapCheckMargin = 0.05f;
    private static readonly RaycastHit2D[] TrapCastBuffer = new RaycastHit2D[8];

    private Collider2D shieldWall;
    private Player passThroughPlayer;
    private static int enemyLayerMask = -1;
    private static int EnemyLayerMask => enemyLayerMask >= 0 ? enemyLayerMask : (enemyLayerMask = LayerMask.GetMask("Enemy"));
    private static readonly Collider2D[] ShieldPushBuffer = new Collider2D[32];
    private static readonly Collider2D[] WallOverlapBuffer = new Collider2D[32];
    private static readonly RaycastHit2D[] ShieldLineBuffer = new RaycastHit2D[8];
    // Only classes whose controller has a "ShieldHeld" bool use it (see ClassKit.ShieldHoldAnimation).
    private static readonly int ShieldHeldHash = Animator.StringToHash("ShieldHeld");

    private readonly NetworkVariable<bool> isShieldActive = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<float> shieldHealth = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // Trails the live mouse aim at a capped turn rate instead of snapping to it, so re-aiming the raised shield feels heavy.
    private readonly NetworkVariable<float> shieldFacingAngle = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private float shieldEndTime;
    private float nextShieldPushTime;
    private float knockbackEndTime;
    private Vector2 knockbackVelocity;

    public bool IsShieldActive => isShieldActive.Value;
    public float CurrentShieldHealth => shieldHealth.Value;
    public float ShieldMaxHealth => activeKit != null ? activeKit.ShieldHealth : 0f;
    public float ShieldFacingAngle => shieldFacingAngle.Value;

    // Called from Ultimate() for shield kits: a press raises it, a second press lowers it early.
    private void UltimateShield(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled() || !ctx.performed)
            return;

        if (isShieldActive.Value)
        {
            EndShield();
            return;
        }

        if (!CanFight || Time.time < ultimateReadyTime)
            return;
        if (!TrySpendMana(activeKit.UltimateManaCost))
            return;

        StartShield();
    }

    private void StartShield()
    {
        isShieldActive.Value = true;
        shieldHealth.Value = activeKit.ShieldHealth;
        shieldFacingAngle.Value = AimAngleDegrees;
        shieldEndTime = Time.time + activeKit.ShieldHoldDuration;
        nextShieldPushTime = Time.time;

        animator.SetFloat(UltimateAnimSpeedHash, activeKit.UltimateAnimSpeed);
        animator.SetTrigger(UltimateHash);
        if (activeKit.ShieldHoldAnimation)
            animator.SetBool(ShieldHeldHash, true);
        OnAbilityUsed?.Invoke(AbilityType.Ultimate, activeKit.UltimateCooldown);
        SpawnAbilityFx(AbilityType.Ultimate);
        PushNearbyEnemies(onSpawn: true);
    }

    private void UpdateShield()
    {
        if (!this.IsLocallyControlled() || !isShieldActive.Value)
            return;

        if (!CanFight || Time.time >= shieldEndTime)
        {
            EndShield();
            return;
        }

        shieldFacingAngle.Value = Mathf.MoveTowardsAngle(shieldFacingAngle.Value, AimAngleDegrees, activeKit.ShieldTurnSpeed * Time.deltaTime);

        if (Time.time >= nextShieldPushTime)
        {
            nextShieldPushTime = Time.time + activeKit.ShieldPushInterval;
            PushNearbyEnemies(onSpawn: false);
        }
    }

    private void EndShield()
    {
        isShieldActive.Value = false;
        ultimateReadyTime = Time.time + activeKit.UltimateCooldown;
        if (activeKit.ShieldHoldAnimation)
            animator.SetBool(ShieldHeldHash, false);
    }

    // Raising the shield also clears players caught in the wall; the periodic push only moves enemies.
    private void PushNearbyEnemies(bool onSpawn)
    {
        if (NetworkObject.IsSpawned)
        {
            PushEnemiesServerRpc(transform.position, shieldFacingAngle.Value, onSpawn);
            if (onSpawn)
                PushPlayersRpc(transform.position, shieldFacingAngle.Value);
        }
        else
        {
            ApplyPush(transform.position, shieldFacingAngle.Value, onSpawn);
        }
    }

    // Each peer pushes only its own player (moved by its owner), forward out of the wall whichever side it overlaps.
    [Rpc(SendTo.NotMe)]
    private void PushPlayersRpc(Vector2 center, float facingDegrees)
    {
        Player local = LocalPlayer.GetPlayer();
        if (local == null || local == this || local.isMounted || local.bodyCollider == null)
            return;

        CapsuleCollider2D template = ShieldHitboxTemplate();
        if (template == null)
            return;

        Vector2 facing = DirFromAngle(facingDegrees);
        Vector2 shieldCenter = center + FxOffset(AbilityType.Ultimate, facing);
        Vector2 querySize = ShieldWallSize(template);

        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(1 << local.gameObject.layer);
        int count = Physics2D.OverlapCapsule(shieldCenter, querySize, template.direction, facingDegrees, filter, ShieldPushBuffer);
        for (int i = 0; i < count; i++)
        {
            if (ShieldPushBuffer[i] != local.bodyCollider)
                continue;

            local.ApplyKnockback(facing * activeKit.ShieldKnockbackForce, activeKit.ShieldKnockbackDuration);
            return;
        }
    }

    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        knockbackVelocity = velocity;
        knockbackEndTime = Time.time + duration;
    }

    // True while a knockback owns this frame's movement.
    private bool ApplyKnockbackMovement()
    {
        if (Time.time >= knockbackEndTime)
            return false;

        rb.linearVelocity = knockbackVelocity;
        return true;
    }

    [ServerRpc]
    private void PushEnemiesServerRpc(Vector2 center, float facingDegrees, bool onSpawn)
    {
        ApplyPush(center, facingDegrees, onSpawn);
    }

    // Extra size for the push query only, since the solid wall keeps enemies touching it, never overlapping.
    private const float PushQueryPadding = 0.8f;

    // Knocks back enemies near the shield's real wall shape on its facing side; on spawn, those caught inside the wall are pushed forward too.
    private void ApplyPush(Vector2 center, float facingDegrees, bool onSpawn)
    {
        CapsuleCollider2D template = ShieldHitboxTemplate();
        if (template == null)
            return;

        Vector2 facing = DirFromAngle(facingDegrees);
        Vector2 shieldCenter = center + FxOffset(AbilityType.Ultimate, facing);
        Vector2 wallSize = ShieldWallSize(template);
        Vector2 querySize = wallSize + new Vector2(PushQueryPadding, PushQueryPadding);

        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(EnemyLayerMask);
        int count = Physics2D.OverlapCapsule(shieldCenter, querySize, template.direction, facingDegrees, filter, ShieldPushBuffer);

        for (int i = 0; i < count; i++)
        {
            Enemy enemy = ShieldPushBuffer[i].GetComponent<Enemy>();
            if (enemy == null)
                continue;

            Vector2 toEnemy = enemy.BodyCenter - shieldCenter;
            bool inFront = Vector2.Dot(toEnemy, facing) > 0f;
            if (!inFront && !(onSpawn && OverlapsWall(ShieldPushBuffer[i], shieldCenter, wallSize, template.direction, facingDegrees)))
                continue;

            Vector2 pushDir = inFront && toEnemy.sqrMagnitude > 0.0001f ? toEnemy.normalized : facing;
            enemy.ApplyKnockback(pushDir * activeKit.ShieldKnockbackForce, activeKit.ShieldKnockbackDuration);
        }
    }

    // Runs on every peer for every raised shield: this peer's own player, and enemies on the server, are kept just in front of the wall, so they slide with it.
    private void UpdateShieldContactPush()
    {
        if (!isShieldActive.Value || activeKit == null)
        {
            SetPassThrough(null);
            return;
        }

        CapsuleCollider2D template = ShieldHitboxTemplate();
        if (template == null)
            return;

        float facingDegrees = shieldFacingAngle.Value;
        Vector2 facing = DirFromAngle(facingDegrees);
        Vector2 shieldCenter = (Vector2)transform.position + FxOffset(AbilityType.Ultimate, facing);
        Vector2 wallSize = ShieldWallSize(template);

        Player local = LocalPlayer.GetPlayer();
        if (local != null && local != this && !local.isMounted && local.bodyCollider != null && local.rb != null)
        {
            Vector2 push = WallFrontPushOut(BodyCenterNow(local.bodyCollider), local.bodyCollider, shieldCenter, facing, wallSize, shieldPushLead, activeKit.UltimateFxDistance);
            if (push == Vector2.zero)
                SetPassThrough(null);
            else if (passThroughPlayer != local && IsPushBlocked(local.bodyCollider, push))
                SetPassThrough(local);
            else if (passThroughPlayer != local)
                local.AddPush(push);
        }

        if (!this.HasServerAuthority())
            return;

        Vector2 querySize = wallSize + new Vector2(PushQueryPadding, PushQueryPadding);
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(EnemyLayerMask);
        int count = Physics2D.OverlapCapsule(shieldCenter, querySize, template.direction, facingDegrees, filter, ShieldPushBuffer);
        for (int i = 0; i < count; i++)
        {
            Enemy enemy = ShieldPushBuffer[i].GetComponent<Enemy>();
            if (enemy == null || Time.deltaTime <= 0f)
                continue;

            Vector2 displacement = WallFrontPushOut(BodyCenterNow(ShieldPushBuffer[i]), ShieldPushBuffer[i], shieldCenter, facing, wallSize, 0f, 0f);
            if (displacement != Vector2.zero)
                enemy.ApplyKnockback(Vector2.ClampMagnitude(displacement / Time.deltaTime, MaxPushSpeed), Time.fixedDeltaTime);
        }
    }

    // A body pinned between the wall and an obstacle passes through the wall instead of being crushed into it; collision returns once it leaves the wall's zone.
    private void SetPassThrough(Player player)
    {
        if (passThroughPlayer == player)
            return;

        if (passThroughPlayer != null && shieldWall != null && passThroughPlayer.bodyCollider != null)
            Physics2D.IgnoreCollision(shieldWall, passThroughPlayer.bodyCollider, false);

        passThroughPlayer = player;

        if (passThroughPlayer != null && shieldWall != null && passThroughPlayer.bodyCollider != null)
            Physics2D.IgnoreCollision(shieldWall, passThroughPlayer.bodyCollider, true);
    }

    // True when something solid other than a player or a shield sits within the push distance.
    private static bool IsPushBlocked(Collider2D body, Vector2 push)
    {
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(body.gameObject.layer));
        int count = body.Cast(push.normalized, filter, TrapCastBuffer, push.magnitude + TrapCheckMargin);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = TrapCastBuffer[i].collider;
            if (hit.GetComponentInParent<Player>() == null && hit.GetComponent<ShieldHoldFX>() == null)
                return true;
        }
        return false;
    }

    public void RegisterShieldWall(Collider2D wall)
    {
        SetPassThrough(null);
        shieldWall = wall;
    }

    // Push along the facing that puts a body lead past the wall's front; reachBehind also catches bodies the wall passed.
    private static Vector2 WallFrontPushOut(Vector2 bodyCenter, Collider2D collider, Vector2 shieldCenter, Vector2 facing, Vector2 wallSize, float lead, float reachBehind)
    {
        Vector2 offset = bodyCenter - shieldCenter;
        float along = Vector2.Dot(offset, facing);
        if (along <= -reachBehind)
            return Vector2.zero;

        float radius = Mathf.Min(collider.bounds.extents.x, collider.bounds.extents.y);
        float lateral = Mathf.Abs(facing.x * offset.y - facing.y * offset.x);
        if (lateral > wallSize.y * 0.5f + radius)
            return Vector2.zero;

        float needed = wallSize.x * 0.5f + radius + ShieldContactGap + lead;
        return along < needed ? facing * (needed - along) : Vector2.zero;
    }

    // Live collider centre from the transform, since bounds only refresh on the next physics step.
    private static Vector2 BodyCenterNow(Collider2D collider)
    {
        return collider.transform.TransformPoint(collider.offset);
    }

    // Shows a remote player's copy already pushed out of this peer's own player and shield, until its owner's real (pushed) position arrives.
    private void LateUpdateRemotePushPrediction()
    {
        if (!NetworkObject.IsSpawned || IsOwner || bodyCollider == null || transform.parent != null)
            return;

        Player local = LocalPlayer.GetPlayer();
        if (local == null || local == this || local.isMounted || local.bodyCollider == null)
            return;

        Vector2 center = BodyCenterNow(bodyCollider);
        float radius = Mathf.Min(bodyCollider.bounds.extents.x, bodyCollider.bounds.extents.y);
        Vector2 offset = center - BodyCenterNow(local.bodyCollider);
        float minDistance = radius + Mathf.Min(local.bodyCollider.bounds.extents.x, local.bodyCollider.bounds.extents.y);
        float distance = offset.magnitude;
        Vector2 push = Vector2.zero;
        if (distance < minDistance)
            push += (distance > 0.0001f ? offset / distance : Vector2.right) * (minDistance - distance) * SoftCollisionShare;

        CapsuleCollider2D template = local.isShieldActive.Value && local.activeKit != null ? local.ShieldHitboxTemplate() : null;
        if (template != null)
        {
            Vector2 facing = DirFromAngle(local.shieldFacingAngle.Value);
            Vector2 shieldCenter = (Vector2)local.transform.position + local.FxOffset(AbilityType.Ultimate, facing);
            Vector2 wallPush = WallFrontPushOut(center + push, bodyCollider, shieldCenter, facing, local.ShieldWallSize(template), 0f, local.activeKit.UltimateFxDistance);
            if (wallPush != Vector2.zero && !IsPushBlocked(bodyCollider, wallPush))
                push += wallPush;
        }

        if (push != Vector2.zero)
            transform.position += (Vector3)push;
    }

    // True when the collider touches the given wall shape.
    private static bool OverlapsWall(Collider2D target, Vector2 wallCenter, Vector2 wallSize, CapsuleDirection2D direction, float angle)
    {
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(1 << target.gameObject.layer);
        int count = Physics2D.OverlapCapsule(wallCenter, wallSize, direction, angle, filter, WallOverlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (WallOverlapBuffer[i] == target)
                return true;
        }
        return false;
    }

    // Same scale SpawnLocalFx applies to the FX: prefab scale x Ultimate Size x level-up size bonus.
    private Vector2 ShieldWallSize(CapsuleCollider2D template)
    {
        Vector2 prefabScale = template.transform.lossyScale;
        float size = activeKit.UltimateSize * SizeBonusMultiplier;
        return new Vector2(template.size.x * Mathf.Abs(prefabScale.x), template.size.y * Mathf.Abs(prefabScale.y)) * size;
    }

    private CapsuleCollider2D ShieldHitboxTemplate()
    {
        GameObject prefab = activeKit != null ? activeKit.UltimateFxPrefab : null;
        return prefab != null ? prefab.GetComponent<CapsuleCollider2D>() : null;
    }

    private static Vector2 DirFromAngle(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    // Any peer: a hit whose attacker-to-target line crosses an active shield is absorbed by it.
    public static bool TryAbsorbWithShield(Vector2 attackerPos, Vector2 targetPos, float damage)
    {
        ContactFilter2D filter = new ContactFilter2D { useLayerMask = true, layerMask = Physics2D.DefaultRaycastLayers, useTriggers = Physics2D.queriesHitTriggers };
        int count = Physics2D.Linecast(attackerPos, targetPos, filter, ShieldLineBuffer);
        for (int i = 0; i < count; i++)
        {
            ShieldHoldFX shield = ShieldLineBuffer[i].collider.GetComponent<ShieldHoldFX>();
            if (shield == null || shield.Owner == null || !shield.Owner.IsShieldActive)
                continue;

            shield.Owner.ApplyShieldDamage(damage);
            return true;
        }
        return false;
    }

    // Callable from any peer: the shield's owner writes its health.
    public void ApplyShieldDamage(float amount)
    {
        if (!NetworkObject.IsSpawned || IsOwner)
        {
            ReduceShieldHealth(amount);
            return;
        }
        ApplyShieldDamageRpc(amount);
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
    private void ApplyShieldDamageRpc(float amount)
    {
        ReduceShieldHealth(amount);
    }

    private void ReduceShieldHealth(float amount)
    {
        shieldHealth.Value = Mathf.Max(0f, shieldHealth.Value - amount);
        if (shieldHealth.Value <= 0f && isShieldActive.Value)
            EndShield();
    }
}
