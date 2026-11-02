using System.Collections.Generic;
using UnityEngine;

// Effects of the special level-up bonuses.
public partial class Player
{
    private const float DashStrikeRadius = 1f;

    private static readonly Collider2D[] DashStrikeBuffer = new Collider2D[32];

    private readonly HashSet<IDamageable> dashStruck = new HashSet<IDamageable>();

    private float invulnerableEndTime;

    private bool IsInvulnerable => Time.time < invulnerableEndTime;

    public bool HasProjectile => IsProjectile(AbilityType.Attack) || IsProjectile(AbilityType.Special) || IsProjectile(AbilityType.Ultimate);

    private int DodgeChargeBonus => levelUpBonusStacks.Value.dodgeCharge * LevelUpBonusCatalog.DodgeChargesPerStack;

    private float DodgeRechargeTime =>
        activeKit.DodgeRechargeSeconds * Mathf.Pow(LevelUpBonusCatalog.DodgeRechargeKeptPerStack, levelUpBonusStacks.Value.dodgeRecharge);

    private float DodgeInvulnerabilityBonus =>
        levelUpBonusStacks.Value.dodgeInvulnerability * LevelUpBonusCatalog.DodgeInvulnerabilityPerStack;

    // Lifesteal skips players and decor, and lingering attacks must not revive a dead attacker.
    private void DealDamage(IDamageable target, float damage)
    {
        LevelUpBonusStacks stacks = levelUpBonusStacks.Value;

        if (Random.value < stacks.critChance * LevelUpBonusCatalog.CritChancePerStack)
            damage *= LevelUpBonusCatalog.CritDamageMultiplier;
        target.RequestDamage(damage);
        if (stacks.lifesteal > 0 && !IsDead && !(target is Player) && !(target is DestructibleDecor))
            Heal(damage * stacks.lifesteal * LevelUpBonusCatalog.LifestealPerStack);
    }

    // Spends an extra life on a killing blow; the stack count is what remains.
    private bool TryConsumeExtraLife()
    {
        LevelUpBonusStacks stacks = levelUpBonusStacks.Value;

        if (stacks.extraLife <= 0)
            return false;
        stacks.extraLife--;
        levelUpBonusStacks.Value = stacks;
        invulnerableEndTime = Time.time + LevelUpBonusCatalog.ExtraLifeInvulnerableSeconds;
        return true;
    }

    private void StartDashStrike()
    {
        dashStruck.Clear();
        invulnerableEndTime = Mathf.Max(invulnerableEndTime, dodgeEndTime + DodgeInvulnerabilityBonus);
    }

    private void StrikeDashTarget(Collider2D other, float damage)
    {
        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target != null && !(target is Player) && dashStruck.Add(target))
            DealDamage(target, damage);
    }

    // Owner only: each target caught along the dash is struck once per dash.
    private void UpdateDashStrike()
    {
        float damage = levelUpBonusStacks.Value.dashDamage * LevelUpBonusCatalog.DashDamagePerStack * DamageMultiplier;
        int count;

        if (!IsDodging || damage <= 0f)
            return;
        count = Physics2D.OverlapCircle(Center, DashStrikeRadius, ContactFilter2D.noFilter, DashStrikeBuffer);
        for (int i = 0; i < count; i++)
            StrikeDashTarget(DashStrikeBuffer[i], damage);
    }

    // Extra targets a player's projectile goes through; an enemy attacker has no stacks.
    public static int BonusPierceOf(GameObject attacker)
    {
        Player player = null;

        if (attacker != null)
            player = attacker.GetComponent<Player>();
        if (player == null)
            return 0;
        return player.levelUpBonusStacks.Value.piercing * LevelUpBonusCatalog.PiercePerStack;
    }

    // Attacker's machine only: every player attack passes here, so crits and lifesteal apply to all.
    public static void DealAttackDamage(GameObject attacker, IDamageable target, float damage)
    {
        Player player = null;

        if (attacker != null)
            player = attacker.GetComponent<Player>();
        if (player == null)
            target.RequestDamage(damage);
        else
            player.DealDamage(target, damage);
    }

    // A melee hit from an enemy or boss; thorns only answer a hit that lands.
    public void TakeMeleeHit(float amount, IDamageable attacker)
    {
        int thorns = levelUpBonusStacks.Value.thorns;

        if (!this.IsLocallyControlled())
        {
            RequestDamage(amount);
            return;
        }
        if (IsInvulnerable)
            return;
        TakeDamage(amount);
        if (thorns > 0)
            attacker.RequestDamage(amount * thorns * LevelUpBonusCatalog.ThornsPerStack);
    }
}
