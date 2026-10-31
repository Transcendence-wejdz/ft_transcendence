using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Chunk geometry: world to chunk, and how many chunks the camera view and the island need.
public partial class ProceduralMapGenerator
{
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

    private float CellWorldSize()
    {
        if (landTilemap.cellSize.x > 0f)
            return landTilemap.cellSize.x;
        return 1f;
    }

    // Chunk radius covering the camera view plus a buffer.
    private int ComputeEffectiveViewDistance()
    {
        Camera cam = Camera.main;
        float depth;
        Vector3 worldMin;
        Vector3 worldMax;
        float halfExtent;

        if (cam == null)
            return viewDistanceInChunks;
        depth = Vector3.Dot(landTilemap.transform.position - cam.transform.position, cam.transform.forward);
        if (depth <= MinCameraDepth)
            return viewDistanceInChunks;
        worldMin = cam.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        worldMax = cam.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
        halfExtent = Mathf.Max(Mathf.Abs(worldMax.x - worldMin.x), Mathf.Abs(worldMax.y - worldMin.y)) * 0.5f;
        return Mathf.CeilToInt(halfExtent / (chunkSize * CellWorldSize())) + viewDistanceInChunks;
    }

    // Chunks from the generator to the farthest possible coastline plus the decor coast offset.
    private int IslandChunkRadius()
    {
        IslandSettings island = Island;
        float reach = island.radius * (1f + island.edgeVariation) + Mathf.Max(0f, island.decorCoastOffset);

        return Mathf.CeilToInt(reach / chunkSize) + 1;
    }

    // Half the camera view in chunks, so the ocean never ends on screen while standing on the coast.
    private int CameraChunkMargin()
    {
        int withBuffer = ComputeEffectiveViewDistance();

        if (Camera.main == null)
            return NoCameraChunkMargin;
        return Mathf.Max(1, withBuffer - viewDistanceInChunks);
    }
}
