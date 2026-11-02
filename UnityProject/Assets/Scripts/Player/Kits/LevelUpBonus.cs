using UnityEngine;

// Level-up bonuses, normal then special. Append only: saves index by value.
public enum LevelUpBonusType
{
    AttackSpeed,
    SpecialCooldown,
    Damage,
    Health,
    MoveSpeed,
    HealthRegen,
    ManaMax,
    ManaRegen,
    Size,
    AttackMobility,
    DodgeCharge,
    DodgeRecharge,
    Lifesteal,
    ExtraLife,
    CritChance,
    Thorns,
    DashDamage,
    DodgeInvulnerability,
}

// Per-stack effect size and display text for each bonus type.
public static class LevelUpBonusCatalog
{
    public const float AttackCooldownReductionPerStack = 0.1f;
    public const float SpecialCooldownReductionPerStack = 0.1f;
    public const float DamageBonusPerStack = 0.1f;
    public const float HealthBonusPerStack = 20f;
    public const float MoveSpeedBonusPerStack = 0.1f;
    public const float HealthRegenBonusPerStack = 1f;
    public const float ManaBonusPerStack = 20f;
    public const float ManaRegenBonusPerStack = 1f;
    public const float SizeBonusPerStack = 0.1f;
    public const float AttackMobilityBonusPerStack = 0.1f;
    public const int DodgeChargesPerStack = 1;
    public const float DodgeRechargeKeptPerStack = 0.75f;
    public const float LifestealPerStack = 0.05f;
    public const float ExtraLifeHealthFraction = 0.5f;
    public const float ExtraLifeInvulnerableSeconds = 2f;
    public const float CritChancePerStack = 0.15f;
    public const float CritDamageMultiplier = 2f;
    public const float ThornsPerStack = 0.5f;
    public const float DashDamagePerStack = 25f;
    public const float DodgeInvulnerabilityPerStack = 0.15f;
    public const int SpecialLevelInterval = 10;

    public static readonly LevelUpBonusType[] NormalTypes = { LevelUpBonusType.AttackSpeed, LevelUpBonusType.SpecialCooldown, LevelUpBonusType.Damage, LevelUpBonusType.Health, LevelUpBonusType.MoveSpeed, LevelUpBonusType.HealthRegen, LevelUpBonusType.ManaMax, LevelUpBonusType.ManaRegen, LevelUpBonusType.Size, LevelUpBonusType.AttackMobility };

    public static readonly LevelUpBonusType[] SpecialTypes = { LevelUpBonusType.DodgeCharge, LevelUpBonusType.DodgeRecharge, LevelUpBonusType.DashDamage, LevelUpBonusType.DodgeInvulnerability, LevelUpBonusType.Lifesteal, LevelUpBonusType.ExtraLife, LevelUpBonusType.CritChance, LevelUpBonusType.Thorns };

    // Survival, then offense, then mobility; the enum's own order is what saves and offers rely on, so it stays as it is.
    public static readonly LevelUpBonusType[] DisplayOrder = { LevelUpBonusType.Health, LevelUpBonusType.HealthRegen, LevelUpBonusType.ManaMax, LevelUpBonusType.ManaRegen, LevelUpBonusType.Damage, LevelUpBonusType.AttackSpeed, LevelUpBonusType.SpecialCooldown, LevelUpBonusType.Size, LevelUpBonusType.AttackMobility, LevelUpBonusType.MoveSpeed };

    public static bool IsSpecialLevel(int level)
    {
        return level > 0 && level % SpecialLevelInterval == 0;
    }

    public static bool IsSpecial(LevelUpBonusType type)
    {
        return System.Array.IndexOf(SpecialTypes, type) >= 0;
    }

    public static LevelUpBonusType[] OfferPool(int level)
    {
        if (IsSpecialLevel(level))
            return SpecialTypes;
        return NormalTypes;
    }

