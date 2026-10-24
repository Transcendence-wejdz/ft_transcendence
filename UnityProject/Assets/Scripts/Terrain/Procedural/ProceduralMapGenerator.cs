using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class ProceduralMapGenerator : MonoBehaviour, ISerializationCallbackReceiver
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
    [Tooltip("Runtime only. Fixed: the seed above. Random: rolled per load by the scene's MapSeedSync. Previous: the seed of the last map generated (e.g. the Boss island after Coop).")]
    [SerializeField] private SeedSource seedSource;

    [Header("Chunk Streaming")]
    [Range(8, 64)]
    [Tooltip("Cells per chunk side.")]
    [SerializeField] private int chunkSize = 16;
    [Range(0, 12)]
    [Tooltip("Extra buffer chunks loaded beyond the camera's current view, to avoid pop-in at screen edges.")]
    [SerializeField] private int viewDistanceInChunks = 1;
    [Range(1, 16)]
    [Tooltip("Max newly-needed chunks generated per frame after the initial load, to spread bursts across frames.")]
    [SerializeField] private int maxChunkGenerationsPerFrame = 2;
    [Tooltip("Island maps only: generate the whole island plus a camera-wide ring of ocean once at start, then never stream chunks again (no hitches when crossing chunk borders).")]
    [SerializeField] private bool loadWholeIsland = true;

    [Header("Biomes")]
    [PowerRange(0.005f, 0.5f, 2f)]
    [Tooltip("Perlin scale of the ground tile patches inside the biome. Smaller = larger patches.")]
    [SerializeField] private float groundNoiseScale = 0.1f;
    [Range(1, 6)]
    [Tooltip("Noise layers stacked for the ground patches. More = more detailed, broken-up borders.")]
    [SerializeField] private int groundNoiseOctaves = 1;
    [Range(0f, 1f)]
    [Tooltip("How much each extra octave weighs compared to the previous one. Higher = rougher patches.")]
    [SerializeField] private float groundNoisePersistence = 0.5f;
    [Range(0.5f, 3f)]
    [Tooltip("Stretches the noise away from its middle value, so the first and last ground tiles show up more.")]
    [SerializeField] private float groundNoiseContrast = 1f;
    [Tooltip("One biome per map, picked as seed modulo the biome count.")]
    [SerializeField] private Biome[] biomes;

    // Pre-biome global water/island values, only read once to seed biomes saved before these settings moved into each biome.
    [SerializeField, HideInInspector] private float waterNoiseScale = 0.1f;
    [SerializeField, HideInInspector] private float waterThreshold = 0.35f;
    [SerializeField, HideInInspector] private TileBase waterTile;
    [SerializeField, HideInInspector] private TileBase waterBackgroundTile;
    [SerializeField, HideInInspector] private TileBase coastFoamTile;
    [SerializeField, HideInInspector] private float islandRadius;
    [SerializeField, HideInInspector] private float islandEdgeVariation = 0.15f;
    [SerializeField, HideInInspector] private float islandEdgeNoiseScale = 0.05f;
    [SerializeField, HideInInspector] private float decorCoastOffset = 3f;

    [Header("Editor Preview")]
    [Tooltip("Keeps a generated preview of the map in the Scene view, refreshed on every inspector change.")]
    [SerializeField] private bool previewInEditor;
    [Range(1, 16)]
    [Tooltip("Chunk radius previewed around the generator when there is no island.")]
    [SerializeField] private int previewRadiusInChunks = 4;

    [System.Serializable]
    private class Biome : ISerializationCallbackReceiver
    {
        public string name;
        [Tooltip("Ground tiles mixed inside this biome, picked by the ground noise in list order (first = lowest noise).")]
        public GroundEntry[] grounds;
        [Range(0f, 30f)]
        [Tooltip("Radius in cells around the player spawn kept as dry land with no decor.")]
        public float spawnSafeRadius = 5f;
        [Tooltip("Decor spawned on this biome. Earlier entries win when several match a cell.")]
        public DecorEntry[] decor;
        public WaterSettings water = new WaterSettings();
        public IslandSettings island = new IslandSettings();
        [HideInInspector] public bool hasOwnWaterIsland;
        [SerializeField, HideInInspector] private TileBase[] groundTiles;
        [SerializeField, HideInInspector] private float[] groundWeights;

        public void OnBeforeSerialize() { }

        // Older data kept tiles and weights in two parallel arrays; they merge into named entries.
        public void OnAfterDeserialize()
        {
            if (groundTiles == null || groundTiles.Length == 0)
                return;
            if (grounds == null || grounds.Length == 0)
            {
                grounds = new GroundEntry[groundTiles.Length];
                for (int i = 0; i < groundTiles.Length; i++)
                    grounds[i] = new GroundEntry { tile = groundTiles[i], weight = groundWeights != null && i < groundWeights.Length ? groundWeights[i] : 1f };
            }
            groundTiles = null;
            groundWeights = null;
        }
    }

    [System.Serializable]
    private class WaterSettings
    {
        [PowerRange(0.005f, 0.5f, 2f)]
        [Tooltip("Perlin scale of the inland lakes. Smaller = fewer, larger lakes.")]
        public float noiseScale = 0.1f;
        [Range(0f, 1f)]
        [Tooltip("Cells whose water noise is below this become water. 0 = no inland lakes.")]
        public float threshold = 0.35f;
        public TileBase tile;
        public TileBase backgroundTile;
        public TileBase coastFoamTile;
    }

    [System.Serializable]
    private class IslandSettings
    {
        [Range(0f, 300f)]
        [Tooltip("Radius in cells around the generator beyond which everything is water. 0 = endless land.")]
        public float radius;
        [Range(0f, 0.5f)]
        [Tooltip("How far the coastline wanders from a perfect circle, as a fraction of the radius.")]
        public float edgeVariation = 0.15f;
        [PowerRange(0.005f, 0.5f, 2f)]
        [Tooltip("Perlin scale of the coastline wobble. Smaller = fewer, wider bays and capes.")]
        public float edgeNoiseScale = 0.05f;
        [Range(-10f, 10f)]
        [Tooltip("Decor only spawns up to this many cells past the coastline (negative keeps it inland). Ignored without an island.")]
        public float decorCoastOffset = 3f;
    }

    [System.Serializable]
    private class GroundEntry
    {
        [HideInInspector] public string label;
        public TileBase tile;
        [Range(0f, 1f)]
        [Tooltip("Share of the noise range this tile gets, relative to the others.")]
        public float weight = 1f;
    }

    private enum SeedSource { Fixed, Random, Previous }

    private enum DecorSurface { Land, Water }

    [System.Serializable]
    private class DecorEntry : ISerializationCallbackReceiver
    {
        [HideInInspector] public string label;
        public GameObject prefab;
        [Tooltip("Land: only on dry tiles. Water: only on water tiles.")]
        public DecorSurface surface = DecorSurface.Land;
        [PowerRange(0f, 1f, 3f)]
        [Tooltip("Chance applied to cells inside the noise range - main density knob. The slider is finer near 0.")]
        public float density = 0.05f;
        [PowerRange(0.01f, 1f, 2f)]
        [Tooltip("Perlin noise scale for clustering. Smaller = larger, smoother clusters.")]
        public float noiseScale = 0.2f;
        [MinMaxRange(0f, 1f)]
        [Tooltip("Cells spawn only where the clustering noise falls in this range. Narrow = rarer patches; a middle band = rings around the clusters.")]
        public Vector2 noiseRange = new Vector2(0.75f, 1f);
        [SerializeField, HideInInspector] private float noiseThreshold = -1f;

        public void OnBeforeSerialize() { }

        // Older data only had a lower cutoff: it becomes the range's min, with the max at 1.
        public void OnAfterDeserialize()
        {
            if (noiseThreshold < 0f)
                return;
            noiseRange = new Vector2(noiseThreshold, 1f);
            noiseThreshold = -1f;
        }
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
    private int activeSeed;
    private bool initialized;
    private int? localRandomSeed;

    private static int? previousSeed;

    private static readonly WaterSettings NoWater = new WaterSettings { threshold = 0f };
    private static readonly IslandSettings NoIsland = new IslandSettings();

    private WaterSettings Water => activeBiome != null ? activeBiome.water : NoWater;
    private IslandSettings Island => activeBiome != null ? activeBiome.island : NoIsland;

    public void OnBeforeSerialize() { }

    // Biomes saved before water/island moved into them start from the old global values.
    public void OnAfterDeserialize()
    {
        if (biomes == null)
            return;

        foreach (Biome biome in biomes)
        {
            if (biome == null || biome.hasOwnWaterIsland)
                continue;
            biome.water = new WaterSettings { noiseScale = waterNoiseScale, threshold = waterThreshold, tile = waterTile, backgroundTile = waterBackgroundTile, coastFoamTile = coastFoamTile };
            biome.island = new IslandSettings { radius = islandRadius, edgeVariation = islandEdgeVariation, edgeNoiseScale = islandEdgeNoiseScale, decorCoastOffset = decorCoastOffset };
            biome.hasOwnWaterIsland = true;
        }
    }

    void Awake()
    {
        TryInitialize();
    }

    // A Random seed waits for MapSeedSync, which a client only gets once the scene's network objects spawn; without one in the scene it rolls locally instead of never generating.
    private bool TryInitialize()
    {
        if (!initialized && Application.isPlaying && seedSource == SeedSource.Random && !MapSeedSync.Current.HasValue)
        {
            if (FindAnyObjectByType<MapSeedSync>(FindObjectsInactive.Include) != null)
                return false;
            Debug.LogWarning($"{name}: Seed Source is Random but the scene has no MapSeedSync, rolling a local seed (peers will not share the map).", this);
            localRandomSeed = Random.Range(int.MinValue, int.MaxValue);
        }
        if (!initialized)
            Initialize();
        return true;
    }

    // Seeds noise offsets and allocates per-chunk buffers, shared by runtime and the editor preview.
    private void Initialize()
    {
        activeSeed = ResolveSeed();
        initialized = Application.isPlaying;
        System.Random rng = new(activeSeed);
        waterOffset = NextOffset(rng);
        islandOffset = NextOffset(rng);
        groundOffset = NextOffset(rng);
        activeBiome = biomes != null && biomes.Length > 0 ? biomes[((activeSeed % biomes.Length) + biomes.Length) % biomes.Length] : null;
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

    // Every peer ran the same previous scene, so a Previous seed matches without network sync; the editor preview always uses the fixed seed.
    private int ResolveSeed()
    {
        if (!Application.isPlaying)
            return seed;
        if (seedSource == SeedSource.Previous)
            return previousSeed ?? seed;

        int resolved = seedSource != SeedSource.Random ? seed
            : MapSeedSync.Current.HasValue ? MapSeedSync.Current.Value
            : localRandomSeed ?? seed;
        previousSeed = resolved;
        return resolved;
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
}
