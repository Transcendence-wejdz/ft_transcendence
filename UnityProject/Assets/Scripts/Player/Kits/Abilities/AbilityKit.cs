using UnityEngine;

// One player action (Attack, Special or Ultimate): its kind first, then cost, FX placement, aim, damage, move lock, glow and the kind's own settings; ClassKit references one per slot.
[CreateAssetMenu(fileName = "Ability", menuName = "Transcendence/Ability Kit")]
public class AbilityKit : ScriptableObject
{
    [Tooltip("Basic spawns its FX, Projectile flies it along the aim, Shield raises a held wall (Ultimate slot only).")]
    [SerializeField] private AbilityKind kind;
    [PowerRange(0.05f, 120f, 2f)]
    [Tooltip("Seconds before the action can be used again.")]
    [SerializeField] private float cooldown = 0.5f;
    [Range(0f, 300f)]
    [Tooltip("Mana spent on use (ignored on the Attack slot).")]
    [SerializeField] private float manaCost;
    [SerializeField] private GameObject fxPrefab;
    [Tooltip("HUD slot icon; empty keeps the slot's default icon.")]
    [SerializeField] private Sprite icon;
    [Range(0f, 10f)]
    [Tooltip("Distance from the FX origin where the FX (and its hitbox) spawns, along the aim.")]
    [SerializeField] private float fxDistance = 1f;
    [Tooltip("Point the FX distance is measured from, relative to the player's feet; X mirrors when aiming left.")]
    [SerializeField] private Vector2 fxOrigin;
    [Tooltip("Uses the center of the aim indicator ring as the FX origin instead of FX Origin.")]
    [SerializeField] private bool fxFromAimIndicator;
    [Tooltip("Instant fires on press. Hold Fixed previews the hitbox at FX Distance while held (an Attack keeps autofiring, others cast on release). Hold Free also lets the player place it within Max Range (mouse distance, or right stick tilt).")]
    [SerializeField] private AbilityAimMode aimMode;
    [Tooltip("Hold modes only: draws the hitbox preview while held. The player's preview setting can still hide it.")]
    [SerializeField] private bool showHitboxPreview = true;
    [Range(0.5f, 20f)]
    [Tooltip("Farthest a Hold Free action can be placed from its FX origin.")]
    [SerializeField] private float maxRange = 6f;
    [Tooltip("Gamepad only, when the player's Aim Assist setting is on: bends each shot toward the target closest to the aim within Aim Assist Angle.")]
    [SerializeField] private bool aimAssist;
    [Range(0f, 60f)]
    [Tooltip("Widest angle, in degrees either side of the aim, a target can be pulled from.")]
    [SerializeField] private float aimAssistAngle = 10f;
    [PowerRange(0f, 300f, 2f)]
    [Tooltip("Damage per hit.")]
    [SerializeField] private float damage = 15f;
    [Range(0f, 3f)]
    [Tooltip("Seconds the movement stays slowed after using it.")]
    [SerializeField] private float moveLockDuration = 0.3f;
    [Range(0f, 1f)]
    [Tooltip("Speed multiplier during the move lock (0 = rooted).")]
    [SerializeField] private float moveSpeedMultiplier = 0.35f;
    [Range(0.1f, 5f)]
    [Tooltip("Playback speed of the action animation.")]
    [SerializeField] private float animSpeed = 1f;
    [Range(0.1f, 5f)]
    [Tooltip("Scale applied to the FX and its hitbox.")]
    [SerializeField] private float size = 1f;
    [SerializeField] private bool ignoreAimRotation;
    [SerializeField] private bool followCaster = true;
    [Range(0f, 5f)]
    [Tooltip("Peak intensity of the light under the FX (0 = no glow).")]
    [SerializeField] private float glowIntensity = 1.2f;
    [Range(0.5f, 3f)]
    [Tooltip("Size of the glow relative to the FX sprite.")]
    [SerializeField] private float glowSize = 1.1f;
    [Range(0f, 3f)]
    [Tooltip("Seconds of the glow flash (0 = stays lit while the FX lives).")]
    [SerializeField] private float glowDuration = 0.42f;
    [Range(0.5f, 40f)]
    [Tooltip("Projectile speed in units per second.")]
    [SerializeField] private float projectileSpeed = 12f;
    [Range(0.5f, 50f)]
    [Tooltip("Distance after which the projectile disappears.")]
    [SerializeField] private float projectileMaxDistance = 10f;
    [Range(1, 20)]
    [Tooltip("Enemies a projectile can pass through before it stops.")]
    [SerializeField] private int pierceCount = 1;
    [Tooltip("Set on a Hold kit: plays while held, and the damage grows until release.")]
    [SerializeField] private GameObject chargeFxPrefab;
    [Range(0f, 10f)]
    [Tooltip("Distance from the charge FX origin where the charge FX sits, along the aim.")]
    [SerializeField] private float chargeFxDistance = 1f;
    [Tooltip("Point the charge FX distance is measured from, relative to the player's feet; X mirrors when aiming left.")]
    [SerializeField] private Vector2 chargeFxOrigin;
    [Tooltip("Uses the center of the aim indicator ring as the charge FX origin instead of Charge FX Origin.")]
    [SerializeField] private bool chargeFxFromAimIndicator;
    [Range(0.2f, 10f)]
    [Tooltip("Seconds held to reach full charge.")]
    [SerializeField] private float chargeSeconds = 2f;
    [Range(1f, 10f)]
    [Tooltip("Damage multiplier at full charge; an immediate release deals the plain damage.")]
    [SerializeField] private float chargeDamageMultiplier = 3f;
    [Range(1f, 5f)]
    [Tooltip("Size multiplier of the charge FX and the shot at full charge; the start of the shot stays in place.")]
    [SerializeField] private float chargeSizeMultiplier = 2f;
    [Tooltip("Dive: played where the player takes off.")]
    [SerializeField] private GameObject takeoffFxPrefab;
    [Range(0.2f, 3f)]
    [Tooltip("Dive: seconds hidden and invulnerable before the impact.")]
    [SerializeField] private float diveAirSeconds = 0.6f;
    [Range(0f, 40f)]
    [Tooltip("Dive: speed enemies in the impact are thrown out at.")]
    [SerializeField] private float diveKnockbackForce = 14f;
    [Range(0f, 2f)]
    [Tooltip("Dive: seconds that throw lasts.")]
    [SerializeField] private float diveKnockbackDuration = 0.35f;
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
    [Tooltip("Holds the Ultimate animation while the shield is up; needs a \"ShieldHeld\" bool on the class controller.")]
    [SerializeField] private bool shieldHoldAnimation;

