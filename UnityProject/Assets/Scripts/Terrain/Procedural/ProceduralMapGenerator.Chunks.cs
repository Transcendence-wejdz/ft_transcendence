using System.Collections.Generic;
using UnityEngine;

// Chunk streaming around the local player, or a one-shot load of the whole island.
public partial class ProceduralMapGenerator
{
    private readonly HashSet<Vector2Int> loadedChunks = new();
    private readonly HashSet<Vector2Int> pendingChunks = new();
    private readonly Queue<Vector2Int> pendingChunkQueue = new();
    private readonly HashSet<Vector2Int> desiredChunks = new();
    private Vector2Int lastPlayerChunk;
    private bool hasGeneratedOnce;

    void Update()
    {
        if (!TryInitialize())
            return;

        if (loadWholeIsland && Island.radius > 0f)
        {
            if (!hasGeneratedOnce)
            {
                hasGeneratedOnce = true;
                GenerateWholeIsland();
            }
            return;
        }

        Vector2Int playerChunk = WorldToChunk(GetTrackedPlayer().position);
        bool firstRun = !hasGeneratedOnce;

        if (firstRun || playerChunk != lastPlayerChunk)
        {
            lastPlayerChunk = playerChunk;
            hasGeneratedOnce = true;
            UpdateChunks(playerChunk);
        }

        ProcessPendingChunks(firstRun ? int.MaxValue : maxChunkGenerationsPerFrame);
    }

    private void UpdateChunks(Vector2Int centerChunk)
    {
        int radius = ComputeEffectiveViewDistance();

        desiredChunks.Clear();
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
                desiredChunks.Add(centerChunk + new Vector2Int(x, y));
        }

        foreach (Vector2Int chunk in desiredChunks)
        {
            if (!loadedChunks.Contains(chunk) && pendingChunks.Add(chunk))
                pendingChunkQueue.Enqueue(chunk);
        }

        loadedChunks.RemoveWhere(chunk =>
        {
            if (desiredChunks.Contains(chunk))
                return false;

            UnloadChunk(chunk);
            return true;
        });
    }

    // One-shot load of every chunk the island (and the ocean the camera can see past its coast) can ever need.
    private void GenerateWholeIsland()
    {
        int radius = IslandChunkRadius() + CameraChunkMargin();
        Vector2Int center = WorldToChunk(transform.position);
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                Vector2Int chunk = center + new Vector2Int(x, y);
                if (loadedChunks.Add(chunk))
                    GenerateChunk(chunk);
            }
        }
    }

    // Chunks from the generator to the farthest possible coastline plus the decor coast offset.
    private int IslandChunkRadius()
    {
        IslandSettings island = Island;
        return Mathf.CeilToInt((island.radius * (1f + island.edgeVariation) + Mathf.Max(0f, island.decorCoastOffset)) / chunkSize) + 1;
    }

    // Half the camera view in chunks, so the ocean never ends on screen while standing on the coast.
    private int CameraChunkMargin()
    {
        int withBuffer = ComputeEffectiveViewDistance();
        return Camera.main != null ? Mathf.Max(1, withBuffer - viewDistanceInChunks) : 2;
    }

    // Generates up to budget queued chunks, dropping ones no longer desired.
    private void ProcessPendingChunks(int budget)
    {
        while (budget > 0 && pendingChunkQueue.Count > 0)
        {
            Vector2Int chunk = pendingChunkQueue.Dequeue();
            pendingChunks.Remove(chunk);

            if (!desiredChunks.Contains(chunk))
                continue;

            GenerateChunk(chunk);
            loadedChunks.Add(chunk);
            budget--;
        }
    }

    private void GenerateChunk(Vector2Int chunk)
    {
        int originX = chunk.x * chunkSize;
        int originY = chunk.y * chunkSize;
        FillWaterMask(originX - 1, originY - 1);

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int worldX = originX + x;
                int worldY = originY + y;
                int index = x + y * chunkSize;
                bool isWater = MaskIsWater(worldX, worldY);
                bool isCoastalGround = !isWater && MaskIsAdjacentToWater(worldX, worldY);

                waterTiles[index] = isWater ? Water.tile : null;
                landTiles[index] = isWater ? null : PickLandTile(worldX, worldY);
                waterBackgroundTiles[index] = isCoastalGround ? Water.backgroundTile : null;
                coastFoamTiles[index] = isCoastalGround ? Water.coastFoamTile : null;
            }
        }

        BoundsInt bounds = ChunkBounds(chunk);
        landTilemap.SetTilesBlock(bounds, landTiles);
        waterTilemap.SetTilesBlock(bounds, waterTiles);
        waterBackgroundTilemap.SetTilesBlock(bounds, waterBackgroundTiles);
        coastFoamTilemap.SetTilesBlock(bounds, coastFoamTiles);

        GenerateDecor(chunk, originX, originY);
    }

    private void UnloadChunk(Vector2Int chunk)
    {
        BoundsInt bounds = ChunkBounds(chunk);
        landTilemap.SetTilesBlock(bounds, clearTiles);
        waterTilemap.SetTilesBlock(bounds, clearTiles);
        waterBackgroundTilemap.SetTilesBlock(bounds, clearTiles);
        coastFoamTilemap.SetTilesBlock(bounds, clearTiles);

        UnloadDecor(chunk);
    }

    private BoundsInt ChunkBounds(Vector2Int chunk)
    {
        return new BoundsInt(chunk.x * chunkSize, chunk.y * chunkSize, 0, chunkSize, chunkSize, 1);
    }

    private Vector2Int WorldToChunk(Vector3 worldPosition)
    {
        Vector3Int cell = landTilemap.WorldToCell(worldPosition);
        return new Vector2Int(
            Mathf.FloorToInt(cell.x / (float)chunkSize),
            Mathf.FloorToInt(cell.y / (float)chunkSize));
    }

    // Chunk radius covering the camera view plus a buffer.
    private int ComputeEffectiveViewDistance()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return viewDistanceInChunks;

        Vector3 toPlane = landTilemap.transform.position - cam.transform.position;
        float depth = Vector3.Dot(toPlane, cam.transform.forward);
        if (depth <= 0.01f)
            return viewDistanceInChunks;

        Vector3 worldMin = cam.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 worldMax = cam.ViewportToWorldPoint(new Vector3(1f, 1f, depth));

        float cellSize = landTilemap.cellSize.x > 0f ? landTilemap.cellSize.x : 1f;
        float chunkWorldSize = chunkSize * cellSize;
        float halfExtent = Mathf.Max(Mathf.Abs(worldMax.x - worldMin.x), Mathf.Abs(worldMax.y - worldMin.y)) * 0.5f;

        return Mathf.CeilToInt(halfExtent / chunkWorldSize) + viewDistanceInChunks;
    }
}
