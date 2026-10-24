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

    public static int GetUpgradeLevel(int index)
    {
        int[] levels = Data.upgrades;
        return index >= 0 && index < levels.Length ? levels[index] : 0;
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
    private static ProgressData Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonUtility.FromJson<ProgressData>(File.ReadAllText(FilePath)) ?? new ProgressData();

            ProgressData fresh = new ProgressData();
            if (File.Exists(LegacyGoldPath))
            {
                LegacyGold legacy = JsonUtility.FromJson<LegacyGold>(File.ReadAllText(LegacyGoldPath));
                fresh.gold = legacy != null ? Mathf.Max(0, legacy.gold) : 0;
            }
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
}
