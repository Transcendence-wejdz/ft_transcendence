using System.Collections.Generic;
using UnityEngine;

// Seeded decor placement per chunk, pooled per prefab and grouped under one parent per category.
public partial class ProceduralMapGenerator
{
    private const string DecorRootName = "Decor";
    private const int DecorSeedSalt = -1640531527;
    private const int SortingOffsetSpread = 5;

    private readonly Dictionary<Vector2Int, List<(GameObject instance, GameObject prefab)>> decorObjects = new Dictionary<Vector2Int, List<(GameObject instance, GameObject prefab)>>();
    private readonly Dictionary<GameObject, Stack<GameObject>> decorPool = new Dictionary<GameObject, Stack<GameObject>>();
    private readonly Dictionary<string, Transform> decorCategoryParents = new Dictionary<string, Transform>();
    private Transform decorParent;
    private DecorEntry[] decorEntries;
    private Vector2[] decorOffsets;

    // Uses the seed's biome decor, with one noise offset per entry so every client clusters it identically.
    private void InitDecorOffsets()
    {
        int count;

        decorEntries = activeBiome?.decor ?? System.Array.Empty<DecorEntry>();
        count = decorEntries.Length;
        decorOffsets = new Vector2[count];
        for (int i = 0; i < count; i++)
            decorOffsets[i] = NextOffset(new System.Random(activeSeed ^ (i * DecorSeedSalt)));
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

    private bool IsInNoiseRange(DecorEntry entry, float noise)
    {
        if (noise <= entry.noiseRange.x)
            return false;
        return entry.noiseRange.y >= 1f || noise <= entry.noiseRange.y;
    }

    // True when the entry grows on this surface, inside its noise band and density roll.
    private bool EntryMatches(int i, int worldX, int worldY, bool isWater)
    {
        DecorEntry entry = decorEntries[i];

        if (entry == null || entry.prefab == null)
            return false;
        if ((entry.surface == DecorSurface.Water) != isWater)
            return false;
        if (!IsInNoiseRange(entry, SampleNoise(worldX, worldY, decorOffsets[i], entry.noiseScale)))
            return false;
        return DecorRoll(worldX, worldY, i) < entry.density;
    }

    // First entry matching the cell's surface and noise/density roll.
    private GameObject PickDecorPrefab(int worldX, int worldY, bool isWater)
    {
        for (int i = 0; i < decorEntries.Length; i++)
        {
            if (EntryMatches(i, worldX, worldY, isWater))
                return decorEntries[i].prefab;
        }
        return null;
    }

    private bool CanHoldDecor(int worldX, int worldY)
    {
        if (IsInSpawnSafeZone(worldX, worldY))
            return false;
        return !IsBeyondIslandCoast(worldX, worldY, Island.decorCoastOffset);
    }

    // Places one decor instance; neighbors get a small sorting tiebreak so overlapping pieces don't flicker.
    private GameObject PlaceDecor(GameObject prefab, int worldX, int worldY)
    {
        GameObject instance = RentDecorInstance(prefab);
        Vector3 position = landTilemap.GetCellCenterWorld(new Vector3Int(worldX, worldY, 0));
        SortingLayer_Auto sortScript;

        instance.transform.SetPositionAndRotation(position, Quaternion.identity);
        instance.SetActive(true);
        if (instance.TryGetComponent(out sortScript))
            sortScript.SetSortingOffset(((worldX % SortingOffsetSpread) + SortingOffsetSpread) % SortingOffsetSpread - 2);
        return instance;
    }

    private void GenerateDecor(Vector2Int chunk, int originX, int originY)
    {
        List<(GameObject instance, GameObject prefab)> spawned = null;
        int worldX;
        int worldY;
        GameObject prefab;

        if (decorOffsets.Length == 0)
            return;
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                worldX = originX + x;
                worldY = originY + y;
                if (!CanHoldDecor(worldX, worldY))
                    continue;
                prefab = PickDecorPrefab(worldX, worldY, MaskIsWater(worldX, worldY));
                if (prefab == null)
                    continue;
                spawned ??= new List<(GameObject instance, GameObject prefab)>();
                spawned.Add((PlaceDecor(prefab, worldX, worldY), prefab));
            }
        }
        if (spawned != null)
            decorObjects[chunk] = spawned;
    }
}
