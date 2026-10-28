using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class Player
{
    public static event Action<AbilityType, float> OnAbilityUsed;

    [Header("Actions")]
    [SerializeField] private ClassKit defaultKit;

    private ClassKit activeKit;
    private Vector2? aimIndicatorCenter;

    private bool isAttackHeld;
    private float attackReadyTime;
    private float specialReadyTime;
    private float ultimateReadyTime;
    private float moveLockEndTime;
    private float moveLockSpeedMultiplier;

    public float MovementSpeedMultiplier
    {
        get
        {
            if (isShieldActive.Value)
                return activeKit != null ? ApplyAttackMobilityBonus(activeKit.UltimateMoveSpeedMultiplier) : 1f;
            return Time.time < moveLockEndTime ? moveLockSpeedMultiplier : 1f;
        }
    }

    // Dead players (pigs) can still move but not fight.
    private bool CanFight => !IsBlocked && !IsDead;

    // Animator params keep their original names (LightAttack/HeavyAttack) for Attack/Special.
    private static readonly int AttackHash = Animator.StringToHash("LightAttack");
    private static readonly int SpecialHash = Animator.StringToHash("HeavyAttack");
    private static readonly int UltimateHash = Animator.StringToHash("Ultimate");
    private static readonly int AttackAnimSpeedHash = Animator.StringToHash("AttackAnimSpeed");
    private static readonly int SpecialAnimSpeedHash = Animator.StringToHash("SpecialAnimSpeed");
    private static readonly int UltimateAnimSpeedHash = Animator.StringToHash("UltimateAnimSpeed");

    private void AwakeActions()
    {
        RefreshActiveKit();
        OnClassOrColorChanged += (_, _) => RefreshActiveKit();
    }

    public ClassKit ActiveKit => activeKit;

    private void RefreshActiveKit()
    {
        ClassKit kit = CharacterCatalog.Instance != null
            ? CharacterCatalog.Instance.GetKit(ClassIndex)
            : null;
        activeKit = kit != null ? kit : defaultKit;
    }

    // Held down: keeps firing on its own once each cooldown ends, instead of requiring repeated clicks.
    public void Attack(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;

        if (ctx.canceled)
            isAttackHeld = false;
        else if (ctx.performed)
            isAttackHeld = true;
    }

    private void UpdateActions()
    {
        if (isAttackHeld && this.IsLocallyControlled() && CanFight && Time.time >= attackReadyTime)
            FireAttack();
    }

    private void FireAttack()
    {
        float cooldown = activeKit.AttackCooldown * AttackCooldownMultiplier;
        animator.SetFloat(AttackAnimSpeedHash, activeKit.AttackAnimSpeed * AttackSpeedMultiplier);
        animator.SetTrigger(AttackHash);
        attackReadyTime = Time.time + cooldown;
        moveLockEndTime = Time.time + activeKit.AttackMoveLockDuration;
        moveLockSpeedMultiplier = ApplyAttackMobilityBonus(activeKit.AttackMoveSpeedMultiplier);
        OnAbilityUsed?.Invoke(AbilityType.Attack, cooldown);
        SpawnAbilityFx(AbilityType.Attack);
    }

    public void Special(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled() || !ctx.performed || !CanFight || Time.time < specialReadyTime)
            return;
        if (!TrySpendMana(activeKit.SpecialManaCost))
            return;

        float cooldown = activeKit.SpecialCooldown * SpecialCooldownMultiplier;
        animator.SetFloat(SpecialAnimSpeedHash, activeKit.SpecialAnimSpeed);
        animator.SetTrigger(SpecialHash);
        specialReadyTime = Time.time + cooldown;
        moveLockEndTime = Time.time + activeKit.SpecialMoveLockDuration;
        moveLockSpeedMultiplier = ApplyAttackMobilityBonus(activeKit.SpecialMoveSpeedMultiplier);
        OnAbilityUsed?.Invoke(AbilityType.Special, cooldown);
        SpawnAbilityFx(AbilityType.Special);
    }

    public void Ultimate(InputAction.CallbackContext ctx)
    {
        if (activeKit != null && activeKit.HasShield)
        {
            UltimateShield(ctx);
            return;
        }

        if (!this.IsLocallyControlled() || !ctx.performed || !CanFight || Time.time < ultimateReadyTime)
            return;
        if (!TrySpendMana(activeKit.UltimateManaCost))
            return;

        animator.SetFloat(UltimateAnimSpeedHash, activeKit.UltimateAnimSpeed);
        animator.SetTrigger(UltimateHash);
        ultimateReadyTime = Time.time + activeKit.UltimateCooldown;
        moveLockEndTime = Time.time + activeKit.UltimateMoveLockDuration;
        moveLockSpeedMultiplier = ApplyAttackMobilityBonus(activeKit.UltimateMoveSpeedMultiplier);
        OnAbilityUsed?.Invoke(AbilityType.Ultimate, activeKit.UltimateCooldown);
        SpawnAbilityFx(AbilityType.Ultimate);
    }

    // Offset of this player's aim indicator ring from its feet, read once since the ring only rotates.
    private Vector2 AimIndicatorCenter
    {
        get
        {
            if (!aimIndicatorCenter.HasValue)
            {
                MouseDirectionIndicator indicator = GetComponentInChildren<MouseDirectionIndicator>(true);
                aimIndicatorCenter = indicator != null ? (Vector2)(indicator.transform.position - transform.position) : Vector2.zero;
            }
            return aimIndicatorCenter.Value;
        }
    }

    // Where this player's FX for an action spawns, relative to its feet (see ClassKit.FxOffset).
    public Vector2 FxOffset(AbilityType ability, Vector2 aim) => activeKit != null ? activeKit.FxOffset(ability, aim, AimIndicatorCenter) : Vector2.zero;

    private GameObject GetFxPrefab(AbilityType ability) => ability switch
    {
        AbilityType.Special => activeKit.SpecialFxPrefab,
        AbilityType.Ultimate => activeKit.UltimateFxPrefab,
        _ => activeKit.AttackFxPrefab,
    };

    private float GetSize(AbilityType ability) => ability switch
    {
        AbilityType.Special => activeKit.SpecialSize,
        AbilityType.Ultimate => activeKit.UltimateSize,
        _ => activeKit.AttackSize,
    };

    private bool GetIgnoreAimRotation(AbilityType ability) => ability switch
    {
        AbilityType.Special => activeKit.SpecialIgnoreAimRotation,
        AbilityType.Ultimate => activeKit.UltimateIgnoreAimRotation,
        _ => activeKit.AttackIgnoreAimRotation,
    };

    private void SpawnAbilityFx(AbilityType ability)
    {
        if (GetFxPrefab(ability) == null)
            return;

        float angle = AimAngleDegrees;
        SpawnLocalFx(angle, hasHitbox: true, ability);

        if (NetworkObject.IsSpawned)
            SpawnAbilityFxServerRpc(angle, ability);
    }

    [ServerRpc]
    private void SpawnAbilityFxServerRpc(float angle, AbilityType ability)
    {
        SpawnAbilityFxClientRpc(angle, OwnerClientId, ability);
    }

    // Mirrors the FX on the other clients only, the attacker already spawned its own (with the hitbox) instantly.
    [ClientRpc]
    private void SpawnAbilityFxClientRpc(float angle, ulong attackerClientId, AbilityType ability)
    {
        if (NetworkManager.Singleton.LocalClientId == attackerClientId)
            return;

        SpawnLocalFx(angle, hasHitbox: false, ability);
    }

    // Position is computed locally from the attacker's current transform, not sent over RPC - round-trip latency would otherwise bake a stale offset into the spawn point.
    private void SpawnLocalFx(float angle, bool hasHitbox, AbilityType ability)
    {
        GameObject prefab = GetFxPrefab(ability);
        if (prefab == null)
            return;

        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector3 spawnPos = transform.position + (Vector3)FxOffset(ability, dir);
        Quaternion rotation = GetIgnoreAimRotation(ability) ? Quaternion.identity : Quaternion.Euler(0f, 0f, angle);

        GameObject fx = Instantiate(prefab, spawnPos, rotation);
        fx.transform.localScale *= GetSize(ability) * SizeBonusMultiplier;
        fx.GetComponent<IAttackFX>()?.Init(gameObject, hasHitbox, activeKit, ability, DamageMultiplier);
    }
}
