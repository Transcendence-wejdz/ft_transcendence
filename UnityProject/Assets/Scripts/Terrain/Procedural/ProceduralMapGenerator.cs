using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class ProceduralMapGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap landTilemap;
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private Tilemap waterBackgroundTilemap;
    [SerializeField] private Tilemap coastFoamTilemap;
    [SerializeField] private Transform player;

    [Header("Generation")]
    [Tooltip("Drives every noise offset and picks the biome (seed modulo the biome count).")]
    [SerializeField] private int seed;

    [Header("Chunk Streaming")]
    [SerializeField] private int chunkSize = 16;
    [Tooltip("Extra buffer chunks loaded beyond the camera's current view, to avoid pop-in at screen edges.")]
    [SerializeField] private int viewDistanceInChunks = 1;
    [Tooltip("Max newly-needed chunks generated per frame after the initial load, to spread bursts across frames.")]
    [SerializeField] private int maxChunkGenerationsPerFrame = 2;

    [Header("Biomes")]
    [Tooltip("Perlin scale of the ground tile patches inside the biome.")]
    [SerializeField] private float groundNoiseScale = 0.1f;
    [Tooltip("One biome per map, picked as seed modulo the biome count.")]
    [SerializeField] private Biome[] biomes;

    [Header("Water")]
    [SerializeField] private float waterNoiseScale = 0.1f;
    [SerializeField] private float waterThreshold = 0.35f;
    [SerializeField] private TileBase waterTile;
    [SerializeField] private TileBase waterBackgroundTile;
    [SerializeField] private TileBase coastFoamTile;

    [Header("Island")]
    [Tooltip("Radius in cells around the generator beyond which everything is water. 0 = endless land.")]
    [SerializeField] private float islandRadius;
    [Range(0f, 0.5f)]
    [Tooltip("How far the coastline wanders from a perfect circle, as a fraction of the radius.")]
    [SerializeField] private float islandEdgeVariation = 0.15f;
    [Tooltip("Perlin scale of the coastline wobble. Smaller = fewer, wider bays and capes.")]
    [SerializeField] private float islandEdgeNoiseScale = 0.05f;
    [Tooltip("Decor only spawns up to this many cells past the coastline (negative keeps it inland). Ignored without an island.")]
    [SerializeField] private float decorCoastOffset = 3f;

    [Header("Editor Preview")]
    [Tooltip("Keeps a generated preview of the map in the Scene view, refreshed on every inspector change.")]
    [SerializeField] private bool previewInEditor;
    [Tooltip("Chunk radius previewed around the generator when there is no island.")]
    [SerializeField] private int previewRadiusInChunks = 4;

    [System.Serializable]
    private class Biome
    {
        public string name;
        [Tooltip("Ground tiles mixed inside this biome, picked by the ground noise.")]
        public TileBase[] groundTiles;
        [Tooltip("Radius in cells around the player spawn kept as dry land with no decor.")]
        public float spawnSafeRadius = 5f;
        [Tooltip("Decor spawned on this biome. Earlier entries win when several match a cell.")]
        public DecorEntry[] decor;
    }

    private enum DecorSurface { Land, Water }

    [System.Serializable]
    private class DecorEntry
    {
        public GameObject prefab;
        [Tooltip("Land: only on dry tiles. Water: only on water tiles.")]
        public DecorSurface surface = DecorSurface.Land;
        [Range(0f, 1f)]
        [Tooltip("Chance applied to cells that pass the noise threshold - main density knob.")]
        public float density = 0.05f;
        [Tooltip("Perlin noise scale for clustering. Smaller = larger, smoother clusters.")]
        public float noiseScale = 0.2f;
        [Range(0f, 1f)]
        [Tooltip("Clustering noise cutoff - higher = sparser/rarer patches.")]
        public float noiseThreshold = 0.75f;
    }

    private readonly HashSet<Vector2Int> loadedChunks = new();
    private readonly HashSet<Vector2Int> pendingChunks = new();
    private readonly Queue<Vector2Int> pendingChunkQueue = new();
    private readonly HashSet<Vector2Int> desiredChunks = new();
    private Vector2Int lastPlayerChunk;
    private bool hasGeneratedOnce;
    private Vector2Int spawnCell;

    private bool[,] waterMask;
    private int maskOriginX;
    private int maskOriginY;
    private TileBase[] landTiles;
    private TileBase[] waterTiles;
    private TileBase[] waterBackgroundTiles;
    private TileBase[] coastFoamTiles;
    private TileBase[] clearTiles;

    void Awake()
    {
        Initialize();
    }

    // Seeds noise offsets and allocates per-chunk buffers, shared by runtime and the editor preview.
    private void Initialize()
    {
        System.Random rng = new(seed);
        waterOffset = NextOffset(rng);
        islandOffset = NextOffset(rng);
        groundOffset = NextOffset(rng);
        activeBiome = biomes != null && biomes.Length > 0 ? biomes[((seed % biomes.Length) + biomes.Length) % biomes.Length] : null;
        InitDecorOffsets();

        Vector3Int cell = landTilemap.WorldToCell(SpawnAreaCenter());
        spawnCell = new Vector2Int(cell.x, cell.y);
        Vector3Int center = landTilemap.WorldToCell(transform.position);
        islandCenter = new Vector2(center.x, center.y);

        int cellCount = chunkSize * chunkSize;
        waterMask = new bool[chunkSize + 2, chunkSize + 2];
        landTiles = new TileBase[cellCount];
        waterTiles = new TileBase[cellCount];
        waterBackgroundTiles = new TileBase[cellCount];
        coastFoamTiles = new TileBase[cellCount];
        clearTiles = new TileBase[cellCount];
    }

    // Same point on every peer (the scene's spawn altar, else the generator), so host and clients clear the same decor; the local player's position differs per machine.
    private Vector3 SpawnAreaCenter()
    {
        PlayerSpawner spawner = FindAnyObjectByType<PlayerSpawner>();
        return spawner != null ? spawner.transform.position : transform.position;
    }

    // Each client streams chunks around its own local character.
    private Transform GetTrackedPlayer()
    {
        GameObject localPlayer = LocalPlayer.Get();
        return localPlayer != null ? localPlayer.transform : player;
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

    void Update()
    {
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

                waterTiles[index] = isWater ? waterTile : null;
                landTiles[index] = isWater ? null : PickLandTile(worldX, worldY);
                waterBackgroundTiles[index] = isCoastalGround ? waterBackgroundTile : null;
                coastFoamTiles[index] = isCoastalGround ? coastFoamTile : null;
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
}
