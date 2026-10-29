using System;
using Unity.Netcode;
using UnityEngine;

public enum BossPattern : byte { None, Charge, Slam, Summon }

public enum BossPhase : byte { Windup, Active, Recovery }

// Server-authoritative boss built from a BossKit; tracks the single living boss for UI and the victory check, behavior split across Boss.*.cs.
public partial class Boss : NetworkBehaviour
{
    [SerializeField] private BossKit kit;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector2 bodyOffset;
    private float bodyRadius;

    public static Boss Active { get; private set; }
    public static event Action<Boss> OnActiveChanged;

    public BossKit Kit => kit;

    // The collider sits above the pivot; range checks and telegraphs start from the real body.
    public Vector2 BodyCenter => (Vector2)transform.position + bodyOffset;

    // Above 1 once enraged: windups, recoveries and cooldowns shrink, walking speeds up.
    private float Tempo => MaxHealth > 0f && CurrentHealth > 0f && CurrentHealth <= MaxHealth * kit.EnrageHealthFraction ? kit.EnrageTempo : 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        bodyOffset = circle != null ? circle.offset * (Vector2)transform.lossyScale : Vector2.zero;
        bodyRadius = circle != null ? circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y) : 0.5f;

        AwakeHealth();
    }

    void OnEnable()
    {
        Active = this;
        OnActiveChanged?.Invoke(this);
    }

    void OnDisable()
    {
        if (Active != this)
            return;

        Active = null;
        OnActiveChanged?.Invoke(null);
    }

    void Start()
    {
        lastPosition = transform.position;
        nextPatternTime = Time.time + kit.PatternCooldown;
        nextSummonTime = Time.time + kit.SummonInterval * 0.5f;
    }

    // Runs on every peer; facing is server-replicated, Idle/Walk come from real movement.
    void Update()
    {
        UpdateVisuals();
        UpdateLocalCharge();
    }

    // Server-only: a running combo owns movement, otherwise start a combo if one fits, else reposition.
    void FixedUpdate()
    {
        if (!this.HasServerAuthority())
            return;

        EnemyFlowFieldManager manager = EnemyFlowFieldManager.Instance;
        if (pattern == BossPattern.None && (target == null || target.IsDead || Time.time >= nextRetargetTime))
            ChooseTarget(manager);
        if (target != null)
            TrackTargetVelocity();

        if (pattern != BossPattern.None)
            TickPattern();
        else if (target == null)
            rb.linearVelocity = Vector2.zero;
        else if (Time.time < nextPatternTime || !TryStartCombo((target.Center - BodyCenter).magnitude))
            Reposition(manager);
    }
}
