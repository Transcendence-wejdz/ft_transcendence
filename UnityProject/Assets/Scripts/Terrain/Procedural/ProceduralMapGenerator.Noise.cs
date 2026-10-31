using UnityEngine;
using UnityEngine.Tilemaps;

// Seeded noise behind water, island coast and ground patches, plus the per-chunk water mask.
public partial class ProceduralMapGenerator
{
    private const float GroundOctaveOffsetX = 1013f;
    private const float GroundOctaveOffsetY = 2027f;

    private Vector2 waterOffset;
    private Vector2 islandOffset;
    private Vector2 groundOffset;
    private Vector2 islandCenter;
    private Biome activeBiome;

    private static Vector2 NextOffset(System.Random rng)
    {
        return new Vector2(rng.Next(-100000, 100000), rng.Next(-100000, 100000));
    }

    private static float SampleNoise(int worldX, int worldY, Vector2 offset, float scale)
    {
        return Mathf.PerlinNoise((worldX + offset.x) * scale, (worldY + offset.y) * scale);
    }

    // False until the seed is known in Play mode, so spawn checks must wait for it.
    public bool IsReady => !Application.isPlaying || initialized;

    // Lets other systems avoid water without duplicating the noise.
    public bool IsWaterAtWorldPosition(Vector3 worldPosition)
    {
        Vector3Int cell;

        if (Application.isPlaying && !initialized)
            return false;
        cell = landTilemap.WorldToCell(worldPosition);
        return IsWater(cell.x, cell.y);
    }

    private bool IsInSpawnSafeZone(int worldX, int worldY)
    {
        if (activeBiome == null)
            return false;
        return Vector2Int.Distance(new Vector2Int(worldX, worldY), spawnCell) <= activeBiome.spawnSafeRadius;
    }

    private bool IsWater(int worldX, int worldY)
    {
        if (IsInSpawnSafeZone(worldX, worldY))
            return false;
        if (IsBeyondIslandCoast(worldX, worldY))
            return true;
        return SampleNoise(worldX, worldY, waterOffset, Water.noiseScale) < Water.threshold;
    }

    // Radius wobbles with world-space noise, so the coastline is a rough circle that stays seamless across chunks.
    private float CoastRadiusAt(int worldX, int worldY)
    {
        IslandSettings island = Island;
        float wobble = SampleNoise(worldX, worldY, islandOffset, island.edgeNoiseScale) * 2f - 1f;

        return island.radius * (1f + wobble * island.edgeVariation);
    }

    private bool IsBeyondIslandCoast(int worldX, int worldY, float offset = 0f)
    {
        if (Island.radius <= 0f)
            return false;
        return Vector2.Distance(new Vector2(worldX, worldY), islandCenter) > CoastRadiusAt(worldX, worldY) + offset;
    }

    // Ground noise picks which of the seed's biome tiles covers a patch, each tile owning a weighted slice of the noise range.
    private TileBase PickLandTile(int worldX, int worldY)
    {
        GroundEntry[] grounds = activeBiome?.grounds;
        float noise;
        float total;

        if (grounds == null || grounds.Length == 0)
            return null;
        noise = SampleGroundNoise(worldX, worldY);
        total = TotalGroundWeight(grounds);
        if (total <= 0f)
            return grounds[Mathf.Clamp(Mathf.FloorToInt(noise * grounds.Length), 0, grounds.Length - 1)]?.tile;
        return PickWeightedGround(grounds, noise * total);
    }

    private static float TotalGroundWeight(GroundEntry[] grounds)
    {
        float total = 0f;

        foreach (GroundEntry ground in grounds)
            total += GroundWeight(ground);
        return total;
    }

    // Walks the weighted slices until the threshold falls inside one.
    private static TileBase PickWeightedGround(GroundEntry[] grounds, float threshold)
    {
        foreach (GroundEntry ground in grounds)
        {
            threshold -= GroundWeight(ground);
            if (threshold < 0f)
                return ground.tile;
        }
        return grounds[grounds.Length - 1]?.tile;
    }

    private static float GroundWeight(GroundEntry ground)
    {
        if (ground == null)
            return 0f;
        return Mathf.Max(0f, ground.weight);
    }

    // Normalized fractal noise (octaves halve in size), then stretched around 0.5 by the contrast.
    private float SampleGroundNoise(int worldX, int worldY)
    {
        float sum = 0f;
        float amplitude = 1f;
        float totalAmplitude = 0f;
        float scale = groundNoiseScale;
        float noise = 0.5f;
        Vector2 octaveOffset;

        for (int octave = 0; octave < Mathf.Max(1, groundNoiseOctaves); octave++)
        {
            octaveOffset = new Vector2(octave * GroundOctaveOffsetX, octave * GroundOctaveOffsetY);
            sum += SampleNoise(worldX, worldY, groundOffset + octaveOffset, scale) * amplitude;
            totalAmplitude += amplitude;
            amplitude *= groundNoisePersistence;
            scale *= 2f;
        }
        if (totalAmplitude > 0f)
            noise = sum / totalAmplitude;
        return Mathf.Clamp01((noise - 0.5f) * groundNoiseContrast + 0.5f);
    }

    // Caches the chunk plus a 1-cell border so coast checks never resample noise.
    private void FillWaterMask(int originX, int originY)
    {
        int size = waterMask.GetLength(0);

        maskOriginX = originX;
        maskOriginY = originY;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                waterMask[x, y] = IsWater(originX + x, originY + y);
        }
    }

    private bool MaskIsWater(int worldX, int worldY)
    {
        return waterMask[worldX - maskOriginX, worldY - maskOriginY];
    }

    private bool MaskIsAdjacentToWater(int worldX, int worldY)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if ((x != 0 || y != 0) && MaskIsWater(worldX + x, worldY + y))
                    return true;
            }
        }
        return false;
    }
}
