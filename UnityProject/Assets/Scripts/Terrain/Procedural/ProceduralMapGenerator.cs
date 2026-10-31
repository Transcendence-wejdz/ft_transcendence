using UnityEngine;
using UnityEngine.Tilemaps;

// Seeded tilemap map generator; settings types, chunk streaming, noise, decor and preview live in ProceduralMapGenerator.*.cs.
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

    [Header("Editor Preview")]
    [Tooltip("Keeps a generated preview of the map in the Scene view, refreshed on every inspector change.")]
    [SerializeField] private bool previewInEditor;
    [Range(1, 16)]
    [Tooltip("Chunk radius previewed around the generator when there is no island.")]
#pragma warning disable CS0414
    [SerializeField] private int previewRadiusInChunks = 4;
#pragma warning restore CS0414

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

    // Seed of the current scene map at runtime, null until known.
    public static int? RuntimeSeed { get; private set; }

    private static readonly WaterSettings NoWater = new WaterSettings { threshold = 0f };
    private static readonly IslandSettings NoIsland = new IslandSettings();

    private WaterSettings Water
    {
        get
        {
            if (activeBiome == null)
                return NoWater;
            return activeBiome.water;
        }
    }

    private IslandSettings Island
    {
        get
        {
            if (activeBiome == null)
                return NoIsland;
            return activeBiome.island;
        }
    }

    // One biome per map, picked as seed modulo the biome count.
    private Biome PickBiome()
    {
        if (biomes == null || biomes.Length == 0)
            return null;
        return biomes[((activeSeed % biomes.Length) + biomes.Length) % biomes.Length];
    }

    private void AllocateChunkBuffers()
    {
        int cellCount = chunkSize * chunkSize;

        waterMask = new bool[chunkSize + 2, chunkSize + 2];
        landTiles = new TileBase[cellCount];
        waterTiles = new TileBase[cellCount];
        waterBackgroundTiles = new TileBase[cellCount];
        coastFoamTiles = new TileBase[cellCount];
        clearTiles = new TileBase[cellCount];
    }

    private void InitCells()
    {
        Vector3Int cell = landTilemap.WorldToCell(SpawnAreaCenter());
        Vector3Int center = landTilemap.WorldToCell(transform.position);

        spawnCell = new Vector2Int(cell.x, cell.y);
        islandCenter = new Vector2(center.x, center.y);
    }

    // Seeds noise offsets and allocates per-chunk buffers, shared by runtime and the editor preview.
    private void Initialize()
    {
        System.Random rng;

        activeSeed = ResolveSeed();
        initialized = Application.isPlaying;
        if (Application.isPlaying)
            RuntimeSeed = activeSeed;
        rng = new System.Random(activeSeed);
        waterOffset = NextOffset(rng);
        islandOffset = NextOffset(rng);
        groundOffset = NextOffset(rng);
        activeBiome = PickBiome();
        InitDecorOffsets();
        InitCells();
        AllocateChunkBuffers();
    }

    // Same point on every peer (the scene's spawn altar, else the generator), so host and clients clear the same decor; the local player's position differs per machine.
    private Vector3 SpawnAreaCenter()
    {
        PlayerSpawner spawner = FindAnyObjectByType<PlayerSpawner>();

        if (spawner == null)
            return transform.position;
        return spawner.transform.position;
    }

    // Each client streams chunks around its own local character.
    private Transform GetTrackedPlayer()
    {
        GameObject localPlayer = LocalPlayer.Get();

        if (localPlayer == null)
            return player;
        return localPlayer.transform;
    }

    void Awake()
    {
        TryInitialize();
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
            RuntimeSeed = null;
    }
}
