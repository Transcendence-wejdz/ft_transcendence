using Unity.Netcode;

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
            case LevelUpBonusType.ManaMax: return "Max Mana";
            case LevelUpBonusType.ManaRegen: return "Mana Regen";
            case LevelUpBonusType.Size: return "Attack Size";
            case LevelUpBonusType.AttackMobility: return "Attack Mobility";
            default: return type.ToString();
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

// Which 3 of the pool are on offer this level-up round; replicated so every client shows the same choices.
public struct OfferedBonusSet : INetworkSerializable
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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref first);
        serializer.SerializeValue(ref second);
        serializer.SerializeValue(ref third);
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
