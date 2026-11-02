using UnityEngine;

public enum EnemyAttackShape { Cone, Lane, Circle }

public enum EnemyBehaviour { Horde, Ranged, Encircle }

// Per-enemy-type stats/combat tuning, so new enemy variants are pure data, not code.
[CreateAssetMenu(fileName = "EnemyKit", menuName = "Transcendence/Enemy Kit")]
public partial class EnemyKit : ScriptableObject
{
    private const float MinDirectionSqr = 0.0001f;
    private const float JoinDistanceMargin = 2f;

    [Range(1, 10)]
    [Tooltip("Difficulty tier: decides from which wave this enemy can spawn.")]
    [SerializeField] private int tier = 1;
    [PowerRange(1f, 2000f, 2f)]
    [Tooltip("Hit points.")]
    [SerializeField] private float maxHealth = 40f;
    [Range(0, 500)]
    [Tooltip("XP granted to the players when it dies.")]
    [SerializeField] private int xpReward = 10;
    [Range(0, 5000)]
    [Tooltip("Score added to the run when it dies.")]
    [SerializeField] private int scoreReward = 100;
    [Range(0f, 10f)]
    [Tooltip("Chase speed in units per second.")]
    [SerializeField] private float moveSpeed = 2.5f;
    [Tooltip("Hit zone drawn by the telegraph and checked by the hit: Cone (angle), Lane (width) or Circle around the enemy.")]
    [SerializeField] private EnemyAttackShape attackShape = EnemyAttackShape.Cone;
    [Tooltip("Distance at which the enemy starts an attack.")]
    [PowerRange(0.1f, 20f, 2f)]
    [SerializeField] private float attackRange = 0.8f;
    [Range(10f, 360f)]
    [Tooltip("Cone only: opening angle of the hit zone.")]
    [SerializeField] private float attackAngle = 100f;
    [Tooltip("Lane only: width of the hit zone.")]
    [Range(0.1f, 5f)]
    [SerializeField] private float attackWidth = 0.8f;
    [Tooltip("Melee only: where the hit zone starts, relative to the body center and turned with the swing. X = forward, Y = to the left. Draggable in the preview.")]
    [SerializeField] private Vector2 attackOrigin;
    [PowerRange(0f, 200f, 2f)]
    [Tooltip("Damage per hit (or per projectile).")]
    [SerializeField] private float damage = 10f;
    [PowerRange(0.05f, 10f, 2f)]
    [Tooltip("Seconds between two attacks.")]
    [SerializeField] private float attackCooldown = 1f;
    [Range(0f, 3f)]
    [Tooltip("Melee: windup before the hit lands (telegraph duration). Ranged: how long the shooter stays still.")]
    [SerializeField] private float attackHitDelay = 0.3f;
    [Range(0f, 3f)]
    [Tooltip("How long the Attack animation state is held before going back to Run/Idle.")]
    [SerializeField] private float attackAnimationDuration = 0.7f;
    [Range(0f, 1f)]
    [Tooltip("Speed multiplier while attacking (0 = rooted).")]
    [SerializeField] private float attackMoveSpeedMultiplier = 0.25f;
    [Tooltip("Hit zone length = Attack Range x this, so a target stepping back a little during the windup still gets hit.")]
    [Range(1f, 3f)]
    [SerializeField] private float hitRangeTolerance = 1.5f;

