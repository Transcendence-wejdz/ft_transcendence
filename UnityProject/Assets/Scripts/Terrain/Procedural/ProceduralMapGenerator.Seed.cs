using UnityEngine;
using UnityEngine.Tilemaps;

// Map seed: fixed, shared through MapSeedSync, or carried over from the previous map.
public partial class ProceduralMapGenerator
{
    // A Random seed still waiting for MapSeedSync's replicated value.
    private bool IsWaitingForSharedSeed()
    {
        if (initialized || !Application.isPlaying)
            return false;
        return seedSource == SeedSource.Random && !MapSeedSync.Current.HasValue;
    }

    // A Random seed waits for MapSeedSync to spawn; a scene without one rolls its seed locally.
    private bool TryInitialize()
    {
        if (IsWaitingForSharedSeed())
        {
            if (FindAnyObjectByType<MapSeedSync>(FindObjectsInactive.Include) != null)
                return false;
            Debug.LogWarning($"{name}: Seed Source is Random but the scene has no MapSeedSync, rolling a local seed (peers will not share the map).", this);
            localRandomSeed = Random.Range(int.MinValue, int.MaxValue);
        }
        if (!initialized)
            Initialize();
        return true;
    }

    private int ResolveRuntimeSeed()
    {
        if (seedSource != SeedSource.Random)
            return seed;
        if (MapSeedSync.Current.HasValue)
            return MapSeedSync.Current.Value;
        return localRandomSeed ?? seed;
    }

    // Every peer ran the same previous scene, so a Previous seed matches without network sync; the editor preview always uses the fixed seed.
    private int ResolveSeed()
    {
        int resolved;

        if (!Application.isPlaying)
            return seed;
        if (seedSource == SeedSource.Previous)
            return previousSeed ?? seed;
        resolved = ResolveRuntimeSeed();
        previousSeed = resolved;
        return resolved;
    }
}
