using UnityEngine;

// Phase 2 tuning: opening roar, shockwave after each slam and burning trail behind charges.
public partial class BossKit
{
    [Range(0f, 5f)]
    [Tooltip("Recovery after a combo once enraged, before the enrage tempo.")]
    [SerializeField] private float enrageRecoveryDuration = 0.55f;
    [Range(0f, 5f)]
    [Tooltip("Roar: invulnerable windup before the push. With Roar Active Duration, the whole invulnerable time.")]
    [SerializeField] private float roarWindup = 1.2f;
    [Range(0f, 5f)]
    [Tooltip("Roar: invulnerable time after the push.")]
    [SerializeField] private float roarActiveDuration = 0.8f;
    [Range(0.1f, 20f)]
    [Tooltip("Roar: players inside this radius are pushed away.")]
    [SerializeField] private float roarRadius = 8f;
    [Range(0f, 8f)]
    [Tooltip("Roar: push speed in units per second (players cap pushes at 8).")]
    [SerializeField] private float roarPushSpeed = 8f;
    [Range(0f, 2f)]
    [Tooltip("Roar: push duration.")]
    [SerializeField] private float roarPushDuration = 0.6f;
    [Range(0f, 5f)]
    [Tooltip("Shockwave: windup after the slam, before the chain and enrage shortenings.")]
    [SerializeField] private float slamWaveWindup = 1.2f;
    [Range(0.1f, 20f)]
    [Tooltip("Shockwave radius around the body center.")]
    [SerializeField] private float slamWaveRadius = 8f;
    [PowerRange(0f, 500f, 2f)]
    [Tooltip("Damage to each player inside the shockwave.")]
    [SerializeField] private float slamWaveDamage = 50f;
    [SerializeField] private GameObject firePatchPrefab;
    [Range(0.2f, 5f)]
    [Tooltip("Distance between two fire patches of a charge.")]
    [SerializeField] private float fireSpacing = 1.2f;
    [Range(0.5f, 20f)]
    [Tooltip("Seconds a fire patch stays on the ground.")]
    [SerializeField] private float fireLifetime = 4f;
    [PowerRange(0f, 200f, 2f)]
    [Tooltip("Damage per second while standing in the fire; overlapping patches don't stack.")]
    [SerializeField] private float fireDamagePerSecond = 20f;

    public float EnrageRecoveryDuration => enrageRecoveryDuration;
    public float RoarWindup => roarWindup;
    public float RoarActiveDuration => roarActiveDuration;
    public float RoarRadius => roarRadius;
    public float RoarPushSpeed => roarPushSpeed;
    public float RoarPushDuration => roarPushDuration;
    public float SlamWaveWindup => slamWaveWindup;
    public float SlamWaveRadius => slamWaveRadius;
    public float SlamWaveDamage => slamWaveDamage;
    public GameObject FirePatchPrefab => firePatchPrefab;
    public float FireSpacing => fireSpacing;
    public float FireLifetime => fireLifetime;
    public float FireDamagePerSecond => fireDamagePerSecond;
}
