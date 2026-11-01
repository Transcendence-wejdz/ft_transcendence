using Unity.Netcode;
using UnityEngine;

// The pool LevelUpManager offers 3-of-N from each level-up.
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

    // Survival, then offense, then mobility; the enum's own order is what saves and offers rely on, so it stays as it is.
    public static readonly LevelUpBonusType[] DisplayOrder = { LevelUpBonusType.Health, LevelUpBonusType.HealthRegen, LevelUpBonusType.ManaMax, LevelUpBonusType.ManaRegen, LevelUpBonusType.Damage, LevelUpBonusType.AttackSpeed, LevelUpBonusType.SpecialCooldown, LevelUpBonusType.Size, LevelUpBonusType.AttackMobility, LevelUpBonusType.MoveSpeed };

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
            default: return string.Empty;
        }
    }
}

// The 3 bonuses of the pool one player is offered this round, derived on every peer from the round's seed.
public struct OfferedBonusSet
{
    public LevelUpBonusType first;
    public LevelUpBonusType second;
    public LevelUpBonusType third;

    public LevelUpBonusType Get(int slot)
    {
        if (slot == 0)
            return first;
        if (slot == 1)
            return second;
        return third;
    }
}

// Per-player stack counts, one NetworkVariable for all of them so adding another type later doesn't touch the NetworkVariable count/order.
public struct LevelUpBonusStacks : INetworkSerializable
{
    public int attackSpeed;
    public int specialCooldown;
    public int damage;
    public int health;
    public int moveSpeed;
    public int healthRegen;
    public int manaMax;
    public int manaRegen;
    public int size;
    public int attackMobility;

    public int Get(LevelUpBonusType type)
    {
        switch (type)
        {
            case LevelUpBonusType.AttackSpeed: return attackSpeed;
            case LevelUpBonusType.SpecialCooldown: return specialCooldown;
            case LevelUpBonusType.Damage: return damage;
            case LevelUpBonusType.Health: return health;
            case LevelUpBonusType.MoveSpeed: return moveSpeed;
            case LevelUpBonusType.HealthRegen: return healthRegen;
            case LevelUpBonusType.ManaMax: return manaMax;
            case LevelUpBonusType.ManaRegen: return manaRegen;
            case LevelUpBonusType.Size: return size;
            case LevelUpBonusType.AttackMobility: return attackMobility;
            default: return 0;
        }
    }

    public void Increment(LevelUpBonusType type)
    {
        switch (type)
        {
            case LevelUpBonusType.AttackSpeed: attackSpeed++; break;
            case LevelUpBonusType.SpecialCooldown: specialCooldown++; break;
            case LevelUpBonusType.Damage: damage++; break;
            case LevelUpBonusType.Health: health++; break;
            case LevelUpBonusType.MoveSpeed: moveSpeed++; break;
            case LevelUpBonusType.HealthRegen: healthRegen++; break;
            case LevelUpBonusType.ManaMax: manaMax++; break;
            case LevelUpBonusType.ManaRegen: manaRegen++; break;
            case LevelUpBonusType.Size: size++; break;
            case LevelUpBonusType.AttackMobility: attackMobility++; break;
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref attackSpeed);
        serializer.SerializeValue(ref specialCooldown);
        serializer.SerializeValue(ref damage);
        serializer.SerializeValue(ref health);
        serializer.SerializeValue(ref moveSpeed);
        serializer.SerializeValue(ref healthRegen);
        serializer.SerializeValue(ref manaMax);
        serializer.SerializeValue(ref manaRegen);
        serializer.SerializeValue(ref size);
        serializer.SerializeValue(ref attackMobility);
    }
}
