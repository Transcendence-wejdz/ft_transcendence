using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Special and Ultimate casts: instant on press, or aimed while held and cast on release.
public partial class Player
{
    // Off cooldown and paid for; the mana is only spent once every other check passed.
    private bool TryPayCast(AbilityKit kit, float readyTime)
    {
        if (kit == null || !CanFight || Time.time < readyTime)
            return false;
        return TrySpendMana(kit.ManaCost);
    }

    // A placed distance of -1 keeps the kit's FX distance.
    private void CastSpecial(float distance)
    {
        AbilityKit kit = Ability(AbilityType.Special);
        float cooldown;

        if (!TryPayCast(kit, specialReadyTime))
            return;
        cooldown = kit.Cooldown * SpecialCooldownMultiplier;
        PlayAbilityAnimation(SpecialHash, SpecialAnimSpeedHash, kit.AnimSpeed);
        specialReadyTime = Time.time + cooldown;
        StartMoveLock(kit);
        OnAbilityUsed?.Invoke(AbilityType.Special, cooldown);
        SpawnAbilityFx(AbilityType.Special, distance);
    }

    // A placed distance of -1 keeps the kit's FX distance.
    private void CastUltimate(float distance)
    {
        AbilityKit kit = Ability(AbilityType.Ultimate);

        if (!TryPayCast(kit, ultimateReadyTime))
            return;
        PlayAbilityAnimation(UltimateHash, UltimateAnimSpeedHash, kit.AnimSpeed);
        ultimateReadyTime = Time.time + kit.Cooldown;
        StartMoveLock(kit);
        OnAbilityUsed?.Invoke(AbilityType.Ultimate, kit.Cooldown);
        SpawnAbilityFx(AbilityType.Ultimate, distance);
    }

    private void Cast(AbilityType ability, float distance)
    {
        if (ability == AbilityType.Special)
            CastSpecial(distance);
        else if (ability == AbilityType.Ultimate)
            CastUltimate(distance);
    }

    // Instant kits cast on press; hold kits preview on press and cast on release (see Player.AbilityAim).
    private void HandleCastInput(AbilityType ability, InputAction.CallbackContext ctx)
    {
        if (!IsHoldAim(ability))
        {
            if (ctx.performed)
                Cast(ability, -1f);
            return;
        }
        if (ctx.performed)
            BeginAim(ability);
        else if (ctx.canceled)
            ReleaseAim(ability);
    }

    public void Special(InputAction.CallbackContext ctx)
    {
        if (this.IsLocallyControlled())
            HandleCastInput(AbilityType.Special, ctx);
    }

    // A shield Ultimate has its own held input (see Player.Shield).
    public void Ultimate(InputAction.CallbackContext ctx)
    {
        if (ShieldKit != null)
        {
            UltimateShield(ctx);
            return;
        }
        if (this.IsLocallyControlled())
            HandleCastInput(AbilityType.Ultimate, ctx);
    }
}
