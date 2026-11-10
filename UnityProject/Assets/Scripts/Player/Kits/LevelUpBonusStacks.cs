using Unity.Netcode;

// The 3 bonuses one player is offered this round, derived on every peer from its seed.
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

// Per-player stacks in one NetworkVariable, so a new type keeps the NetworkVariable layout; normal stats are floats since permanent levels grant fractions.
public struct LevelUpBonusStacks : INetworkSerializable
{
    public float attackSpeed;
    public float specialCooldown;
    public float damage;
    public float health;
    public float moveSpeed;
    public float healthRegen;
    public float manaMax;
    public float manaRegen;
    public float size;
    public float attackMobility;
    public int dodgeCharge;
    public int dodgeRecharge;
    public int lifesteal;
    public int extraLife;
    public int critChance;
    public int thorns;
    public int dashDamage;
    public int dodgeInvulnerability;
    public int piercing;
    public bool extraLifeSpent;

    // Special bonuses are offered once per run, and a spent extra life has no stack left to show it was taken.
    public bool IsTaken(LevelUpBonusType type)
    {
        return Get(type) > 0f || (type == LevelUpBonusType.ExtraLife && extraLifeSpent);
    }

    public float Get(LevelUpBonusType type)
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
            case LevelUpBonusType.DodgeCharge: return dodgeCharge;
            case LevelUpBonusType.DodgeRecharge: return dodgeRecharge;
            case LevelUpBonusType.Lifesteal: return lifesteal;
            case LevelUpBonusType.ExtraLife: return extraLife;
            case LevelUpBonusType.CritChance: return critChance;
            case LevelUpBonusType.Thorns: return thorns;
            case LevelUpBonusType.DashDamage: return dashDamage;
            case LevelUpBonusType.DodgeInvulnerability: return dodgeInvulnerability;
            case LevelUpBonusType.Piercing: return piercing;
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
            case LevelUpBonusType.DodgeCharge: dodgeCharge++; break;
            case LevelUpBonusType.DodgeRecharge: dodgeRecharge++; break;
            case LevelUpBonusType.Lifesteal: lifesteal++; break;
            case LevelUpBonusType.ExtraLife: extraLife++; break;
            case LevelUpBonusType.CritChance: critChance++; break;
            case LevelUpBonusType.Thorns: thorns++; break;
            case LevelUpBonusType.DashDamage: dashDamage++; break;
            case LevelUpBonusType.DodgeInvulnerability: dodgeInvulnerability++; break;
            case LevelUpBonusType.Piercing: piercing++; break;
        }
    }

    // Normal stats only: the permanent upgrades' starting amount.
    public void Add(LevelUpBonusType type, float amount)
    {
        switch (type)
        {
            case LevelUpBonusType.AttackSpeed: attackSpeed += amount; break;
            case LevelUpBonusType.SpecialCooldown: specialCooldown += amount; break;
            case LevelUpBonusType.Damage: damage += amount; break;
            case LevelUpBonusType.Health: health += amount; break;
            case LevelUpBonusType.MoveSpeed: moveSpeed += amount; break;
            case LevelUpBonusType.HealthRegen: healthRegen += amount; break;
            case LevelUpBonusType.ManaMax: manaMax += amount; break;
            case LevelUpBonusType.ManaRegen: manaRegen += amount; break;
            case LevelUpBonusType.Size: size += amount; break;
            case LevelUpBonusType.AttackMobility: attackMobility += amount; break;
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
        serializer.SerializeValue(ref dodgeCharge);
        serializer.SerializeValue(ref dodgeRecharge);
        serializer.SerializeValue(ref lifesteal);
        serializer.SerializeValue(ref extraLife);
        serializer.SerializeValue(ref critChance);
        serializer.SerializeValue(ref thorns);
        serializer.SerializeValue(ref dashDamage);
        serializer.SerializeValue(ref dodgeInvulnerability);
        serializer.SerializeValue(ref piercing);
        serializer.SerializeValue(ref extraLifeSpent);
    }
}