    [SerializeField] private bool isRanged;
    [SerializeField] private GameObject projectilePrefab;
    [Range(0.5f, 40f)]
    [Tooltip("Projectile speed in units per second.")]
    [SerializeField] private float projectileSpeed = 12f;
    [Range(0.5f, 40f)]
    [Tooltip("Distance after which the projectile disappears.")]
    [SerializeField] private float projectileMaxDistance = 8f;
    [Range(0f, 5f)]
    [Tooltip("Height of the lobbed arc (0 = straight shot).")]
    [SerializeField] private float projectileArcHeight = 0.8f;
    [Tooltip("Horde runs straight at the nearest player. Ranged stops at Attack Range and backs off below Keep Away Distance. Encircle circles to the player's side or back before charging, and only circles again after landing an attack.")]
    [SerializeField] private EnemyBehaviour behaviour;
    [Range(0f, 20f)]
    [Tooltip("Ranged only: closer than this to its target, the enemy backs away instead of shooting.")]
    [SerializeField] private float keepAwayDistance = 3f;
    [Range(0.1f, 1.5f)]
    [Tooltip("Ranged only: speed multiplier while backing away.")]
    [SerializeField] private float retreatSpeedMultiplier = 0.8f;
    [Range(0f, 8f)]
    [Tooltip("Ranged only: the enemy steps away from other enemies closer than this, also sideways between shots, so shooters spread out instead of packing.")]
    [SerializeField] private float rangedSpacing = 2.5f;
    [Range(1f, 15f)]
    [Tooltip("Encircle only: distance at which the enemy circles around its target; the target breaking further away after a hit sends it circling again.")]
    [SerializeField] private float encircleRadius = 4f;
    [Range(1f, 30f)]
    [Tooltip("Encircle only: closer than this to its target, the enemy stops chasing straight and circles to its place (always at least Encircle Radius + 2).")]
    [SerializeField] private float encircleJoinDistance = 9f;
    [Range(5f, 90f)]
    [Tooltip("Encircle only: within this many degrees of its place around the target, the enemy charges.")]
    [SerializeField] private float encircleSlotTolerance = 20f;
    [Range(0.5f, 10f)]
    [Tooltip("Encircle only: the enemy charges anyway after circling this long, so a turning target cannot keep it circling.")]
    [SerializeField] private float encircleMaxFlankTime = 2.5f;
    [Range(0f, 1f)]
    [Tooltip("Encircle only: chance after each attack that the enemy backs off and circles again, even if the target stays close.")]
    [SerializeField] private float encircleRegroupChance = 0.5f;
    [Range(0.05f, 2f)]
    [Tooltip("Seconds between target re-evaluations.")]
    [SerializeField] private float retargetInterval = 0.25f;
    [Range(0.02f, 1f)]
    [Tooltip("Seconds between steering updates.")]
    [SerializeField] private float steerInterval = 0.1f;
    [Range(0f, 30f)]
    [Tooltip("Below this distance the enemy runs straight at the target instead of following the flow field.")]
    [SerializeField] private float directChaseRange = 8f;
    [Range(0.05f, 2f)]
    [Tooltip("Seconds between line-of-sight checks.")]
    [SerializeField] private float lineOfSightInterval = 0.3f;
    [Range(5f, 100f)]
    [Tooltip("Farther than this from every player, the enemy is teleported back near them.")]
    [SerializeField] private float maxDistanceFromPlayers = 40f;
    [Range(0.25f, 10f)]
    [Tooltip("Seconds between leash distance checks.")]
    [SerializeField] private float leashCheckInterval = 2f;

    [Tooltip("Variant of the base Enemy prefab with this type's sprite and Animator.")]
    [SerializeField] private GameObject enemyPrefab;
    [Tooltip("Aseprite controllers name states after clip tags (e.g. \"Shoot\", \"Walk\"), so they are set per kit.")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string runStateName = "Run";
    [SerializeField] private string attackStateName = "Attack";

    public int Tier => tier;
    public float MaxHealth => maxHealth;
    public int XpReward => xpReward;
    public int ScoreReward => scoreReward;
    public float MoveSpeed => moveSpeed;
    public EnemyAttackShape AttackShape => attackShape;
    public float AttackRange => attackRange;
    public float AttackAngle => attackAngle;
    public float AttackWidth => attackWidth;
    public Vector2 AttackOrigin => attackOrigin;
    public float HitReach => attackRange * hitRangeTolerance;
    public float Damage => damage;
    public float AttackCooldown => attackCooldown;
    public float AttackHitDelay => attackHitDelay;
    public float AttackAnimationDuration => attackAnimationDuration;
    public float AttackMoveSpeedMultiplier => attackMoveSpeedMultiplier;
    public bool IsRanged => isRanged;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileMaxDistance => projectileMaxDistance;
    public float ProjectileArcHeight => projectileArcHeight;
    public float RetargetInterval => retargetInterval;
    public EnemyBehaviour Behaviour => behaviour;
    public float KeepAwayDistance => keepAwayDistance;
    public float RetreatSpeedMultiplier => retreatSpeedMultiplier;
    public float RangedSpacing => rangedSpacing;
    public float EncircleRadius => encircleRadius;
    public float EncircleJoinDistance => Mathf.Max(encircleJoinDistance, encircleRadius + JoinDistanceMargin);
    public float EncircleSlotTolerance => encircleSlotTolerance;
    public float EncircleMaxFlankTime => encircleMaxFlankTime;
    public float EncircleRegroupChance => encircleRegroupChance;
    public float SteerInterval => steerInterval;
    public float DirectChaseRange => directChaseRange;
    public float LineOfSightInterval => lineOfSightInterval;
    public float MaxDistanceFromPlayers => maxDistanceFromPlayers;
    public float LeashCheckInterval => leashCheckInterval;
    public GameObject EnemyPrefab => enemyPrefab;
    public string IdleStateName => idleStateName;
    public string RunStateName => runStateName;
    public string AttackStateName => attackStateName;

}
