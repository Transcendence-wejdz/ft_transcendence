using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// One networked enemy built from an EnemyKit; behavior is split across Enemy.*.cs by concern.
public partial class Enemy : NetworkBehaviour
{
    private const float DefaultBodyRadius = 0.4f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private NetworkTransform networkTransform;
    private WaveSpawner waveSpawner;

    private float bodyRadius;
    private Vector2 bodyOffset;

    private readonly NetworkVariable<int> kitIndex = new OfflineSafeNetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public EnemyKit Kit { get; private set; }

    // The collider is offset from the pivot; queries must start at the real body.
    public Vector2 BodyCenter => rb.position + bodyOffset;

    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.sharedMaterial = GetSlipperyMaterial();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        networkTransform = GetComponent<NetworkTransform>();
    }

    // Body size comes from the collider, shared with the flow field so its cells match an enemy body.
    private void InitBody()
    {
        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

        bodyRadius = DefaultBodyRadius;
        bodyOffset = Vector2.zero;
        if (circle != null)
        {
            bodyRadius = circle.radius * scale;
            bodyOffset = circle.offset * (Vector2)transform.lossyScale;
        }
        EnemyFlowFieldManager.BodyOffset = bodyOffset;
        EnemyFlowFieldManager.BodyRadius = bodyRadius;
    }

    private WaveSpawner GetWaveSpawner()
    {
        if (waveSpawner == null)
            return FindAnyObjectByType<WaveSpawner>();
        return waveSpawner;
    }

    // Remote clients never get Initialize, so they resolve the kit from the replicated index.
    private void HandleKitIndexChanged(int previous, int current)
    {
        WaveSpawner spawner;
        EnemyKit kit;

        if (Kit != null || current < 0)
            return;
        spawner = GetWaveSpawner();
        if (spawner == null)
            return;
        kit = spawner.GetKit(current);
        if (kit != null)
            Initialize(kit);
    }

    // Frozen in place (animation included) behind the Game Over screen.
    private bool FreezeIfRunOver()
    {
        if (!GameOverCheck.IsRunOver)
            return false;
        if (animator != null)
            animator.speed = 0f;
        if (this.HasServerAuthority())
            rb.linearVelocity = Vector2.zero;
        return true;
    }

    // Called by WaveSpawner right after Instantiate, before any network spawn.
    public void Initialize(EnemyKit kit)
    {
        Kit = kit;
        CacheAnimationHashes();
    }

    // Called by WaveSpawner before Spawn(), so remote clients get the kit in the first payload.
    public void SetNetworkedKitIndex(int index)
    {
        kitIndex.Value = index;
    }

    public override void OnNetworkSpawn()
    {
        kitIndex.OnValueChanged += HandleKitIndexChanged;
        HandleKitIndexChanged(-1, kitIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        kitIndex.OnValueChanged -= HandleKitIndexChanged;
    }

    void Awake()
    {
        CacheComponents();
        InitBody();
        AwakeHealth();
    }

    void Start()
    {
        lastPosition = transform.position;
        waveSpawner = FindAnyObjectByType<WaveSpawner>();
        if (Kit != null)
            StaggerTimers();
    }

    void Update()
    {
        if (FreezeIfRunOver())
            return;
        UpdateVisuals();
    }

    void FixedUpdate()
    {
        if (!this.HasServerAuthority() || GameOverCheck.IsRunOver)
            return;
        TickAttack();
        TickMovement();
    }
}
