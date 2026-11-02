using System;
using Unity.Netcode;
using UnityEngine;

public enum BossPattern : byte { None, Charge, Slam, Summon }

public enum BossPhase : byte { Windup, Active, Recovery }

// Server-authoritative boss built from a BossKit; tracks the single living boss for UI and the victory check, behavior split across Boss.*.cs.
public partial class Boss : NetworkBehaviour
{
    private const float DefaultBodyRadius = 0.5f;

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

    private bool IsEnraged
    {
        get
        {
            if (MaxHealth <= 0f || CurrentHealth <= 0f)
                return false;
            return CurrentHealth <= MaxHealth * kit.EnrageHealthFraction;
        }
    }

    // Above 1 once enraged: windups, recoveries and cooldowns shrink, walking speeds up.
    private float Tempo
    {
        get
        {
            if (IsEnraged)
                return kit.EnrageTempo;
            return 1f;
        }
    }

    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
    }

    private void InitBody()
    {
        CircleCollider2D circle = GetComponent<CircleCollider2D>();

        bodyOffset = Vector2.zero;
        bodyRadius = DefaultBodyRadius;
        if (circle == null)
            return;
        bodyOffset = circle.offset * (Vector2)transform.lossyScale;
        bodyRadius = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
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

    private bool NeedsNewTarget()
    {
        if (pattern != BossPattern.None)
            return false;
        if (target == null || target.IsDead)
            return true;
        return Time.time >= nextRetargetTime;
    }

    // A running combo owns movement, otherwise start a combo if one fits, else reposition.
    private void TickBehaviour(EnemyFlowFieldManager manager)
    {
        if (pattern != BossPattern.None)
        {
            TickPattern();
            return;
        }
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (Time.time < nextPatternTime || !TryStartCombo((target.Center - BodyCenter).magnitude))
            Reposition(manager);
    }

    void Awake()
    {
        CacheComponents();
        InitBody();
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
        if (FreezeIfRunOver())
            return;
        UpdateVisuals();
        UpdateLocalCharge();
    }

    void FixedUpdate()
    {
        EnemyFlowFieldManager manager;

        if (!this.HasServerAuthority() || GameOverCheck.IsRunOver)
            return;
        SetChargePassThrough(IsCharging);
        manager = EnemyFlowFieldManager.Instance;
        if (NeedsNewTarget())
            ChooseTarget(manager);
        if (target != null)
            TrackTargetVelocity();
        TickBehaviour(manager);
    }
}
