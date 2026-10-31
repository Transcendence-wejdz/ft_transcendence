using Unity.Netcode;
using UnityEngine;

// Level-up bonus stacks and the choice for the current level-up round, owned by each player.
public partial class Player
{
    private readonly NetworkVariable<bool> hasChosenLevelUpBonus = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<LevelUpBonusStacks> levelUpBonusStacks = new OfflineSafeNetworkVariable<LevelUpBonusStacks>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool HasChosenLevelUpBonus => hasChosenLevelUpBonus.Value;
    public LevelUpBonusStacks BonusStacks => levelUpBonusStacks.Value;

    public float AttackSpeedMultiplier =>
        1f + levelUpBonusStacks.Value.attackSpeed * LevelUpBonusCatalog.AttackCooldownReductionPerStack;
    public float AttackCooldownMultiplier => 1f / AttackSpeedMultiplier;
    public float SpecialCooldownMultiplier =>
        1f / (1f + levelUpBonusStacks.Value.specialCooldown * LevelUpBonusCatalog.SpecialCooldownReductionPerStack);
    public float DamageMultiplier =>
        1f + levelUpBonusStacks.Value.damage * LevelUpBonusCatalog.DamageBonusPerStack;
    public float LevelUpHealthBonus =>
        levelUpBonusStacks.Value.health * LevelUpBonusCatalog.HealthBonusPerStack;
    public float MoveSpeedBonusMultiplier =>
        1f + levelUpBonusStacks.Value.moveSpeed * LevelUpBonusCatalog.MoveSpeedBonusPerStack;
    public float LevelUpHealthRegenBonus =>
        levelUpBonusStacks.Value.healthRegen * LevelUpBonusCatalog.HealthRegenBonusPerStack;
    public float LevelUpManaBonus =>
        levelUpBonusStacks.Value.manaMax * LevelUpBonusCatalog.ManaBonusPerStack;
    public float LevelUpManaRegenBonus =>
        levelUpBonusStacks.Value.manaRegen * LevelUpBonusCatalog.ManaRegenBonusPerStack;
    public float SizeBonusMultiplier =>
        1f + levelUpBonusStacks.Value.size * LevelUpBonusCatalog.SizeBonusPerStack;
    public float AttackMobilityBonus =>
        levelUpBonusStacks.Value.attackMobility * LevelUpBonusCatalog.AttackMobilityBonusPerStack;

    // Raises a move-lock speed fraction toward 1 (no slowdown), never past it.
    private float ApplyAttackMobilityBonus(float baseMultiplier)
    {
        return Mathf.Min(1f, baseMultiplier + AttackMobilityBonus);
    }

    private bool HasCarriedOfflineStacks()
    {
        if (NetworkObject.IsSpawned || !this.IsLocallyControlled())
            return false;
        return RunCarryOver.IsForActiveScene && RunCarryOver.HasOfflineBonusStacks;
    }

    private ClientRpcParams OwnerOnlyParams()
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };
    }

    private void ChooseLevelUpBonusLocal(LevelUpBonusType type)
    {
        LevelUpBonusStacks stacks = levelUpBonusStacks.Value;

        hasChosenLevelUpBonus.Value = true;
        stacks.Increment(type);
        levelUpBonusStacks.Value = stacks;
        if (type == LevelUpBonusType.Health)
            Heal(LevelUpBonusCatalog.HealthBonusPerStack);
        else if (type == LevelUpBonusType.ManaMax)
            RestoreMana(LevelUpBonusCatalog.ManaBonusPerStack);
    }

    private void ResetLevelUpChoiceLocal()
    {
        hasChosenLevelUpBonus.Value = false;
    }

    [ClientRpc]
    private void ForceLevelUpBonusClientRpc(LevelUpBonusType type, ClientRpcParams rpcParams = default)
    {
        ChooseLevelUpBonusLocal(type);
    }

    [ClientRpc]
    private void ResetLevelUpChoiceClientRpc(ClientRpcParams rpcParams = default)
    {
        ResetLevelUpChoiceLocal();
    }

    // Offline only: re-applies the bonuses an offline solo player earned before a scene change (see RunCarryOver).
    private void StartLevelUp()
    {
        if (!HasCarriedOfflineStacks())
            return;
        levelUpBonusStacks.Value = RunCarryOver.OfflineBonusStacks;
        ResetToFull();
    }

    // Called on the local player when a fresh run starts: the run's stacks begin at the bought permanent upgrades, at full health and mana.
    public void ApplyPermanentUpgrades()
    {
        if (!this.IsLocallyControlled())
            return;
        levelUpBonusStacks.Value = PermanentUpgrades.StartingStacks();
        ResetToFull();
    }

    // Called by this player's own level-up UI.
    public void ChooseLevelUpBonus(LevelUpBonusType type)
    {
        if (!this.IsLocallyControlled())
            return;
        ChooseLevelUpBonusLocal(type);
    }

    // Called by LevelUpManager (server) on a player that didn't pick before the timer ran out.
    public void ForceLevelUpBonus(LevelUpBonusType type)
    {
        if (!NetworkObject.IsSpawned)
        {
            ChooseLevelUpBonusLocal(type);
            return;
        }
        ForceLevelUpBonusClientRpc(type, OwnerOnlyParams());
    }

    // Called by LevelUpManager (server) when a new level-up sequence starts.
    public void ResetLevelUpChoice()
    {
        if (!NetworkObject.IsSpawned)
        {
            ResetLevelUpChoiceLocal();
            return;
        }
        ResetLevelUpChoiceClientRpc(OwnerOnlyParams());
    }
}
