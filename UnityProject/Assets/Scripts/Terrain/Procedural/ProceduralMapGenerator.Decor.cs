using System.Collections.Generic;
using UnityEngine;

public partial class ProceduralMapGenerator
{
    private readonly Dictionary<Vector2Int, List<(GameObject instance, GameObject prefab)>> decorObjects = new();
    private readonly Dictionary<GameObject, Stack<GameObject>> decorPool = new();
    private readonly Dictionary<string, Transform> decorCategoryParents = new();
    private const string DecorRootName = "Decor";
    private Transform decorParent;
    private DecorEntry[] decorEntries;
    private Vector2[] decorOffsets;

    // Uses the seed's biome decor, with one noise offset per entry so every client clusters it identically.
    private void InitDecorOffsets()
    {
        decorEntries = activeBiome?.decor ?? System.Array.Empty<DecorEntry>();
        int count = decorEntries.Length;
        decorOffsets = new Vector2[count];
        for (int i = 0; i < count; i++)
            decorOffsets[i] = NextOffset(new System.Random(activeSeed ^ (i * -1640531527)));
    }

    private void GenerateDecor(Vector2Int chunk, int originX, int originY)
    {
        if (decorOffsets.Length == 0)
            return;

        List<(GameObject instance, GameObject prefab)> spawned = null;

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int worldX = originX + x;
                int worldY = originY + y;

                if (IsInSpawnSafeZone(worldX, worldY) || IsBeyondIslandCoast(worldX, worldY, Island.decorCoastOffset))
                    continue;

                GameObject prefab = PickDecorPrefab(worldX, worldY, MaskIsWater(worldX, worldY));
                if (prefab == null)
                    continue;

                GameObject instance = RentDecorInstance(prefab);
                instance.transform.SetPositionAndRotation(landTilemap.GetCellCenterWorld(new Vector3Int(worldX, worldY, 0)), Quaternion.identity);
                instance.SetActive(true);

                if (instance.TryGetComponent(out SortingLayer_Auto sortScript))
                    sortScript.SetSortingOffset(((worldX % 5) + 5) % 5 - 2);

                spawned ??= new List<(GameObject instance, GameObject prefab)>();
                spawned.Add((instance, prefab));
            }
        }

        if (spawned != null)
            decorObjects[chunk] = spawned;
    }

    // Pooled rather than destroyed, since chunks reload often.
    private void UnloadDecor(Vector2Int chunk)
    {
        if (!decorObjects.Remove(chunk, out List<(GameObject instance, GameObject prefab)> decorList))
            return;

        foreach ((GameObject instance, GameObject prefab) in decorList)
        {
            instance.SetActive(false);

            if (!decorPool.TryGetValue(prefab, out Stack<GameObject> pool))
            {
                pool = new Stack<GameObject>();
                decorPool[prefab] = pool;
            }

            pool.Push(instance);
        }
    }

    // Instances get their category parent once at creation and keep it.
    private GameObject RentDecorInstance(GameObject prefab)
    {
        if (decorPool.TryGetValue(prefab, out Stack<GameObject> pool) && pool.Count > 0)
            return pool.Pop();

        return MarkEditorOnly(Instantiate(prefab, GetDecorCategoryParent(prefab)));
    }

    // Edit-mode preview decor must never end up serialized into the scene or prefab.
    private static GameObject MarkEditorOnly(GameObject root)
    {
        if (!Application.isPlaying)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.DontSave;
        }

        return root;
    }

    // Category is the prefab name minus trailing digits (Tree1 -> Tree).
    private Transform GetDecorCategoryParent(GameObject prefab)
    {
        if (decorParent == null)
        {
            decorParent = MarkEditorOnly(new GameObject(DecorRootName)).transform;
            decorParent.SetParent(transform, false);
        }

        string category = System.Text.RegularExpressions.Regex.Replace(prefab.name, @"\d+$", "");
        if (!decorCategoryParents.TryGetValue(category, out Transform categoryParent))
        {
            categoryParent = MarkEditorOnly(new GameObject(category)).transform;
            categoryParent.SetParent(decorParent, false);
            decorCategoryParents[category] = categoryParent;
        }

        return categoryParent;
    }

    // Stable roll in [0, 1) per cell and entry, so tuning one entry never reshuffles the others' rolls.
    private float DecorRoll(int worldX, int worldY, int entryIndex)
    {
        unchecked
        {
            uint h = (uint)activeSeed;
            h ^= (uint)worldX * 0x8DA6B343u;
            h ^= (uint)worldY * 0xD8163841u;
            h ^= (uint)entryIndex * 0xCB1AB31Fu;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return (h & 0xFFFFFFu) / 16777216f;
        }
    }

    // First entry matching the cell's surface and noise/density roll.
    private GameObject PickDecorPrefab(int worldX, int worldY, bool isWater)
    {
        for (int i = 0; i < decorEntries.Length; i++)
        {
            DecorEntry entry = decorEntries[i];
            if (entry == null || entry.prefab == null)
                continue;

            if ((entry.surface == DecorSurface.Water) != isWater)
                continue;

            float noise = SampleNoise(worldX, worldY, decorOffsets[i], entry.noiseScale);
            if (noise <= entry.noiseRange.x || (entry.noiseRange.y < 1f && noise > entry.noiseRange.y))
                continue;

            if (DecorRoll(worldX, worldY, i) >= entry.density)
                continue;

            return entry.prefab;
        }

        return null;
    }
}
