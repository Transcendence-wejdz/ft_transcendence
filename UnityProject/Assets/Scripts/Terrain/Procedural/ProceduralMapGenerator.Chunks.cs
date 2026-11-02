using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Chunk streaming around the local player, or a one-shot load of the whole island.
public partial class ProceduralMapGenerator
{
    private const int NoCameraChunkMargin = 2;
    private const float MinCameraDepth = 0.01f;

    private readonly HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> pendingChunks = new HashSet<Vector2Int>();
    private readonly Queue<Vector2Int> pendingChunkQueue = new Queue<Vector2Int>();
    private readonly HashSet<Vector2Int> desiredChunks = new HashSet<Vector2Int>();
    private Vector2Int lastPlayerChunk;
    private bool hasGeneratedOnce;

    private bool LoadsWholeIsland => loadWholeIsland && Island.radius > 0f;

    // Fills one cell of each tile buffer: water or land, plus the shore background and foam on coastal ground.
    private void FillCell(int index, int worldX, int worldY)
    {
        bool isWater = MaskIsWater(worldX, worldY);
        bool isCoastalGround = !isWater && MaskIsAdjacentToWater(worldX, worldY);

        waterTiles[index] = null;
        landTiles[index] = null;
        waterBackgroundTiles[index] = null;
        coastFoamTiles[index] = null;
        if (isWater)
            waterTiles[index] = Water.tile;
        else
            landTiles[index] = PickLandTile(worldX, worldY);
        if (!isCoastalGround)
            return;
        waterBackgroundTiles[index] = Water.backgroundTile;
        coastFoamTiles[index] = Water.coastFoamTile;
    }

    private void SetChunkTiles(BoundsInt bounds, TileBase[] land, TileBase[] water, TileBase[] background, TileBase[] foam)
    {
        landTilemap.SetTilesBlock(bounds, land);
        waterTilemap.SetTilesBlock(bounds, water);
        waterBackgroundTilemap.SetTilesBlock(bounds, background);
        coastFoamTilemap.SetTilesBlock(bounds, foam);
    }

    private void GenerateChunk(Vector2Int chunk)
    {
        int originX = chunk.x * chunkSize;
        int originY = chunk.y * chunkSize;

        FillWaterMask(originX - 1, originY - 1);
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
                FillCell(x + y * chunkSize, originX + x, originY + y);
        }
        SetChunkTiles(ChunkBounds(chunk), landTiles, waterTiles, waterBackgroundTiles, coastFoamTiles);
        GenerateDecor(chunk, originX, originY);
    }

    private void UnloadChunk(Vector2Int chunk)
    {
        SetChunkTiles(ChunkBounds(chunk), clearTiles, clearTiles, clearTiles, clearTiles);
        UnloadDecor(chunk);
    }

    private bool UnloadIfUndesired(Vector2Int chunk)
    {
        if (desiredChunks.Contains(chunk))
            return false;
        UnloadChunk(chunk);
        return true;
    }

    private void CollectDesiredChunks(Vector2Int centerChunk, int radius)
    {
        desiredChunks.Clear();
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
                desiredChunks.Add(centerChunk + new Vector2Int(x, y));
        }
    }

    private void UpdateChunks(Vector2Int centerChunk)
    {
        CollectDesiredChunks(centerChunk, ComputeEffectiveViewDistance());
        foreach (Vector2Int chunk in desiredChunks)
        {
            if (!loadedChunks.Contains(chunk) && pendingChunks.Add(chunk))
                pendingChunkQueue.Enqueue(chunk);
        }
        loadedChunks.RemoveWhere(UnloadIfUndesired);
    }

    // One-shot load of every chunk the island (and the ocean the camera can see past its coast) can ever need.
    private void GenerateWholeIsland()
    {
        int radius = IslandChunkRadius() + CameraChunkMargin();
        Vector2Int center = WorldToChunk(transform.position);
        Vector2Int chunk;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                chunk = center + new Vector2Int(x, y);
                if (loadedChunks.Add(chunk))
                    GenerateChunk(chunk);
            }
        }
    }

    // Generates up to budget queued chunks, dropping ones no longer desired.
    private void ProcessPendingChunks(int budget)
    {
        Vector2Int chunk;

        while (budget > 0 && pendingChunkQueue.Count > 0)
        {
            chunk = pendingChunkQueue.Dequeue();
            pendingChunks.Remove(chunk);
            if (!desiredChunks.Contains(chunk))
                continue;
            GenerateChunk(chunk);
            loadedChunks.Add(chunk);
            budget--;
        }
    }

    private void GenerateIslandOnce()
    {
        if (hasGeneratedOnce)
            return;
        hasGeneratedOnce = true;
        GenerateWholeIsland();
    }

    // The very first pass loads everything at once; later ones spread new chunks across frames.
    private void StreamAroundPlayer()
    {
        Vector2Int playerChunk = WorldToChunk(GetTrackedPlayer().position);
        bool firstRun = !hasGeneratedOnce;

        if (firstRun || playerChunk != lastPlayerChunk)
        {
            lastPlayerChunk = playerChunk;
            hasGeneratedOnce = true;
            UpdateChunks(playerChunk);
        }
        if (firstRun)
            ProcessPendingChunks(int.MaxValue);
        else
            ProcessPendingChunks(maxChunkGenerationsPerFrame);
    }

    void Update()
    {
        if (!TryInitialize())
            return;
        if (LoadsWholeIsland)
            GenerateIslandOnce();
        else
            StreamAroundPlayer();
    }
}
