using UnityEngine;

// Per-boss tuning (health, chase, charge/slam/summon patterns, animator states), so a new boss is mostly data.
[CreateAssetMenu(fileName = "BossKit", menuName = "Transcendence/Boss Kit")]
public class BossKit : ScriptableObject
{
    [SerializeField] private string displayName = "Troll";
    [PowerRange(1f, 20000f, 2f)]
    [Tooltip("Hit points with one player.")]
    [SerializeField] private float maxHealth = 1500f;
    [Range(0f, 2f)]
    [Tooltip("Extra max health per additional player, as a fraction of Max Health.")]
    [SerializeField] private float healthPerExtraPlayer = 0.5f;
    [Range(0f, 10f)]
    [Tooltip("Walk speed in units per second (multiplied by the enrage tempo).")]
    [SerializeField] private float moveSpeed = 1.8f;
    [Range(0.05f, 3f)]
    [Tooltip("Seconds between target re-evaluations.")]
    [SerializeField] private float retargetInterval = 0.5f;
    [Range(0.02f, 1f)]
    [Tooltip("Seconds between steering updates.")]
    [SerializeField] private float steerInterval = 0.1f;
    [Range(0f, 5f)]
    [Tooltip("Pause between two combos.")]
    [SerializeField] private float patternCooldown = 0.3f;
    [Range(0f, 5f)]
    [Tooltip("Recovery after the last step of a combo, when the boss is open to hits.")]
    [SerializeField] private float recoveryDuration = 0.8f;
    [Range(0f, 1f)]
    [Tooltip("Health fraction under which the boss enrages.")]
    [SerializeField] private float enrageHealthFraction = 0.5f;
    [Range(1f, 3f)]
    [Tooltip("Speed multiplier on movement, windups, recoveries and cooldowns once enraged.")]
    [SerializeField] private float enrageTempo = 1.35f;
    [Range(0f, 30f)]
    [Tooltip("The boss only opens with chained charges when the target is at least this far.")]
    [SerializeField] private float chargeMinRange = 4f;
    [PowerRange(0.1f, 60f, 2f)]
    [Tooltip("Seconds before charging again, counted from the end of the combo.")]
    [SerializeField] private float chargeCooldown = 5f;
    [Range(0f, 2f)]
    [Tooltip("How far ahead (in seconds of target movement) the charge aims.")]
    [SerializeField] private float chargeLeadSeconds = 0.35f;
    [Range(1, 10)]
    [Tooltip("Charges chained at range before the closing summon.")]
    [SerializeField] private int chargeChainCount = 3;
    [Range(0f, 5f)]
    [Tooltip("Windup before a charge (shortened when chained).")]
    [SerializeField] private float chargeWindup = 1f;
    [Range(1f, 40f)]
    [Tooltip("Charge speed in units per second.")]
    [SerializeField] private float chargeSpeed = 14f;
    [Range(1f, 40f)]
    [Tooltip("Max charge length (it overshoots the target by 3 units, minimum 4).")]
    [SerializeField] private float chargeDistance = 9f;
    [Range(0.1f, 10f)]
    [Tooltip("Charge lane width (never narrower than the body plus a contact margin).")]
    [SerializeField] private float chargeWidth = 1.8f;
    [PowerRange(0f, 500f, 2f)]
    [Tooltip("Damage to each player the charge runs over (once per charge).")]
    [SerializeField] private float chargeDamage = 30f;
    [Range(0f, 20f)]
    [Tooltip("Melee distance: below it the boss uses charge + slam, and circles the target at 80% of it.")]
    [SerializeField] private float slamMaxRange = 3f;
    [PowerRange(0.1f, 60f, 2f)]
    [Tooltip("Seconds before slamming again, counted from the end of the combo.")]
    [SerializeField] private float slamCooldown = 2.5f;
    [Range(0f, 5f)]
    [Tooltip("Windup before the slam lands.")]
    [SerializeField] private float slamWindup = 1.2f;
    [Range(0.1f, 20f)]
    [Tooltip("Slam radius around the body center.")]
    [SerializeField] private float slamRadius = 3.5f;
    [PowerRange(0f, 500f, 2f)]
    [Tooltip("Damage to each player inside the slam.")]
    [SerializeField] private float slamDamage = 40f;
    [Range(0f, 3f)]
    [Tooltip("Duration of the slam animation once it lands.")]
    [SerializeField] private float slamActiveDuration = 0.6f;
    [SerializeField] private EnemyKit[] summonKits;
    [Range(0, 50)]
    [Tooltip("Enemies spawned per summon.")]
    [SerializeField] private int summonCount = 4;
    [PowerRange(1f, 120f, 2f)]
    [Tooltip("Seconds between summons (the first one opens the fight).")]
    [SerializeField] private float summonInterval = 15f;
    [Range(0f, 5f)]
    [Tooltip("Windup before the summon.")]
    [SerializeField] private float summonWindup = 1f;
    [Range(0.5f, 20f)]
    [Tooltip("Summoned enemies appear within this radius around the boss.")]
    [SerializeField] private float summonRadius = 3f;
    [Range(0f, 3f)]
    [Tooltip("Duration of the summon animation.")]
    [SerializeField] private float summonActiveDuration = 0.6f;
    [SerializeField] private GameObject[] summonFxPrefabs;
    [SerializeField] private Vector2 summonFxOffset = new Vector2(0f, 0.4f);
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string walkState = "Walk";
    [SerializeField] private string windupState = "Wind-up";
    [SerializeField] private string attackState = "Attack";
    [SerializeField] private string recoveryState = "Recovery";
    [Range(0.1f, 5f)]
    [Tooltip("Walk animation speed while charging.")]
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
