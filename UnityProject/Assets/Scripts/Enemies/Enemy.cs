using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// One networked enemy built from an EnemyKit; behavior is split across Enemy.*.cs by concern.
public partial class Enemy : NetworkBehaviour
{
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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.sharedMaterial = GetSlipperyMaterial();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        networkTransform = GetComponent<NetworkTransform>();

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        bodyRadius = circle != null ? circle.radius * scale : 0.4f;
        bodyOffset = circle != null ? circle.offset * (Vector2)transform.lossyScale : Vector2.zero;
        EnemyFlowFieldManager.BodyOffset = bodyOffset;

        AwakeHealth();
    }

    void Start()
    {
        lastPosition = transform.position;
        waveSpawner = FindAnyObjectByType<WaveSpawner>();
        if (Kit != null)
            StaggerTimers();
    }

    // Called by WaveSpawner right after Instantiate, before any network spawn.
    public void Initialize(EnemyKit kit)
    {
        Kit = kit;
        CacheAnimationHashes();
    }

    // Called by WaveSpawner before Spawn(), so remote clients get the kit in the first payload.
    public void SetNetworkedKitIndex(int index) => kitIndex.Value = index;

    public override void OnNetworkSpawn()
    {
        kitIndex.OnValueChanged += HandleKitIndexChanged;
        HandleKitIndexChanged(-1, kitIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        kitIndex.OnValueChanged -= HandleKitIndexChanged;
    }

    // Remote clients never get Initialize, so they resolve the kit from the replicated index.
    private void HandleKitIndexChanged(int previous, int current)
    {
        if (Kit != null || current < 0)
            return;

        WaveSpawner spawner = waveSpawner != null ? waveSpawner : FindAnyObjectByType<WaveSpawner>();
        EnemyKit kit = spawner != null ? spawner.GetKit(current) : null;
        if (kit != null)
            Initialize(kit);
    }

    void Update()
    {
        UpdateVisuals();
    }

    void FixedUpdate()
    {
        if (!this.HasServerAuthority())
            return;

        TickAttack();
        TickMovement();
    }
}
