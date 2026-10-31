using System;
using System.IO;
using UnityEngine;

// Desktop/Editor save of meta progression (gold and permanent upgrade levels); the website keeps both on the account instead.
public static class LocalProgressStore
{
    [Serializable]
    private class ProgressData
    {
        public int gold;
        public int[] upgrades = Array.Empty<int>();
    }

    [Serializable]
    private class LegacyGold
    {
        public int gold;
    }

    private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "player_progress.json");
    private static readonly string LegacyGoldPath = Path.Combine(Application.persistentDataPath, "player_gold.json");

    private static ProgressData data;

    public static int Gold
    {
        get => Data.gold;
        set
        {
            Data.gold = Mathf.Max(0, value);
            Save();
        }
    }

    private static ProgressData Data
    {
        get
        {
            if (data == null)
                data = Load();
            return data;
        }
    }

    // Falls back to the older gold-only file so gold earned before upgrades existed is kept.
    private static int LoadLegacyGold()
    {
        LegacyGold legacy;

        if (!File.Exists(LegacyGoldPath))
            return 0;
        legacy = JsonUtility.FromJson<LegacyGold>(File.ReadAllText(LegacyGoldPath));
        if (legacy == null)
            return 0;
        return Mathf.Max(0, legacy.gold);
    }

    private static ProgressData Load()
    {
        ProgressData fresh;

        try
        {
            if (File.Exists(FilePath))
                return JsonUtility.FromJson<ProgressData>(File.ReadAllText(FilePath)) ?? new ProgressData();
            fresh = new ProgressData();
            fresh.gold = LoadLegacyGold();
            return fresh;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LocalProgressStore: failed to load {FilePath}, starting fresh. {e.Message}");
            return new ProgressData();
        }
    }

    private static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Data));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LocalProgressStore: failed to save {FilePath}. {e.Message}");
        }
    }

    public static int GetUpgradeLevel(int index)
    {
        int[] levels = Data.upgrades;

        if (index < 0 || index >= levels.Length)
            return 0;
        return levels[index];
    }

    public static void SetUpgradeLevel(int index, int level)
    {
        if (index < 0)
            return;
        if (Data.upgrades.Length <= index)
            Array.Resize(ref Data.upgrades, index + 1);
        Data.upgrades[index] = level;
        Save();
    }
}
