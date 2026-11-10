using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Decor broken by player attacks; damage is kept by cell, so chunk reloads and rejoins stay in sync.
public partial class DestructibleDecor : MonoBehaviour, IDamageable
{
    private const float PlacedCellPrecision = 100f;
    private const int PlacedCellOffset = 1000000;

    [SerializeField] private float maxHealth = 45f;
    [SerializeField] private Sprite stumpSprite;

    private static readonly Dictionary<Vector2Int, float> DamageByCell = new Dictionary<Vector2Int, float>();
    private static readonly Dictionary<Vector2Int, DestructibleDecor> PlacedByCell = new Dictionary<Vector2Int, DestructibleDecor>();

    private SpriteRenderer spriteRenderer;
    private DecorFlipbook flipbook;
    private RevealOccluder occluder;
    private Collider2D[] colliders;
    private Vector3 spriteRestPosition;
    private Vector2Int cell;
    private bool bound;
    private bool broken;

    public static IReadOnlyDictionary<Vector2Int, float> Damage => DamageByCell;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ListenForNewScenes()
    {
        DamageByCell.Clear();
        PlacedByCell.Clear();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // Every run or map starts with its decor intact.
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            DamageByCell.Clear();
    }

    private static float DamageAt(Vector2Int cell)
    {
        float damage;

        DamageByCell.TryGetValue(cell, out damage);
        return damage;
    }

    private void Unbind()
    {
        DestructibleDecor placed;

        if (bound && PlacedByCell.TryGetValue(cell, out placed) && placed == this)
            PlacedByCell.Remove(cell);
    }

    private void Refresh(bool withEffects)
    {
        if (broken)
            return;
        if (DamageAt(cell) >= maxHealth)
            Break(withEffects);
        else if (withEffects)
            PlayHitFeedback();
    }

    // Same position on every peer, offset so it can't match a generated cell.
    private Vector2Int HandPlacedCell()
    {
        Vector3 position = transform.position;

        return new Vector2Int(Mathf.RoundToInt(position.x * PlacedCellPrecision) + PlacedCellOffset, Mathf.RoundToInt(position.y * PlacedCellPrecision));
    }

    // Adds a hit from any peer; a cell not loaded here finds it when it binds.
    public static void ApplyHit(Vector2Int cell, float amount)
    {
        DestructibleDecor placed;

        DamageByCell[cell] = DamageAt(cell) + amount;
        if (PlacedByCell.TryGetValue(cell, out placed))
            placed.Refresh(true);
    }

    // Start of a join: the host's state replaces this client's, with everything intact again.
    public static void ClearAll()
    {
        DamageByCell.Clear();
        foreach (DestructibleDecor placed in PlacedByCell.Values)
            placed.Repair();
    }

    // The host's totals for a joining player, with no effects.
    public static void ApplyTotals(int[] xs, int[] ys, float[] totals)
    {
        Vector2Int cell;
        DestructibleDecor placed;

        for (int i = 0; i < xs.Length; i++)
        {
            cell = new Vector2Int(xs[i], ys[i]);
            DamageByCell[cell] = totals[i];
            if (PlacedByCell.TryGetValue(cell, out placed))
                placed.Refresh(false);
        }
    }

    // Called each time this pooled instance takes a cell, or at Start when hand-placed.
    public void Bind(Vector2Int worldCell)
    {
        Unbind();
        cell = worldCell;
        bound = true;
        occluder = GetComponent<RevealOccluder>();
        PlacedByCell[cell] = this;
        Repair();
        Refresh(false);
    }

    public void RequestDamage(float amount)
    {
        if (broken)
            return;
        ApplyHit(cell, amount);
        LocalPlayer.GetPlayer()?.BroadcastDecorHit(cell, amount);
    }

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        flipbook = GetComponentInChildren<DecorFlipbook>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
        spriteRestPosition = spriteRenderer.transform.localPosition;
    }

    // Generated decor is bound by now; anything else was placed by hand, like the Lobby's.
    void Start()
    {
        if (!bound)
            Bind(HandPlacedCell());
    }

    void OnDisable()
    {
        StopShake();
    }

    void OnDestroy()
    {
        Unbind();
    }
}
