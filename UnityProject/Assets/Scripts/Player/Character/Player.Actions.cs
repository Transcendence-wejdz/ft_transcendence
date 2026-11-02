using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Attack (autofire while held), Special and Ultimate: cooldowns, mana, move lock, and the FX spawned locally then mirrored.
public partial class Player
{
    public static event Action<AbilityType, float> OnAbilityUsed;

    // Animator params keep their original names (LightAttack/HeavyAttack) for Attack/Special.
    private static readonly int AttackHash = Animator.StringToHash("LightAttack");
    private static readonly int SpecialHash = Animator.StringToHash("HeavyAttack");
    private static readonly int UltimateHash = Animator.StringToHash("Ultimate");
    private static readonly int AttackAnimSpeedHash = Animator.StringToHash("AttackAnimSpeed");
    private static readonly int SpecialAnimSpeedHash = Animator.StringToHash("SpecialAnimSpeed");
    private static readonly int UltimateAnimSpeedHash = Animator.StringToHash("UltimateAnimSpeed");

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

    public ClassKit ActiveKit => activeKit;

    public float MovementSpeedMultiplier
    {
        get
        {
            if (isShieldActive.Value)
                return ShieldMoveSpeedMultiplier();
            if (Time.time < moveLockEndTime)
                return moveLockSpeedMultiplier;
            return 1f;
        }
    }

    // Dead players (pigs) can still move but not fight.
    private bool CanFight => !IsBlocked && !IsDead;

    private bool CanAutofire
    {
        get
        {
            if (!isAttackHeld || !this.IsLocallyControlled() || !CanFight)
                return false;
            return Time.time >= attackReadyTime && Ability(AbilityType.Attack) != null;
        }
    }

    // Offset of this player's aim indicator ring from its feet, read once since the ring only rotates.
    private Vector2 AimIndicatorCenter
    {
        get
        {
            if (!aimIndicatorCenter.HasValue)
                aimIndicatorCenter = FindAimIndicatorCenter();
            return aimIndicatorCenter.Value;
        }
    }

    private Vector2 FindAimIndicatorCenter()
    {
        MouseDirectionIndicator indicator = GetComponentInChildren<MouseDirectionIndicator>(true);

        if (indicator == null)
            return Vector2.zero;
        return indicator.transform.position - transform.position;
    }

    private float ShieldMoveSpeedMultiplier()
    {
        if (ShieldKit == null)
            return 1f;
        return ApplyAttackMobilityBonus(ShieldKit.MoveSpeedMultiplier);
    }

    private void RefreshActiveKit()
    {
        ClassKit kit = null;

        if (CharacterCatalog.Instance != null)
            kit = CharacterCatalog.Instance.GetKit(ClassIndex);
        if (kit != null)
            activeKit = kit;
        else
            activeKit = defaultKit;
    }

    private void HandleKitClassChanged(int classIndex, int colorIndex)
    {
        RefreshActiveKit();
    }

    private void AwakeActions()
    {
        RefreshActiveKit();
        OnClassOrColorChanged += HandleKitClassChanged;
    }

    private void StartMoveLock(AbilityKit kit)
    {
        moveLockEndTime = Time.time + kit.MoveLockDuration;
        moveLockSpeedMultiplier = ApplyAttackMobilityBonus(kit.MoveSpeedMultiplier);
    }

    private void PlayAbilityAnimation(int triggerHash, int speedHash, float speed)
    {
        animator.SetFloat(speedHash, speed);
        animator.SetTrigger(triggerHash);
    }

    private void FireAttack()
    {
        AbilityKit kit = Ability(AbilityType.Attack);
        float cooldown = kit.Cooldown * AttackCooldownMultiplier;

        PlayAbilityAnimation(AttackHash, AttackAnimSpeedHash, kit.AnimSpeed * AttackSpeedMultiplier);
        attackReadyTime = Time.time + cooldown;
        StartMoveLock(kit);
        OnAbilityUsed?.Invoke(AbilityType.Attack, cooldown);
        SpawnAbilityFx(AbilityType.Attack, AimedDistance(AbilityType.Attack));
    }

    private void UpdateActions()
    {
        if (CanAutofire)
            FireAttack();
        UpdateAim();
    }

    // This player's action in a slot, null when its class has none there.
    public AbilityKit Ability(AbilityType ability)
    {
        if (activeKit == null)
            return null;
        return activeKit.Ability(ability);
    }

    // Held down: keeps firing on its own once each cooldown ends, instead of requiring repeated clicks; hold kits also preview each shot.
    public void Attack(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;
        if (ctx.canceled)
        {
            isAttackHeld = false;
            ReleaseAim(AbilityType.Attack);
        }
        else if (ctx.performed)
        {
            isAttackHeld = true;
            if (IsHoldAim(AbilityType.Attack))
                BeginAim(AbilityType.Attack);
        }
    }

}
