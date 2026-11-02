using System;
using System.Text.RegularExpressions;
using UnityEngine;

// Roguelite meta upgrades bought with gold at the Lobby's upgrade NPC: each level is one free stack of that level-up bonus at the start of every run.
public static class PermanentUpgrades
{
    public const int MaxLevel = 5;
    public const int BaseCost = 50;

    [Serializable]
    private class PurchaseRequest
    {
        public string type;
    }

    private static readonly LevelUpBonusType[] AllTypes = (LevelUpBonusType[])Enum.GetValues(typeof(LevelUpBonusType));
    private static readonly int[] serverLevels = new int[AllTypes.Length];
    private static bool purchasePending;

    public static event Action OnChanged;

    public static int GetLevel(LevelUpBonusType type)
    {
        int index = (int)type;

        if (GoldWallet.UsesServer)
            return serverLevels[index];
        return Mathf.Clamp(LocalProgressStore.GetUpgradeLevel(index), 0, MaxLevel);
    }

    // Must match upgradeCost in the web app's upgrades.ts.
    public static int CostFor(int level)
    {
        return BaseCost << level;
    }

    public static int NextCost(LevelUpBonusType type)
    {
        return CostFor(GetLevel(type));
    }

    public static bool CanBuy(LevelUpBonusType type)
    {
        if (purchasePending || !GoldWallet.CanEarn)
            return false;
        return GetLevel(type) < MaxLevel && GoldWallet.Balance >= NextCost(type);
    }

    private static void BuyLocal(LevelUpBonusType type, int level, int cost, Action<bool> onDone)
    {
        bool paid = GoldWallet.TrySpendLocal(cost);

        if (paid)
            LocalProgressStore.SetUpgradeLevel((int)type, level + 1);
        OnChanged?.Invoke();
        onDone?.Invoke(paid);
    }

    private static void HandlePurchaseResponse(int status, LevelUpBonusType type, int level, int cost, Action<bool> onDone)
    {
        bool accepted = status >= 200 && status < 300;

        purchasePending = false;
        if (accepted)
        {
            serverLevels[(int)type] = level + 1;
            GoldWallet.ApplyServerSpend(cost);
        }
        OnChanged?.Invoke();
        onDone?.Invoke(accepted);
    }

    private static void BuyOnServer(LevelUpBonusType type, int level, int cost, Action<bool> onDone)
    {
        string body = JsonUtility.ToJson(new PurchaseRequest { type = type.ToString() });

        purchasePending = true;
        OnChanged?.Invoke();
        WebBackend.Post("/api/game/upgrade", body, status => HandlePurchaseResponse(status, type, level, cost, onDone));
    }

    // Desktop/Editor pays from the local save at once; the website asks the server, which charges the account itself.
    public static void Buy(LevelUpBonusType type, Action<bool> onDone = null)
    {
        int level;
        int cost;

        if (!CanBuy(type))
        {
            onDone?.Invoke(false);
            return;
        }
        level = GetLevel(type);
        cost = CostFor(level);
        if (GoldWallet.UsesServer)
            BuyOnServer(type, level, cost, onDone);
        else
            BuyLocal(type, level, cost, onDone);
    }

    private static int ParseServerLevel(string json, LevelUpBonusType type)
    {
        Match match;

        if (string.IsNullOrEmpty(json))
            return 0;
        match = Regex.Match(json, $"\"{type}\"\\s*:\\s*(\\d+)");
        if (!match.Success)
            return 0;
        return Mathf.Clamp(int.Parse(match.Groups[1].Value), 0, MaxLevel);
    }

    // The website sends the account's levels as a flat {"Damage":2,...} object, which JsonUtility can't read into a dictionary.
    public static void SetFromServer(string json)
    {
        if (!GoldWallet.UsesServer)
            return;
        foreach (LevelUpBonusType type in AllTypes)
            serverLevels[(int)type] = ParseServerLevel(json, type);
        OnChanged?.Invoke();
    }

    public static LevelUpBonusStacks StartingStacks()
    {
        LevelUpBonusStacks stacks = default;

        foreach (LevelUpBonusType type in AllTypes)
        {
            for (int i = GetLevel(type); i > 0; i--)
                stacks.Increment(type);
        }
        return stacks;
    }
}
