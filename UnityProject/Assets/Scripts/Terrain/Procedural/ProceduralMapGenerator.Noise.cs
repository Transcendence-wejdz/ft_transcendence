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

        return SampleNoise(worldX, worldY, waterOffset, waterNoiseScale) < waterThreshold;
    }

    // Radius wobbles with world-space noise, so the coastline is a rough circle that stays seamless across chunks.
    private float CoastRadiusAt(int worldX, int worldY)
    {
        float wobble = SampleNoise(worldX, worldY, islandOffset, islandEdgeNoiseScale) * 2f - 1f;
        return islandRadius * (1f + wobble * islandEdgeVariation);
    }

    private bool IsBeyondIslandCoast(int worldX, int worldY, float offset = 0f)
    {
        if (islandRadius <= 0f)
            return false;

        return Vector2.Distance(new Vector2(worldX, worldY), islandCenter) > CoastRadiusAt(worldX, worldY) + offset;
    }

    // Ground noise picks which of the seed's biome tiles covers a patch.
    private TileBase PickLandTile(int worldX, int worldY)
    {
        TileBase[] tiles = activeBiome?.groundTiles;
        if (tiles == null || tiles.Length == 0)
            return null;

        float noise = SampleNoise(worldX, worldY, groundOffset, groundNoiseScale);
        return tiles[Mathf.Clamp(Mathf.FloorToInt(noise * tiles.Length), 0, tiles.Length - 1)];
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
