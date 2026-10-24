using Unity.Netcode;
using UnityEngine;

// Cosmetic Sky-layer clouds over the map; positions derive from the map seed and the shared network clock, so peers match without syncing.
public class SeaClouds : MonoBehaviour
{
    [SerializeField] private Sprite[] cloudSprites;
    [Range(0, 300)]
    [SerializeField] private int cloudCount = 10;
    [Tooltip("Wind direction, normalized at runtime.")]
    [SerializeField] private Vector2 windDirection = new(1f, 0.15f);
    [SerializeField] private Vector2 speedRange = new(0.2f, 0.6f);
    [SerializeField] private Vector2 scaleRange = new(0.7f, 1.3f);
    [Range(0f, 1f)]
    [SerializeField] private float opacity = 0.9f;
    [Tooltip("World-space size of the cloud area, centered on this object. Clouds wrap inside it, so keep it a bit wider than the map.")]
    [SerializeField] private Vector2 areaSize = new(60f, 60f);
    [Range(0f, 30f)]
    [Tooltip("Clouds fade out over this distance before reaching the area edge, so the wrap to the other side is invisible.")]
    [SerializeField] private float edgeFade = 8f;
    [Tooltip("Clouds turn see-through around the local player when they cover them.")]
    [SerializeField] private bool revealPlayer = true;
    [Tooltip("Used when the scene has no map generator (e.g. Lobby), so peers still share the same sky.")]
    [SerializeField] private int fallbackSeed;
    [SerializeField] private string sortingLayer = "Sky";
    [SerializeField] private int sortingOrder;
    [Tooltip("Leave empty for the default sprite material.")]
    [SerializeField] private Material cloudMaterial;

    private Transform[] clouds;
    private SpriteRenderer[] renderers;
    private Vector2[] startOffsets;
    private float[] speeds;
    private float[] alphas;
    private Vector2Int[] wrapKeys;
    private int cloudSeed;
    private bool waitForMapSeed;
    private bool spawned;

    void Start()
    {
        if (cloudSprites == null || cloudSprites.Length == 0)
        {
            enabled = false;
            return;
        }
        waitForMapSeed = FindAnyObjectByType<ProceduralMapGenerator>(FindObjectsInactive.Include) != null;
    }

    // Each cloud gets its start point and speed from the seed, so they match across peers.
    private void Spawn(int seed)
    {
        cloudSeed = seed;
        clouds = new Transform[cloudCount];
        renderers = new SpriteRenderer[cloudCount];
        startOffsets = new Vector2[cloudCount];
        speeds = new float[cloudCount];
        alphas = new float[cloudCount];
        wrapKeys = new Vector2Int[cloudCount];
        System.Random rng = new(seed);

        for (int i = 0; i < cloudCount; i++)
        {
            GameObject cloud = new("Cloud");
            cloud.transform.SetParent(transform, false);
            clouds[i] = cloud.transform;
            renderers[i] = cloud.AddComponent<SpriteRenderer>();
            renderers[i].sortingLayerName = sortingLayer;
            renderers[i].sortingOrder = sortingOrder;
            if (cloudMaterial != null)
                renderers[i].sharedMaterial = cloudMaterial;
            startOffsets[i] = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
            speeds[i] = Mathf.Lerp(speedRange.x, speedRange.y, (float)rng.NextDouble());
            wrapKeys[i] = new Vector2Int(int.MinValue, int.MinValue);
            if (revealPlayer)
            {
                RevealOccluder occluder = cloud.AddComponent<RevealOccluder>();
                occluder.AlwaysInFront = true;
                occluder.Moves = true;
            }
        }
        spawned = true;
    }

    void Update()
    {
        if (!spawned)
        {
            int? seed = waitForMapSeed ? ProceduralMapGenerator.RuntimeSeed : fallbackSeed;
            if (!seed.HasValue)
                return;
            Spawn(seed.Value);
        }

        Rect area = GetArea();
        Vector2 wind = windDirection.normalized;
        double time = SharedTime();

        for (int i = 0; i < cloudCount; i++)
        {
            double travelled = speeds[i] * time;
            double rawX = startOffsets[i].x * area.width + wind.x * travelled;
            double rawY = startOffsets[i].y * area.height + wind.y * travelled;
            Vector2Int wrap = new((int)System.Math.Floor(rawX / area.width), (int)System.Math.Floor(rawY / area.height));
            float x = area.xMin + (float)(rawX - wrap.x * (double)area.width);
            float y = area.yMin + (float)(rawY - wrap.y * (double)area.height);

            if (wrap != wrapKeys[i])
            {
                wrapKeys[i] = wrap;
                Restyle(i, wrap);
            }

            clouds[i].position = new Vector3(x, y, 0f);

            float edgeDistance = Mathf.Min(x - area.xMin, area.xMax - x, y - area.yMin, area.yMax - y);
            float alpha = opacity * (edgeFade > 0f ? Mathf.Clamp01(edgeDistance / edgeFade) : 1f);
            if (Mathf.Abs(alpha - alphas[i]) > 0.004f)
            {
                alphas[i] = alpha;
                renderers[i].color = new Color(1f, 1f, 1f, alpha);
            }
        }
    }

    // Netcode's server time is the same on every peer; offline falls back to the local clock.
    private static double SharedTime()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network != null && network.IsListening && (network.IsServer || network.IsConnectedClient))
            return network.ServerTime.Time;
        return Time.timeAsDouble;
    }

    private Rect GetArea()
    {
        Vector3 center = transform.position;
        return new Rect(center.x - areaSize.x * 0.5f, center.y - areaSize.y * 0.5f, areaSize.x, areaSize.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0f));
    }

    // New look each time a cloud re-enters, picked from the seed and wrap count so every peer agrees.
    private void Restyle(int i, Vector2Int wrap)
    {
        System.Random rng = new(Hash(cloudSeed, i, wrap.x, wrap.y));
        renderers[i].sprite = cloudSprites[rng.Next(cloudSprites.Length)];
        renderers[i].color = new Color(1f, 1f, 1f, alphas[i]);
        renderers[i].flipX = rng.NextDouble() < 0.5;
        clouds[i].localScale = Vector3.one * Mathf.Lerp(scaleRange.x, scaleRange.y, (float)rng.NextDouble());
    }

    private static int Hash(int a, int b, int c, int d)
    {
        unchecked
        {
            int h = a;
            h = h * 486187739 + b;
            h = h * 486187739 + c;
            h = h * 486187739 + d;
            return h;
        }
    }
}
