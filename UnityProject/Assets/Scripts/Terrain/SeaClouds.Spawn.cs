using Unity.Netcode;
using UnityEngine;

// Cloud spawning: one renderer per cloud, start point and speed from the seed.
public partial class SeaClouds
{
    private void AllocateArrays()
    {
        clouds = new Transform[cloudCount];
        renderers = new SpriteRenderer[cloudCount];
        startOffsets = new Vector2[cloudCount];
        speeds = new float[cloudCount];
        alphas = new float[cloudCount];
        wrapKeys = new Vector2Int[cloudCount];
    }

    private SpriteRenderer AddCloudRenderer(GameObject cloud)
    {
        SpriteRenderer renderer = cloud.AddComponent<SpriteRenderer>();

        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;
        if (cloudMaterial != null)
            renderer.sharedMaterial = cloudMaterial;
        return renderer;
    }

    private static void AddRevealOccluder(GameObject cloud)
    {
        RevealOccluder occluder = cloud.AddComponent<RevealOccluder>();

        occluder.AlwaysInFront = true;
        occluder.Moves = true;
    }

    private void CreateCloud(int i, System.Random rng)
    {
        GameObject cloud = new GameObject("Cloud");

        cloud.transform.SetParent(transform, false);
        clouds[i] = cloud.transform;
        renderers[i] = AddCloudRenderer(cloud);
        startOffsets[i] = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
        speeds[i] = Mathf.Lerp(speedRange.x, speedRange.y, (float)rng.NextDouble());
        wrapKeys[i] = new Vector2Int(int.MinValue, int.MinValue);
        if (revealPlayer)
            AddRevealOccluder(cloud);
    }

    // Each cloud gets its start point and speed from the seed, so they match across peers.
    private void Spawn(int seed)
    {
        System.Random rng = new System.Random(seed);

        cloudSeed = seed;
        AllocateArrays();
        for (int i = 0; i < cloudCount; i++)
            CreateCloud(i, rng);
        spawned = true;
    }

    // The map scene waits for its generator's seed; other scenes use the fallback seed.
    private int? GetSeed()
    {
        if (waitForMapSeed)
            return ProceduralMapGenerator.RuntimeSeed;
        return fallbackSeed;
    }
}
