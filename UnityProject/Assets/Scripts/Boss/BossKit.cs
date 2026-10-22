using UnityEngine;

// Per-boss tuning (health, chase, charge/slam/summon patterns, animator states), so a new boss is mostly data.
[CreateAssetMenu(fileName = "BossKit", menuName = "Transcendence/Boss Kit")]
public class BossKit : ScriptableObject
{
    [SerializeField] private string displayName = "Troll";

    [Header("Health")]
    [SerializeField] private float maxHealth = 1500f;
    [SerializeField] private float healthPerExtraPlayer = 0.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float retargetInterval = 0.5f;
    [SerializeField] private float steerInterval = 0.1f;

    [Header("Patterns")]
    [SerializeField] private float patternCooldown = 0.3f;
    [SerializeField] private float recoveryDuration = 0.8f;

    [Header("Enrage")]
    [SerializeField, Range(0f, 1f)] private float enrageHealthFraction = 0.5f;
    [SerializeField] private float enrageTempo = 1.35f;

    [Header("Charge")]
    [SerializeField] private float chargeMinRange = 4f;
    [SerializeField] private float chargeCooldown = 5f;
    [SerializeField] private float chargeLeadSeconds = 0.35f;
    [SerializeField] private int chargeChainCount = 3;
    [SerializeField] private float chargeWindup = 1f;
    [SerializeField] private float chargeSpeed = 14f;
    [SerializeField] private float chargeDistance = 9f;
    [SerializeField] private float chargeWidth = 1.8f;
    [SerializeField] private float chargeDamage = 30f;

    [Header("Slam")]
    [SerializeField] private float slamMaxRange = 3f;
    [SerializeField] private float slamCooldown = 2.5f;
    [SerializeField] private float slamWindup = 1.2f;
    [SerializeField] private float slamRadius = 3.5f;
    [SerializeField] private float slamDamage = 40f;
    [SerializeField] private float slamActiveDuration = 0.6f;

    [Header("Summon")]
    [SerializeField] private EnemyKit[] summonKits;
    [SerializeField] private int summonCount = 4;
    [SerializeField] private float summonInterval = 15f;
    [SerializeField] private float summonWindup = 1f;
    [SerializeField] private float summonRadius = 3f;
    [SerializeField] private float summonActiveDuration = 0.6f;
    [SerializeField] private GameObject[] summonFxPrefabs;
    [SerializeField] private Vector2 summonFxOffset = new Vector2(0f, 0.4f);

    [Header("Animator States")]
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string walkState = "Walk";
    [SerializeField] private string windupState = "Wind-up";
    [SerializeField] private string attackState = "Attack";
    [SerializeField] private string recoveryState = "Recovery";
    [SerializeField] private float chargeAnimationSpeed = 2.5f;

    public string DisplayName => displayName;
    public float MaxHealth => maxHealth;
    public float HealthPerExtraPlayer => healthPerExtraPlayer;
    public float MoveSpeed => moveSpeed;
    public float RetargetInterval => retargetInterval;
    public float SteerInterval => steerInterval;
    public float PatternCooldown => patternCooldown;
    public float RecoveryDuration => recoveryDuration;
    public float EnrageHealthFraction => enrageHealthFraction;
    public float EnrageTempo => enrageTempo;
    public float ChargeCooldown => chargeCooldown;
    public float ChargeLeadSeconds => chargeLeadSeconds;
    public int ChargeChainCount => chargeChainCount;
    public float SlamCooldown => slamCooldown;
    public float ChargeMinRange => chargeMinRange;
    public float ChargeWindup => chargeWindup;
    public float ChargeSpeed => chargeSpeed;
    public float ChargeDistance => chargeDistance;
    public float ChargeWidth => chargeWidth;
    public float ChargeDamage => chargeDamage;
    public float SlamMaxRange => slamMaxRange;
    public float SlamWindup => slamWindup;
    public float SlamRadius => slamRadius;
    public float SlamDamage => slamDamage;
    public float SlamActiveDuration => slamActiveDuration;
    public EnemyKit[] SummonKits => summonKits;
    public int SummonCount => summonCount;
    public float SummonInterval => summonInterval;
    public float SummonWindup => summonWindup;
    public float SummonRadius => summonRadius;
    public float SummonActiveDuration => summonActiveDuration;
    public GameObject[] SummonFxPrefabs => summonFxPrefabs;
    public Vector2 SummonFxOffset => summonFxOffset;
    public string IdleState => idleState;
    public string WalkState => walkState;
    public string WindupState => windupState;
    public string AttackState => attackState;
    public string RecoveryState => recoveryState;
    public float ChargeAnimationSpeed => chargeAnimationSpeed;
}
