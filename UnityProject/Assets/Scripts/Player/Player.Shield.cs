using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Warrior-style held Ultimate: a directional wall that pushes enemies in front and absorbs damage aimed at anything behind it, until its own HP runs out or the hold timer expires.
public partial class Player
{
    private static int enemyLayerMask = -1;
    private static int EnemyLayerMask => enemyLayerMask >= 0 ? enemyLayerMask : (enemyLayerMask = LayerMask.GetMask("Enemy"));
    private static readonly Collider2D[] ShieldPushBuffer = new Collider2D[32];
    private static readonly RaycastHit2D[] ShieldLineBuffer = new RaycastHit2D[8];
    // Only meaningful on a class whose Animator Controller actually has a "ShieldHeld" bool param (see ClassKit.ShieldHoldAnimation); Animator.SetBool on a missing param is a harmless no-op elsewhere.
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

    public bool IsShieldActive => isShieldActive.Value;
    public float CurrentShieldHealth => shieldHealth.Value;
    public float ShieldMaxHealth => activeKit != null ? activeKit.ShieldHealth : 0f;
    public float ShieldFacingAngle => shieldFacingAngle.Value;

    // Routed here from Ultimate() when activeKit.HasShield; a press raises it, a second press lowers it early (it also drops on its own when the hold timer or its HP runs out).
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
        PushNearbyEnemies();
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
            PushNearbyEnemies();
        }
    }

    private void EndShield()
    {
        isShieldActive.Value = false;
        ultimateReadyTime = Time.time + activeKit.UltimateCooldown;
        if (activeKit.ShieldHoldAnimation)
            animator.SetBool(ShieldHeldHash, false);
    }

    private void PushNearbyEnemies()
    {
        if (NetworkObject.IsSpawned)
            PushEnemiesServerRpc(transform.position, shieldFacingAngle.Value);
        else
            ApplyPush(transform.position, shieldFacingAngle.Value);
    }

    [ServerRpc]
    private void PushEnemiesServerRpc(Vector2 center, float facingDegrees)
    {
        ApplyPush(center, facingDegrees);
    }

    // Padding added on top of the shield's actual collider size for the push query only - the solid collider already stops enemies at its surface (touching, not overlapping), so an unpadded query of the same shape would never actually detect them.
    private const float PushQueryPadding = 0.8f;

    // Knocks back any enemy near the shield's actual collider shape (read from its FX prefab, scaled like the spawned FX) on the facing side - matches the physical wall instead of an abstract radius.
    private void ApplyPush(Vector2 center, float facingDegrees)
    {
        CapsuleCollider2D template = ShieldHitboxTemplate();
        if (template == null)
            return;

        Vector2 facing = DirFromAngle(facingDegrees);
        Vector2 shieldCenter = center + facing * activeKit.UltimateFxDistance;
        Vector2 querySize = ShieldWallSize(template) + new Vector2(PushQueryPadding, PushQueryPadding);

        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(EnemyLayerMask);
        int count = Physics2D.OverlapCapsule(shieldCenter, querySize, template.direction, facingDegrees, filter, ShieldPushBuffer);

        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = ShieldPushBuffer[i].GetComponent<EnemyAI>();
            if (enemy == null)
                continue;

            Vector2 toEnemy = enemy.BodyCenter - shieldCenter;
            if (Vector2.Dot(toEnemy, facing) <= 0f)
                continue;

            Vector2 pushDir = toEnemy.sqrMagnitude > 0.0001f ? toEnemy.normalized : facing;
            enemy.ApplyKnockback(pushDir * activeKit.ShieldKnockbackForce, activeKit.ShieldKnockbackDuration);
        }
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

    // Called server-side (EnemyActions.ResolveHit already runs with server authority); a line between attacker and target that crosses an active shield's real collider gets absorbed by it instead of reaching the target.
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

    public void ApplyShieldDamage(float amount)
    {
        if (!NetworkObject.IsSpawned)
        {
            ReduceShieldHealth(amount);
            return;
        }
        ApplyShieldDamageClientRpc(amount, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    private void ApplyShieldDamageClientRpc(float amount, ClientRpcParams rpcParams = default)
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
