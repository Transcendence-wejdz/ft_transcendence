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
    [Tooltip("Distance from the FX origin where the FX (and its hitbox) spawns, along the aim.")]
    [SerializeField] private float attackFxDistance = 1f;
    [Tooltip("Point the FX distance is measured from, relative to the player's feet; X mirrors when aiming left.")]
    [SerializeField] private Vector2 attackFxOrigin;
    [Tooltip("Uses the center of the aim indicator ring as the FX origin instead of FX Origin.")]
    [SerializeField] private bool attackFxFromAimIndicator;
    [Tooltip("Instant fires on press. Hold Fixed also previews the hitbox at FX Distance while held (autofire keeps going). Hold Free lets the player place each shot within Max Range (mouse distance, or right stick tilt).")]
    [SerializeField] private AbilityAimMode attackAimMode;
    [Range(0.5f, 20f)]
    [Tooltip("Farthest a Hold Free attack can be placed from its FX origin.")]
    [SerializeField] private float attackMaxRange = 6f;
    [Tooltip("Gamepad only, when the player's Aim Assist setting is on: bends each shot toward the target closest to the aim within Aim Assist Angle.")]
    [SerializeField] private bool attackAimAssist = true;
    [Range(0f, 60f)]
    [Tooltip("Widest angle, in degrees either side of the aim, a target can be pulled from.")]
    [SerializeField] private float attackAimAssistAngle = 10f;
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
    [Tooltip("Distance from the FX origin where the FX (and its hitbox) spawns, along the aim.")]
    [SerializeField] private float specialFxDistance = 1f;
    [Tooltip("Point the FX distance is measured from, relative to the player's feet; X mirrors when aiming left.")]
    [SerializeField] private Vector2 specialFxOrigin;
    [Tooltip("Uses the center of the aim indicator ring as the FX origin instead of FX Origin.")]
    [SerializeField] private bool specialFxFromAimIndicator;
    [Tooltip("Instant casts on press. Hold Fixed previews the hitbox at FX Distance and casts on release. Hold Free also lets the player place it within Max Range (mouse distance, or right stick tilt).")]
    [SerializeField] private AbilityAimMode specialAimMode;
    [Range(0.5f, 20f)]
    [Tooltip("Farthest a Hold Free special can be placed from its FX origin.")]
    [SerializeField] private float specialMaxRange = 6f;
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
    [Tooltip("Distance from the FX origin where the FX (and its hitbox) spawns, along the aim.")]
    [SerializeField] private float ultimateFxDistance = 1f;
    [Tooltip("Point the FX distance is measured from, relative to the player's feet; X mirrors when aiming left.")]
    [SerializeField] private Vector2 ultimateFxOrigin;
    [Tooltip("Uses the center of the aim indicator ring as the FX origin instead of FX Origin.")]
    [SerializeField] private bool ultimateFxFromAimIndicator;
    [Tooltip("Instant casts on press. Hold Fixed previews the hitbox at FX Distance and casts on release. Hold Free also lets the player place it within Max Range (mouse distance, or right stick tilt). Ignored with a shield.")]
    [SerializeField] private AbilityAimMode ultimateAimMode;
    [Range(0.5f, 20f)]
    [Tooltip("Farthest a Hold Free ultimate can be placed from its FX origin.")]
    [SerializeField] private float ultimateMaxRange = 6f;
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
    // Holds the Ultimate animation while the shield is up; needs a "ShieldHeld" bool on the class controller.
    [SerializeField] private bool shieldHoldAnimation;

    public float MaxHealth => maxHealth;
    public float HealthRegenPerSecond => healthRegenPerSecond;
    public float MaxMana => maxMana;
    public float ManaRegenPerSecond => manaRegenPerSecond;

    public float AttackCooldown => attackCooldown;
    public GameObject AttackFxPrefab => attackFxPrefab;
    public float AttackFxDistance => attackFxDistance;
    public Vector2 AttackFxOrigin => attackFxOrigin;
    public bool AttackFxFromAimIndicator => attackFxFromAimIndicator;
    public bool AttackAimAssist => attackAimAssist;
    public float AttackAimAssistAngle => attackAimAssistAngle;
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

    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileMaxDistance => projectileMaxDistance;
    public int PierceCount => pierceCount;

    public float SpecialCooldown => specialCooldown;
    public float SpecialManaCost => specialManaCost;
    public GameObject SpecialFxPrefab => specialFxPrefab;
    public float SpecialFxDistance => specialFxDistance;
    public Vector2 SpecialFxOrigin => specialFxOrigin;
    public bool SpecialFxFromAimIndicator => specialFxFromAimIndicator;
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
    public Vector2 UltimateFxOrigin => ultimateFxOrigin;
    public bool UltimateFxFromAimIndicator => ultimateFxFromAimIndicator;
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

    // Where an action's FX spawns relative to the player's feet: its origin (the aim indicator center, or FX Origin with X mirrored when aiming left) plus its distance along the aim, unless a placed distance overrides it.
    public Vector2 FxOffset(AbilityType ability, Vector2 aim, Vector2 aimIndicatorCenter, float distanceOverride = -1f)
    {
        (Vector2 origin, bool fromIndicator, float distance) = ability switch
        {
            AbilityType.Special => (specialFxOrigin, specialFxFromAimIndicator, specialFxDistance),
            AbilityType.Ultimate => (ultimateFxOrigin, ultimateFxFromAimIndicator, ultimateFxDistance),
            _ => (attackFxOrigin, attackFxFromAimIndicator, attackFxDistance),
        };
        if (fromIndicator)
            origin = aimIndicatorCenter;
        else if (aim.x < -0.01f)
            origin.x = -origin.x;
        return origin + aim * (distanceOverride >= 0f ? distanceOverride : distance);
    }

    // The shield ultimate has its own hold, so its aim mode is ignored.
    public AbilityAimMode AimMode(AbilityType ability) => ability switch
    {
        AbilityType.Special => specialAimMode,
        AbilityType.Ultimate => hasShield ? AbilityAimMode.Instant : ultimateAimMode,
        _ => attackAimMode,
    };

    public float MaxRange(AbilityType ability) => ability switch
    {
        AbilityType.Special => specialMaxRange,
        AbilityType.Ultimate => ultimateMaxRange,
        _ => attackMaxRange,
    };

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
