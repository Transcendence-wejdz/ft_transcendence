using Unity.Netcode;
using UnityEngine;

// Rolls one random map seed per scene load on the host and replicates it, so every peer generates the same map.
public class MapSeedSync : NetworkBehaviour
{
    public static int? Current { get; private set; }

    private readonly NetworkVariable<int> seed = new OfflineSafeNetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // The seed also picks the biome, so a chosen biome reaches every peer through it.
    private static int RollSeed()
    {
        int rolled = Random.Range(int.MinValue, int.MaxValue);
        ProceduralMapGenerator generator = FindAnyObjectByType<ProceduralMapGenerator>(FindObjectsInactive.Include);

        if (generator == null || !RunOptions.Current.HasBiome)
            return rolled;
        return generator.SeedForBiome(rolled, RunOptions.Current.BiomeName);
    }

    // Offline solo has no spawn, so it rolls its own seed right away.
    void Start()
    {
        NetworkManager network = NetworkManager.Singleton;

        if (!IsSpawned && (network == null || !network.IsListening))
            Current = RollSeed();
    }

    // The spawn payload already carries the host's value, so clients read it here before any chunk is generated.
    public override void OnNetworkSpawn()
    {
        if (IsServer)
            seed.Value = RollSeed();
        Current = seed.Value;
    }

    public override void OnDestroy()
    {
        Current = null;
        base.OnDestroy();
    }
}
