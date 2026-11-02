using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Held shield Ultimate: a wall that pushes enemies and stops every hit and projectile coming at its front, until its HP or timer runs out.
public partial class Player
{
    private const float MinTravelSqr = 0.000001f;

    // Only classes whose controller has a "ShieldHeld" bool use it (see AbilityKit.ShieldHoldAnimation).
    private static readonly int ShieldHeldHash = Animator.StringToHash("ShieldHeld");
    private static readonly RaycastHit2D[] ShieldLineBuffer = new RaycastHit2D[8];
    private static int shieldWallMask = -1;

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
    public float ShieldFacingAngle => shieldFacingAngle.Value;

    public float ShieldMaxHealth
    {
        get
        {
            if (ShieldKit == null)
                return 0f;
            return ShieldKit.ShieldHealth;
        }
    }

    // The class's Ultimate when it is a held shield, null otherwise.
    private AbilityKit ShieldKit
    {
        get
        {
            AbilityKit kit = Ability(AbilityType.Ultimate);

            if (kit == null || kit.Kind != AbilityKind.Shield)
                return null;
            return kit;
        }
    }

    private static int ShieldWallMask
    {
        get
        {
            if (shieldWallMask < 0)
                shieldWallMask = LayerMask.GetMask("ShieldWall");
            return shieldWallMask;
        }
    }

    private bool ShouldDropShield => !CanFight || Time.time >= shieldEndTime || ShieldKit == null;

    private void PlayShieldAnimation(AbilityKit kit)
    {
        animator.SetFloat(UltimateAnimSpeedHash, kit.AnimSpeed);
        animator.SetTrigger(UltimateHash);
        if (kit.ShieldHoldAnimation)
            animator.SetBool(ShieldHeldHash, true);
    }

    private void StartShield()
    {
        AbilityKit kit = ShieldKit;

        isShieldActive.Value = true;
        shieldHealth.Value = kit.ShieldHealth;
        shieldFacingAngle.Value = AimAngleDegrees;
        shieldEndTime = Time.time + kit.ShieldHoldDuration;
        nextShieldPushTime = Time.time;
        PlayShieldAnimation(kit);
        OnAbilityUsed?.Invoke(AbilityType.Ultimate, kit.Cooldown);
        SpawnAbilityFx(AbilityType.Ultimate);
        PushNearbyEnemies(onSpawn: true);
    }

    // The cooldown starts once the shield drops, not when it is raised.
    private void EndShield()
    {
        AbilityKit kit = ShieldKit;

        isShieldActive.Value = false;
        if (kit == null)
            return;
        ultimateReadyTime = Time.time + kit.Cooldown;
        if (kit.ShieldHoldAnimation)
            animator.SetBool(ShieldHeldHash, false);
    }

    private bool CanRaiseShield()
    {
        if (!CanFight || Time.time < ultimateReadyTime)
            return false;
        return TrySpendMana(ShieldKit.ManaCost);
    }

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
        if (CanRaiseShield())
            StartShield();
    }

    private void TickShieldPush()
    {
        if (Time.time < nextShieldPushTime)
            return;
        nextShieldPushTime = Time.time + ShieldKit.ShieldPushInterval;
        PushNearbyEnemies(onSpawn: false);
    }

    private void UpdateShield()
    {
        if (!this.IsLocallyControlled() || !isShieldActive.Value)
            return;
        if (ShouldDropShield)
        {
            EndShield();
            return;
        }
        shieldFacingAngle.Value = Mathf.MoveTowardsAngle(shieldFacingAngle.Value, AimAngleDegrees, ShieldKit.ShieldTurnSpeed * Time.deltaTime);
        TickShieldPush();
    }

}
