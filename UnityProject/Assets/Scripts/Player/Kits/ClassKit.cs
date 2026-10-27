using UnityEngine;
using UnityEngine.Serialization;

// Per-class stats and combat tuning: General (health, mana, dodge) then Actions, each of Attack/Special/Ultimate self-contained.
[CreateAssetMenu(fileName = "ClassKit", menuName = "Transcendence/Class Kit")]
public class ClassKit : ScriptableObject
{
    [PowerRange(1f, 1000f, 2f)]
    [Tooltip("Hit points.")]
    [SerializeField] private float maxHealth = 100f;
    [Range(0f, 20f)]
    [Tooltip("Health regenerated per second.")]
    [SerializeField] private float healthRegenPerSecond = 1f;
    [Range(0f, 300f)]
    [Tooltip("Mana pool.")]
    [SerializeField] private float maxMana = 50f;
    [Range(0f, 30f)]
    [Tooltip("Mana regenerated per second.")]
    [SerializeField] private float manaRegenPerSecond = 1f;
    [Range(0, 5)]
    [Tooltip("Dodges available before waiting for a recharge.")]
    [SerializeField] private int dodgeCharges = 2;
    [Range(0.1f, 20f)]
    [Tooltip("Seconds to refill one dodge charge.")]
    [SerializeField] private float dodgeRechargeSeconds = 3f;
    [Range(0f, 10f)]
    [Tooltip("Distance covered by one dodge.")]
    [SerializeField] private float dodgeDistance = 2.5f;
    [Range(0.05f, 1f)]
    [Tooltip("Duration of one dodge (invulnerable meanwhile).")]
    [SerializeField] private float dodgeDuration = 0.2f;
    [PowerRange(0.05f, 30f, 2f)]
    [Tooltip("Seconds before the action can be used again.")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private GameObject attackFxPrefab;
    [Range(0f, 10f)]
    [Tooltip("Distance from the player where the FX (and its hitbox) spawns, along the aim.")]
    [SerializeField] private float attackFxDistance = 1f;
    [FormerlySerializedAs("damage")]
    [PowerRange(0f, 300f, 2f)]
    [Tooltip("Damage per hit.")]
    [SerializeField] private float attackDamage = 15f;
    [Range(0f, 3f)]
    [Tooltip("Seconds the movement stays slowed after using it.")]
    [SerializeField] private float attackMoveLockDuration = 0.3f;
    [Range(0f, 1f)]
    [Tooltip("Speed multiplier during the move lock (0 = rooted).")]
    [SerializeField] private float attackMoveSpeedMultiplier = 0.35f;
    [Range(0.1f, 5f)]
    [Tooltip("Playback speed of the action animation.")]
    [SerializeField] private float attackAnimSpeed = 1f;
    [Range(0.1f, 5f)]
    [Tooltip("Scale applied to the FX and its hitbox.")]
    [SerializeField] private float attackSize = 1f;
    [SerializeField] private bool attackIgnoreAimRotation;
    [SerializeField] private bool attackFollowCaster = true;
    [Range(0f, 5f)]
    [Tooltip("Peak intensity of the light under the FX (0 = no glow).")]
    [SerializeField] private float attackGlowIntensity = 1.2f;
    [Range(0.5f, 3f)]
    [Tooltip("Size of the glow relative to the FX sprite.")]
    [SerializeField] private float attackGlowSize = 1.1f;
    [Range(0f, 3f)]
    [Tooltip("Seconds of the glow flash (0 = stays lit while the FX lives).")]
    [SerializeField] private float attackGlowDuration = 0.42f;
    [SerializeField] private bool hasProjectile;
    [Range(0.5f, 40f)]
    [Tooltip("Projectile speed in units per second.")]
    [SerializeField] private float projectileSpeed = 12f;
    [Range(0.5f, 50f)]
    [Tooltip("Distance after which the projectile disappears.")]
    [SerializeField] private float projectileMaxDistance = 10f;
    [Range(1, 20)]
    [Tooltip("Enemies a projectile can pass through before it stops.")]
    [SerializeField] private int pierceCount = 1;
    [PowerRange(0.05f, 30f, 2f)]
    [Tooltip("Seconds before the action can be used again.")]
    [SerializeField] private float specialCooldown = 2f;
    [Range(0f, 300f)]
    [Tooltip("Mana spent on use.")]
    [SerializeField] private float specialManaCost;
    [SerializeField] private GameObject specialFxPrefab;
    [Range(0f, 10f)]
    [Tooltip("Distance from the player where the FX (and its hitbox) spawns, along the aim.")]
    [SerializeField] private float specialFxDistance = 1f;
    [PowerRange(0f, 300f, 2f)]
    [Tooltip("Damage per hit.")]
    [SerializeField] private float specialDamage = 15f;
    [Range(0f, 3f)]
    [Tooltip("Seconds the movement stays slowed after using it.")]
    [SerializeField] private float specialMoveLockDuration = 0.3f;
    [Range(0f, 1f)]
    [Tooltip("Speed multiplier during the move lock (0 = rooted).")]
    [SerializeField] private float specialMoveSpeedMultiplier = 0.35f;
    [Range(0.1f, 5f)]
    [Tooltip("Playback speed of the action animation.")]
    [SerializeField] private float specialAnimSpeed = 1f;
    [Range(0.1f, 5f)]
    [Tooltip("Scale applied to the FX and its hitbox.")]
    [SerializeField] private float specialSize = 1f;
    [SerializeField] private bool specialIgnoreAimRotation;
    [SerializeField] private bool specialFollowCaster = true;
    [Range(0f, 5f)]
    [Tooltip("Peak intensity of the light under the FX (0 = no glow).")]
    [SerializeField] private float specialGlowIntensity = 1.2f;
    [Range(0.5f, 3f)]
    [Tooltip("Size of the glow relative to the FX sprite.")]
    [SerializeField] private float specialGlowSize = 1.1f;
    [Range(0f, 3f)]
    [Tooltip("Seconds of the glow flash (0 = stays lit while the FX lives).")]
    [SerializeField] private float specialGlowDuration = 0.42f;
    [PowerRange(0.05f, 30f, 2f)]
    [Tooltip("Seconds before the action can be used again.")]
    [SerializeField] private float ultimateCooldown = 5f;
    [Range(0f, 300f)]
    [Tooltip("Mana spent on use.")]
    [SerializeField] private float ultimateManaCost;
    [SerializeField] private GameObject ultimateFxPrefab;
    [Range(0f, 10f)]
    [Tooltip("Distance from the player where the FX (and its hitbox) spawns, along the aim.")]
    [SerializeField] private float ultimateFxDistance = 1f;
    [PowerRange(0f, 300f, 2f)]
    [Tooltip("Damage per hit.")]
    [SerializeField] private float ultimateDamage = 15f;
    [Range(0f, 3f)]
    [Tooltip("Seconds the movement stays slowed after using it.")]
    [SerializeField] private float ultimateMoveLockDuration = 0.3f;
    [Range(0f, 1f)]
    [Tooltip("Speed multiplier during the move lock (0 = rooted).")]
    [SerializeField] private float ultimateMoveSpeedMultiplier = 0.35f;
    [Range(0.1f, 5f)]
    [Tooltip("Playback speed of the action animation.")]
    [SerializeField] private float ultimateAnimSpeed = 1f;
    [Range(0.1f, 5f)]
    [Tooltip("Scale applied to the FX and its hitbox.")]
    [SerializeField] private float ultimateSize = 1f;
    [SerializeField] private bool ultimateIgnoreAimRotation;
    [SerializeField] private bool ultimateFollowCaster = true;
    [Range(0f, 5f)]
    [Tooltip("Peak intensity of the light under the FX (0 = no glow).")]
    [SerializeField] private float ultimateGlowIntensity = 1.2f;
    [Range(0.5f, 3f)]
    [Tooltip("Size of the glow relative to the FX sprite.")]
    [SerializeField] private float ultimateGlowSize = 1.1f;
    [Range(0f, 3f)]
    [Tooltip("Seconds of the glow flash (0 = stays lit while the FX lives).")]
    [SerializeField] private float ultimateGlowDuration = 0.42f;
    [SerializeField] private bool hasShield;
    [Range(0.5f, 30f)]
    [Tooltip("Max seconds the shield stays up before dropping on its own.")]
    [SerializeField] private float shieldHoldDuration = 10f;
    [PowerRange(1f, 2000f, 2f)]
    [Tooltip("Damage the shield absorbs before breaking.")]
    [SerializeField] private float shieldHealth = 150f;
    [Range(0f, 30f)]
    [Tooltip("Push strength on enemies in front of the shield.")]
    [SerializeField] private float shieldKnockbackForce = 8f;
    [Range(0f, 2f)]
    [Tooltip("How long a pushed enemy loses control.")]
    [SerializeField] private float shieldKnockbackDuration = 0.25f;
    [Range(0.05f, 3f)]
    [Tooltip("Seconds between two pushes while the shield is up.")]
    [SerializeField] private float shieldPushInterval = 0.5f;
    [Range(0f, 720f)]
    [Tooltip("Degrees per second the raised shield turns towards the aim.")]
    [SerializeField] private float shieldTurnSpeed = 120f;
    // When true, the character's Ultimate cast animation loops/holds instead of auto-returning to Idle/Walk, for as long as the shield is up - needs a "ShieldHeld" bool param on that class's Animator Controller to have any effect.
    [SerializeField] private bool shieldHoldAnimation;

    public float MaxHealth => maxHealth;
    public float HealthRegenPerSecond => healthRegenPerSecond;
    public float MaxMana => maxMana;
    public float ManaRegenPerSecond => manaRegenPerSecond;

    public float AttackCooldown => attackCooldown;
    public GameObject AttackFxPrefab => attackFxPrefab;
    public float AttackFxDistance => attackFxDistance;
    public float AttackDamage => attackDamage;
    public float AttackMoveLockDuration => attackMoveLockDuration;
    public float AttackMoveSpeedMultiplier => attackMoveSpeedMultiplier;
    public float AttackAnimSpeed => attackAnimSpeed;
    public float AttackSize => attackSize;
    public bool AttackIgnoreAimRotation => attackIgnoreAimRotation;
    public bool AttackFollowCaster => attackFollowCaster;
    public float AttackGlowIntensity => attackGlowIntensity;
    public float AttackGlowSize => attackGlowSize;
    public float AttackGlowDuration => attackGlowDuration;

    public bool HasProjectile => hasProjectile;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileMaxDistance => projectileMaxDistance;
    public int PierceCount => pierceCount;

    public float SpecialCooldown => specialCooldown;
    public float SpecialManaCost => specialManaCost;
    public GameObject SpecialFxPrefab => specialFxPrefab;
    public float SpecialFxDistance => specialFxDistance;
    public float SpecialDamage => specialDamage;
    public float SpecialMoveLockDuration => specialMoveLockDuration;
    public float SpecialMoveSpeedMultiplier => specialMoveSpeedMultiplier;
    public float SpecialAnimSpeed => specialAnimSpeed;
    public float SpecialSize => specialSize;
    public bool SpecialIgnoreAimRotation => specialIgnoreAimRotation;
    public bool SpecialFollowCaster => specialFollowCaster;
    public float SpecialGlowIntensity => specialGlowIntensity;
    public float SpecialGlowSize => specialGlowSize;
    public float SpecialGlowDuration => specialGlowDuration;

    public float UltimateCooldown => ultimateCooldown;
    public float UltimateManaCost => ultimateManaCost;
    public GameObject UltimateFxPrefab => ultimateFxPrefab;
    public float UltimateFxDistance => ultimateFxDistance;
    public float UltimateDamage => ultimateDamage;
    public float UltimateMoveLockDuration => ultimateMoveLockDuration;
    public float UltimateMoveSpeedMultiplier => ultimateMoveSpeedMultiplier;
    public float UltimateAnimSpeed => ultimateAnimSpeed;
    public float UltimateSize => ultimateSize;
    public bool UltimateIgnoreAimRotation => ultimateIgnoreAimRotation;
    public bool UltimateFollowCaster => ultimateFollowCaster;
    public float UltimateGlowIntensity => ultimateGlowIntensity;
    public float UltimateGlowSize => ultimateGlowSize;
    public float UltimateGlowDuration => ultimateGlowDuration;

    public bool HasShield => hasShield;
    public float ShieldHoldDuration => shieldHoldDuration;
    public float ShieldHealth => shieldHealth;
    public float ShieldKnockbackForce => shieldKnockbackForce;
    public float ShieldKnockbackDuration => shieldKnockbackDuration;
    public float ShieldPushInterval => shieldPushInterval;
    public float ShieldTurnSpeed => shieldTurnSpeed;
    public bool ShieldHoldAnimation => shieldHoldAnimation;
    public int DodgeCharges => dodgeCharges;
    public float DodgeRechargeSeconds => dodgeRechargeSeconds;
    public float DodgeDistance => dodgeDistance;
    public float DodgeDuration => dodgeDuration;
}
