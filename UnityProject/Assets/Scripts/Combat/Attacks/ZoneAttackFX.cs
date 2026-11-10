using System.Collections.Generic;
using UnityEngine;

// Ground zone FX in three phases (start once, loop for a duration, end once) that damages everything inside it every tick during the loop; only the attacker's copy deals damage. An optional overlay layer (e.g. falling arrows) plays the same frames Y-sorted with characters.
public partial class ZoneAttackFX : MonoBehaviour, IAttackFX
{
    private enum Phase { Start, Loop, End }

    private const string GroundSortingLayer = "Ground";

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] startFrames;
    [SerializeField] private Sprite[] loopFrames;
    [SerializeField] private Sprite[] endFrames;
    [SerializeField] private float frameRate = 12f;
    [Range(0.1f, 20f)]
    [Tooltip("Seconds the loop phase lasts, ticking damage meanwhile.")]
    [SerializeField] private float duration = 2.5f;
    [Range(0.05f, 3f)]
    [Tooltip("Seconds between two damage ticks; each tick hits everything inside once.")]
    [SerializeField] private float tickInterval = 0.5f;
    [Tooltip("Draws on the Ground sorting layer, under every character and decor (like enemy telegraphs), instead of Y-sorting with them.")]
    [SerializeField] private bool drawOnGround = true;
    [Tooltip("Optional second layer drawn with the same frame timing but Y-sorted with characters, so it shows in front of those behind it; leave empty for a single-layer zone.")]
    [SerializeField] private SpriteRenderer overlayRenderer;
    [SerializeField] private Sprite[] overlayStartFrames;
    [SerializeField] private Sprite[] overlayLoopFrames;
    [SerializeField] private Sprite[] overlayEndFrames;

    private GameObject owner;
    private float damage;
    private Collider2D hitbox;
    private Phase phase;
    private int frameIndex;
    private float frameTimer;
    private float loopTimer;
    private float tickTimer;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<IDamageable> hitThisTick = new HashSet<IDamageable>();
    private ContactFilter2D filter;

    private void SetupZoneRenderer(Color tint)
    {
        if (spriteRenderer == null)
            return;
        spriteRenderer.color = tint;
        if (drawOnGround)
        {
            spriteRenderer.sortingLayerName = GroundSortingLayer;
            spriteRenderer.sortingOrder = 0;
        }
        else
            spriteRenderer.sortingOrder = SortingLayer_Auto.OrderForY(transform.position.y);
    }

    private void SetupOverlayRenderer(Color tint)
    {
        if (overlayRenderer == null)
            return;
        overlayRenderer.color = tint;
        overlayRenderer.sortingOrder = SortingLayer_Auto.OrderForY(transform.position.y);
    }

    // Skips empty phases, so a sheet without a start or end still works; past End the FX is gone.
    private void EnterPhase(Phase next)
    {
        while (next <= Phase.End && Frames(next).Length == 0 && next != Phase.Loop)
            next++;
        if (next > Phase.End)
        {
            Destroy(gameObject);
            return;
        }
        phase = next;
        frameIndex = 0;
        frameTimer = 0f;
        if (Frames(phase).Length > 0)
            SetFrame(0);
        if (phase != Phase.Loop)
            return;
        loopTimer = 0f;
        tickTimer = 0f;
        DamageInside();
    }

    // Ticks damage during the loop; true once the loop is over and the end phase started.
    private bool TickLoop()
    {
        loopTimer += Time.deltaTime;
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            DamageInside();
        }
        if (loopTimer < duration)
            return false;
        EnterPhase(Phase.End);
        return true;
    }

    public void Init(GameObject attacker, bool hasHitbox, AbilityKit ability, float damageMultiplier)
    {
        Color tint = CharacterCatalog.FxColorOf(attacker);

        owner = attacker;
        SetupZoneRenderer(tint);
        SetupOverlayRenderer(tint);
        FxGlow.Setup(gameObject, attacker, ability);
        damage = ability.Damage * damageMultiplier;
        if (!hasHitbox && hitbox != null)
            hitbox.enabled = false;
    }

    void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        filter = ContactFilter2D.noFilter;
        EnterPhase(Phase.Start);
    }

    void Update()
    {
        if (phase == Phase.Loop && TickLoop())
            return;
        AdvanceFrames();
    }
}
