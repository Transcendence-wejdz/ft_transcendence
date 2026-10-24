using UnityEngine;
using UnityEngine.Tilemaps;

public partial class ProceduralMapGenerator
{
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

    // Lets other systems avoid water without duplicating the noise.
    public bool IsWaterAtWorldPosition(Vector3 worldPosition)
    {
        if (Application.isPlaying && !initialized)
            return false;
        Vector3Int cell = landTilemap.WorldToCell(worldPosition);
        return IsWater(cell.x, cell.y);
    }

    private bool IsInSpawnSafeZone(int worldX, int worldY)
    {
        return activeBiome != null && Vector2Int.Distance(new Vector2Int(worldX, worldY), spawnCell) <= activeBiome.spawnSafeRadius;
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
        if (grounds == null || grounds.Length == 0)
            return null;

        float noise = SampleGroundNoise(worldX, worldY);
        float total = 0f;
        foreach (GroundEntry ground in grounds)
            total += GroundWeight(ground);
        if (total <= 0f)
            return grounds[Mathf.Clamp(Mathf.FloorToInt(noise * grounds.Length), 0, grounds.Length - 1)]?.tile;

        float threshold = noise * total;
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
        return ground != null ? Mathf.Max(0f, ground.weight) : 0f;
    }

    // Normalized fractal noise (octaves halve in size), then stretched around 0.5 by the contrast.
    private float SampleGroundNoise(int worldX, int worldY)
    {
        float sum = 0f;
        float amplitude = 1f;
        float totalAmplitude = 0f;
        float scale = groundNoiseScale;
        for (int octave = 0; octave < Mathf.Max(1, groundNoiseOctaves); octave++)
        {
            sum += SampleNoise(worldX, worldY, groundOffset + new Vector2(octave * 1013f, octave * 2027f), scale) * amplitude;
            totalAmplitude += amplitude;
            amplitude *= groundNoisePersistence;
            scale *= 2f;
        }

        float noise = totalAmplitude > 0f ? sum / totalAmplitude : 0.5f;
        return Mathf.Clamp01((noise - 0.5f) * groundNoiseContrast + 0.5f);
    }

    // Caches the chunk plus a 1-cell border so coast checks never resample noise.
    private void FillWaterMask(int originX, int originY)
    {
        maskOriginX = originX;
        maskOriginY = originY;
        int size = waterMask.GetLength(0);

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