    public AbilityKind Kind => kind;
    public float Cooldown => cooldown;
    public float ManaCost => manaCost;
    public GameObject FxPrefab => fxPrefab;
    public Sprite Icon => icon;
    public GameObject TakeoffFxPrefab => takeoffFxPrefab;
    public float DiveAirSeconds => diveAirSeconds;
    public float DiveKnockbackForce => diveKnockbackForce;
    public float DiveKnockbackDuration => diveKnockbackDuration;
    public GameObject ChargeFxPrefab => chargeFxPrefab;
    public float ChargeSeconds => chargeSeconds;
    public float ChargeDamageMultiplier => chargeDamageMultiplier;
    public float ChargeSizeMultiplier => chargeSizeMultiplier;
    public float FxDistance => fxDistance;
    public Vector2 FxOrigin => fxOrigin;
    public bool FxFromAimIndicator => fxFromAimIndicator;
    // A shield has its own hold, so it always aims Instant.
    public AbilityAimMode AimMode
    {
        get
        {
            if (kind == AbilityKind.Shield)
                return AbilityAimMode.Instant;
            return aimMode;
        }
    }
    public bool ShowHitboxPreview => showHitboxPreview;
    public float MaxRange => maxRange;
    public bool AimAssist => aimAssist;
    public float AimAssistAngle => aimAssistAngle;
    public float Damage => damage;
    public float MoveLockDuration => moveLockDuration;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public float AnimSpeed => animSpeed;
    public float Size => size;
    public bool IgnoreAimRotation => ignoreAimRotation;
    public bool FollowCaster => followCaster;
    public float GlowIntensity => glowIntensity;
    public float GlowSize => glowSize;
    public float GlowDuration => glowDuration;

    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileMaxDistance => projectileMaxDistance;
    public int PierceCount => pierceCount;

    public float ShieldHoldDuration => shieldHoldDuration;
    public float ShieldHealth => shieldHealth;
    public float ShieldKnockbackForce => shieldKnockbackForce;
    public float ShieldKnockbackDuration => shieldKnockbackDuration;
    public float ShieldPushInterval => shieldPushInterval;
    public float ShieldTurnSpeed => shieldTurnSpeed;
    public bool ShieldHoldAnimation => shieldHoldAnimation;

    // Where the FX spawns relative to the player's feet: its origin (the aim indicator center, or FX Origin with X mirrored when aiming left) plus its distance along the aim, unless a placed distance overrides it.
    public Vector2 FxOffset(Vector2 aim, Vector2 aimIndicatorCenter, float distanceOverride = -1f)
    {
        float distance = fxDistance;

        if (distanceOverride >= 0f)
            distance = distanceOverride;
        return OffsetFrom(fxOrigin, fxFromAimIndicator, distance, aim, aimIndicatorCenter);
    }

    // Where the charge FX sits relative to the player's feet, placed like FxOffset from its own settings.
    public Vector2 ChargeFxOffset(Vector2 aim, Vector2 aimIndicatorCenter)
    {
        return OffsetFrom(chargeFxOrigin, chargeFxFromAimIndicator, chargeFxDistance, aim, aimIndicatorCenter);
    }

    private static Vector2 OffsetFrom(Vector2 origin, bool fromAimIndicator, float distance, Vector2 aim, Vector2 aimIndicatorCenter)
    {
        if (fromAimIndicator)
            origin = aimIndicatorCenter;
        else if (aim.x < -0.01f)
            origin.x = -origin.x;
        return origin + aim * distance;
    }
}
