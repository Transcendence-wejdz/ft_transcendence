using UnityEngine;

// Per-class stats (health, mana, dodge) and the AbilityKit used in each action slot.
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
    [SerializeField] private AbilityKit attack;
    [Tooltip("Specials this class can use; only the first one is used for now.")]
    [SerializeField] private AbilityKit[] specials = new AbilityKit[1];
    [Tooltip("Ultimates this class can use; only the first one is used for now.")]
    [SerializeField] private AbilityKit[] ultimates = new AbilityKit[1];

    public float MaxHealth => maxHealth;
    public float HealthRegenPerSecond => healthRegenPerSecond;
    public float MaxMana => maxMana;
    public float ManaRegenPerSecond => manaRegenPerSecond;

    public int DodgeCharges => dodgeCharges;
    public float DodgeRechargeSeconds => dodgeRechargeSeconds;
    public float DodgeDistance => dodgeDistance;
    public float DodgeDuration => dodgeDuration;

    private static AbilityKit First(AbilityKit[] abilities)
    {
        if (abilities == null || abilities.Length == 0)
            return null;
        return abilities[0];
    }

    // The action in a slot, null when the class has none there.
    public AbilityKit Ability(AbilityType ability)
    {
        if (ability == AbilityType.Special)
            return First(specials);
        if (ability == AbilityType.Ultimate)
            return First(ultimates);
        return attack;
    }
}
