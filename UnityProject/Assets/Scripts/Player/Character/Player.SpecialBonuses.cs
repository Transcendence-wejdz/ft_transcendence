using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Effects of the special level-up bonuses.
public partial class Player
{
    private const float DashStrikeRadius = 1f;
    private const float ExtraLifeBlinkInterval = 0.12f;
    private const float ExtraLifeBlinkAlpha = 0.35f;

    [SerializeField] private GameObject extraLifeFxPrefab;

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
        stacks.extraLifeSpent = true;
        levelUpBonusStacks.Value = stacks;
        invulnerableEndTime = Time.time + LevelUpBonusCatalog.ExtraLifeInvulnerableSeconds;
        PlayExtraLifeFeedback();
        if (NetworkObject.IsSpawned)
            ExtraLifeFeedbackRpc();
        return true;
    }

    // Blinks on scaled time like the invulnerability it shows; a disabled player gets its alpha back from StopSpawnFade.
    private IEnumerator ExtraLifeBlinkRoutine()
    {
        WaitForSeconds delay = new WaitForSeconds(ExtraLifeBlinkInterval);

        for (float t = 0f; t < LevelUpBonusCatalog.ExtraLifeInvulnerableSeconds; t += ExtraLifeBlinkInterval * 2f)
        {
            SetSpriteAlpha(ExtraLifeBlinkAlpha);
            yield return delay;
            SetSpriteAlpha(1f);
            yield return delay;
        }
    }

    private void PlayExtraLifeFeedback()
    {
        if (extraLifeFxPrefab != null)
            PlayerSpawner.TintSpawnFx(Instantiate(extraLifeFxPrefab, BodyCenter() + (Vector3)transformFxOffset, Quaternion.identity), LevelUpBonusCatalog.GetColor(LevelUpBonusType.ExtraLife));
        StartCoroutine(ExtraLifeBlinkRoutine());
    }

    [Rpc(SendTo.NotMe)]
    private void ExtraLifeFeedbackRpc()
    {
        PlayExtraLifeFeedback();
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
    public static bool IsProtectedAlly(GameObject attacker, IDamageable target)
    {
        if (RunOptions.Current.friendlyFire || !(target is Player))
            return false;
        return attacker != null && attacker.GetComponent<Player>() != null;
    }

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
