using Unity.Netcode;

public enum RunBiome : byte { Random, Forest, Plains, Mountain }

// Options picked in the mode select, the same on every peer for the whole run.
public struct RunOptions : INetworkSerializeByMemcpy
{
    public const float NoFriendlyFireScoreMultiplier = 0.75f;

    public RunBiome biome;
    public bool friendlyFire;

    public static RunOptions Default => new RunOptions { biome = RunBiome.Random, friendlyFire = true };

    public static RunOptions Current { get; set; } = Default;

    public bool HasBiome => biome != RunBiome.Random;

    public string BiomeName => biome.ToString();

    // Turning friendly fire off makes a party's run easier, which means nothing for a player alone.
    public float ScoreMultiplier()
    {
        if (friendlyFire || Player.AllActiveInstances.Count <= 1)
            return 1f;
        return NoFriendlyFireScoreMultiplier;
    }
}
