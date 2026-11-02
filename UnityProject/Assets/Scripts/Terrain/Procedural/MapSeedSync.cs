using Unity.Netcode;
using UnityEngine;

// Rolls one random map seed per scene load on the host and replicates it, so every peer generates the same map.
public class MapSeedSync : NetworkBehaviour
{
    public static int? Current { get; private set; }

    private readonly NetworkVariable<int> seed = new OfflineSafeNetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Offline solo has no spawn, so it rolls its own seed right away.
    void Start()
    {
        NetworkManager network = NetworkManager.Singleton;

        if (!IsSpawned && (network == null || !network.IsListening))
            Current = Random.Range(int.MinValue, int.MaxValue);
    }

    // The spawn payload already carries the host's value, so clients read it here before any chunk is generated.
    public override void OnNetworkSpawn()
    {
        if (IsServer)
            seed.Value = Random.Range(int.MinValue, int.MaxValue);
        Current = seed.Value;
    }

    public override void OnDestroy()
    {
        Current = null;
        base.OnDestroy();
    }
}
