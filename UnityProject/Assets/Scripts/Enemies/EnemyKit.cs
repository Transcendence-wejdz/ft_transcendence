using UnityEngine;

public enum EnemyAttackShape { Cone, Lane, Circle }

// Per-enemy-type stats/combat tuning, so new enemy variants are pure data, not code.
[CreateAssetMenu(fileName = "EnemyKit", menuName = "Transcendence/Enemy Kit")]
public class EnemyKit : ScriptableObject
{
    [Range(1, 10)]
    [Tooltip("Difficulty tier: decides from which wave this enemy can spawn.")]
    [SerializeField] private int tier = 1;
    [PowerRange(1f, 2000f, 2f)]
    [Tooltip("Hit points.")]
    [SerializeField] private float maxHealth = 40f;
    [Range(0, 500)]
    [Tooltip("XP granted to the players when it dies.")]
    [SerializeField] private int xpReward = 10;
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

    // Frames/frame rate live on the projectile prefab itself (see ArrowProjectile for the player-side equivalent), not here.
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

    // Each enemy type is its own prefab variant (sprite/Animator baked in) of the shared base Enemy prefab, rather than one prefab re-skinned at spawn time.
    [SerializeField] private GameObject enemyPrefab;
    // Aseprite-imported controllers name states after the source clip tags (e.g. "Shoot", "Walk"), which don't all match - kept configurable instead of renaming the generated asset.
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string runStateName = "Run";
    [SerializeField] private string attackStateName = "Attack";

    public int Tier => tier;
    public float MaxHealth => maxHealth;
    public int XpReward => xpReward;
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
    public float HitRangeTolerance => hitRangeTolerance;
    public bool IsRanged => isRanged;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileMaxDistance => projectileMaxDistance;
    public float ProjectileArcHeight => projectileArcHeight;
    public float RetargetInterval => retargetInterval;
    public float SteerInterval => steerInterval;
    public float DirectChaseRange => directChaseRange;
    public float LineOfSightInterval => lineOfSightInterval;
    public float MaxDistanceFromPlayers => maxDistanceFromPlayers;
    public float LeashCheckInterval => leashCheckInterval;
    public GameObject EnemyPrefab => enemyPrefab;
    public string IdleStateName => idleStateName;
    public string RunStateName => runStateName;
    public string AttackStateName => attackStateName;

    // Attack origin in world space: the body center moved by attackOrigin, expressed in the swing's own frame.
    public Vector2 AttackOriginWorld(Vector2 bodyCenter, Vector2 direction)
    {
        Vector2 forward = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector2 left = new Vector2(-forward.y, forward.x);
        return bodyCenter + forward * attackOrigin.x + left * attackOrigin.y;
    }

    // The zone the telegraph shows, from the attack origin along the swing direction.
    public bool HitZoneContains(Vector2 bodyCenter, Vector2 direction, Vector2 point)
    {
        Vector2 toPoint = point - AttackOriginWorld(bodyCenter, direction);
        float reach = HitReach;
        switch (attackShape)
        {
            case EnemyAttackShape.Lane:
                Vector2 forward = direction.normalized;
                float along = Vector2.Dot(toPoint, forward);
                float side = Mathf.Abs(forward.x * toPoint.y - forward.y * toPoint.x);
                return along >= 0f && along <= reach && side <= attackWidth * 0.5f;
            case EnemyAttackShape.Circle:
                return toPoint.sqrMagnitude <= reach * reach;
            default:
                return toPoint.sqrMagnitude <= reach * reach && Vector2.Angle(direction, toPoint) <= attackAngle * 0.5f;
        }
    }

    public void ShowTelegraph(Transform anchor, Vector2 bodyCenter, Vector2 direction, float duration)
    {
        Vector2 origin = AttackOriginWorld(bodyCenter, direction);
        switch (attackShape)
        {
            case EnemyAttackShape.Lane:
                AttackTelegraph.ShowLane(anchor, origin, direction, HitReach, attackWidth, duration);
                break;
            case EnemyAttackShape.Circle:
                AttackTelegraph.Show(anchor, origin, Vector2.right, 360f, HitReach, duration);
                break;
            default:
                AttackTelegraph.Show(anchor, origin, direction, attackAngle, HitReach, duration);
                break;
        }
    }
}