    // A family of bonuses shares a hue: health is red, mana is blue, the regen variant of each a lighter shade.
    public static Color32 GetColor(LevelUpBonusType type)
    {
        switch (type)
        {
            case LevelUpBonusType.AttackSpeed: return new Color32(0xF5, 0xD5, 0x45, 0xFF);
            case LevelUpBonusType.SpecialCooldown: return new Color32(0xB3, 0x7D, 0xFF, 0xFF);
            case LevelUpBonusType.Damage: return new Color32(0xFF, 0x9A, 0x3D, 0xFF);
            case LevelUpBonusType.Health: return new Color32(0xF0, 0x4F, 0x4A, 0xFF);
            case LevelUpBonusType.MoveSpeed: return new Color32(0x4F, 0xC9, 0x5A, 0xFF);
            case LevelUpBonusType.HealthRegen: return new Color32(0xFF, 0x9A, 0x94, 0xFF);
            case LevelUpBonusType.ManaMax: return new Color32(0x4C, 0x8D, 0xFF, 0xFF);
            case LevelUpBonusType.ManaRegen: return new Color32(0x8F, 0xC4, 0xFF, 0xFF);
            case LevelUpBonusType.Size: return new Color32(0xF0, 0x6F, 0xC8, 0xFF);
            case LevelUpBonusType.AttackMobility: return new Color32(0x3F, 0xD1, 0xC6, 0xFF);
            case LevelUpBonusType.DodgeCharge: return new Color32(0x7F, 0xE0, 0xFF, 0xFF);
            case LevelUpBonusType.DodgeRecharge: return new Color32(0xB5, 0xEC, 0xFF, 0xFF);
            case LevelUpBonusType.DashDamage: return new Color32(0xFF, 0x6B, 0x3D, 0xFF);
            case LevelUpBonusType.DodgeInvulnerability: return new Color32(0xE8, 0xF4, 0xFF, 0xFF);
            case LevelUpBonusType.Lifesteal: return new Color32(0xD9, 0x2B, 0x5A, 0xFF);
            case LevelUpBonusType.ExtraLife: return new Color32(0xFF, 0xD7, 0x6A, 0xFF);
            case LevelUpBonusType.CritChance: return new Color32(0xFF, 0xE0, 0x3D, 0xFF);
            case LevelUpBonusType.Thorns: return new Color32(0x9B, 0xC4, 0x3A, 0xFF);
            default: return new Color32(0xED, 0xE6, 0xD6, 0xFF);
        }
    }

    public static string GetDisplayName(LevelUpBonusType type)
    {
        switch (type)
        {
            case LevelUpBonusType.AttackSpeed: return "Attack Speed";
            case LevelUpBonusType.SpecialCooldown: return "Special Cooldown";
            case LevelUpBonusType.Damage: return "Damage";
            case LevelUpBonusType.Health: return "Health";
            case LevelUpBonusType.MoveSpeed: return "Move Speed";
            case LevelUpBonusType.HealthRegen: return "Health Regen";
            case LevelUpBonusType.ManaMax: return "Mana";
            case LevelUpBonusType.ManaRegen: return "Mana Regen";
            case LevelUpBonusType.Size: return "Attack Size";
            case LevelUpBonusType.AttackMobility: return "Attack Mobility";
            case LevelUpBonusType.DodgeCharge: return "Extra Dash";
            case LevelUpBonusType.DodgeRecharge: return "Dash Recharge";
            case LevelUpBonusType.DashDamage: return "Dash Strike";
            case LevelUpBonusType.DodgeInvulnerability: return "Dash Guard";
            case LevelUpBonusType.Lifesteal: return "Lifesteal";
            case LevelUpBonusType.ExtraLife: return "Extra Life";
            case LevelUpBonusType.CritChance: return "Critical Hit";
            case LevelUpBonusType.Thorns: return "Thorns";
            default: return type.ToString();
        }
    }

