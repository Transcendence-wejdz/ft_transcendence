using UnityEngine;

// Which enemies a wave can roll: difficulty rises by swapping weak tiers for stronger ones, never by raising their stats.
public partial class WaveSpawner
{
    [Tooltip("Wave from which each tier stops spawning (first entry = tier 1). 0 = never.")]
    [SerializeField] private int[] tierRetireWave = { 13, 20, 0, 0 };

    private int UnlockWaveForTier(int tier)
    {
        if (tier > tierUnlockWave.Length)
            return int.MaxValue;
        return tierUnlockWave[tier - 1];
    }

    private int RetireWaveForTier(int tier)
    {
        if (tier > tierRetireWave.Length || tierRetireWave[tier - 1] <= 0)
            return int.MaxValue;
        return tierRetireWave[tier - 1];
    }

    private bool IsTierActive(int tier)
    {
        return waveNumber >= UnlockWaveForTier(tier) && waveNumber < RetireWaveForTier(tier);
    }

    // Falls back to kit 0 while no tier is active.
    private void RefreshUnlockedKitIndices()
    {
        unlockedKitIndices.Clear();
        for (int i = 0; i < enemyKits.Length; i++)
        {
            if (IsTierActive(Mathf.Max(1, enemyKits[i].Tier)))
                unlockedKitIndices.Add(i);
        }
        if (unlockedKitIndices.Count == 0)
            unlockedKitIndices.Add(0);
    }
}