    // The real total for that many stacks: special cooldown shrinks by a divisor, so its percent doesn't simply add up.
    public static string GetTotalText(LevelUpBonusType type, int count)
    {
        switch (type)
        {
            case LevelUpBonusType.AttackSpeed: return $"+{count * AttackCooldownReductionPerStack * 100f:0}%";
            case LevelUpBonusType.SpecialCooldown: return $"-{(1f - 1f / (1f + count * SpecialCooldownReductionPerStack)) * 100f:0}%";
            case LevelUpBonusType.Damage: return $"+{count * DamageBonusPerStack * 100f:0}%";
            case LevelUpBonusType.Health: return $"+{count * HealthBonusPerStack:0}";
            case LevelUpBonusType.MoveSpeed: return $"+{count * MoveSpeedBonusPerStack * 100f:0}%";
            case LevelUpBonusType.HealthRegen: return $"+{count * HealthRegenBonusPerStack:0}/s";
            case LevelUpBonusType.ManaMax: return $"+{count * ManaBonusPerStack:0}";
            case LevelUpBonusType.ManaRegen: return $"+{count * ManaRegenBonusPerStack:0}/s";
            case LevelUpBonusType.Size: return $"+{count * SizeBonusPerStack * 100f:0}%";
            case LevelUpBonusType.AttackMobility: return $"-{count * AttackMobilityBonusPerStack * 100f:0} pts";
            case LevelUpBonusType.DodgeCharge: return $"+{count * DodgeChargesPerStack}";
            case LevelUpBonusType.DodgeRecharge: return $"-{(1f - Mathf.Pow(DodgeRechargeKeptPerStack, count)) * 100f:0}%";
            case LevelUpBonusType.DashDamage: return $"{count * DashDamagePerStack:0} dmg";
            case LevelUpBonusType.DodgeInvulnerability: return $"+{count * DodgeInvulnerabilityPerStack:0.00} s";
            case LevelUpBonusType.Lifesteal: return $"{count * LifestealPerStack * 100f:0}%";
            case LevelUpBonusType.ExtraLife: return $"{count} left";
            case LevelUpBonusType.CritChance: return $"{Mathf.Min(1f, count * CritChancePerStack) * 100f:0}%";
            case LevelUpBonusType.Thorns: return $"{count * ThornsPerStack * 100f:0}%";
            default: return string.Empty;
        }
    }

    public static string GetDescription(LevelUpBonusType type)
    {
        switch (type)
        {
            case LevelUpBonusType.AttackSpeed: return $"-{AttackCooldownReductionPerStack * 100f:0}% attack cooldown";
            case LevelUpBonusType.SpecialCooldown: return $"-{SpecialCooldownReductionPerStack * 100f:0}% special cooldown";
            case LevelUpBonusType.Damage: return $"+{DamageBonusPerStack * 100f:0}% damage";
            case LevelUpBonusType.Health: return $"+{HealthBonusPerStack:0} max health";
            case LevelUpBonusType.MoveSpeed: return $"+{MoveSpeedBonusPerStack * 100f:0}% move speed";
            case LevelUpBonusType.HealthRegen: return $"+{HealthRegenBonusPerStack:0} health regen/s";
            case LevelUpBonusType.ManaMax: return $"+{ManaBonusPerStack:0} max mana";
            case LevelUpBonusType.ManaRegen: return $"+{ManaRegenBonusPerStack:0} mana regen/s";
            case LevelUpBonusType.Size: return $"+{SizeBonusPerStack * 100f:0}% attack size";
            case LevelUpBonusType.AttackMobility: return $"-{AttackMobilityBonusPerStack * 100f:0} pts attack slow";
            case LevelUpBonusType.DodgeCharge: return $"+{DodgeChargesPerStack} dash charge";
            case LevelUpBonusType.DodgeRecharge: return $"-{(1f - DodgeRechargeKeptPerStack) * 100f:0}% dash recharge time";
            case LevelUpBonusType.DashDamage: return $"Dashing deals {DashDamagePerStack:0} damage";
            case LevelUpBonusType.DodgeInvulnerability: return $"+{DodgeInvulnerabilityPerStack:0.00} s dash invulnerability";
            case LevelUpBonusType.Lifesteal: return $"Heal {LifestealPerStack * 100f:0}% of damage dealt";
            case LevelUpBonusType.ExtraLife: return $"Survive one death at {ExtraLifeHealthFraction * 100f:0}% health";
            case LevelUpBonusType.CritChance: return $"+{CritChancePerStack * 100f:0}% chance of x{CritDamageMultiplier:0} damage";
            case LevelUpBonusType.Thorns: return $"Melee attackers take {ThornsPerStack * 100f:0}% of their hit";
            default: return string.Empty;
        }
    }
}
