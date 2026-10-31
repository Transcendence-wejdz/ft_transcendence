using System.Collections.Generic;
using UnityEngine;

// Local one-shot slash FX; only the attacker's copy keeps a live hitbox.
public class SlashAttackFX : MonoBehaviour, IAttackFX
{
    public static event System.Action<GameObject, Vector3> OnHitLanded;

    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 24f;

    private SpriteRenderer spriteRenderer;
    private Collider2D hitbox;
    private GameObject owner;
    private float damage;
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private int frameIndex;
    private float frameTimer;

    // Steps to the next frame; false once the flipbook is over.
    private bool AdvanceFrame()
    {
        frameIndex++;
        if (frameIndex >= frames.Length)
            return false;
        spriteRenderer.sprite = frames[frameIndex];
        return true;
    }

    // Already hit targets and the attacker itself never take the slash.
    private bool CanHit(IDamageable damageable)
    {
        MonoBehaviour behaviour;

        if (damageable == null || hitTargets.Contains(damageable))
            return false;
        behaviour = damageable as MonoBehaviour;
        return behaviour == null || behaviour.gameObject != owner;
    }

    // Follow-caster tracks the attacker for its whole lifetime instead of staying at its world spawn position.
    public void Init(GameObject attacker, bool hasHitbox, AbilityKit ability, float damageMultiplier)
    {
        owner = attacker;
        GetComponent<SpriteRenderer>().color = CharacterCatalog.FxColorOf(attacker);
        FxGlow.Setup(gameObject, attacker, ability);
        damage = ability.Damage * damageMultiplier;
        if (ability.FollowCaster)
            transform.SetParent(attacker.transform);
        if (!hasHitbox && hitbox != null)
            hitbox.enabled = false;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<Collider2D>();
        if (frames.Length > 0)
            spriteRenderer.sprite = frames[0];
    }

    void Update()
    {
        float frameDuration = 1f / frameRate;

        frameTimer += Time.deltaTime;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            if (!AdvanceFrame())
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (!CanHit(damageable))
            return;
        hitTargets.Add(damageable);
        if (Player.TryBlockAttack(owner, damageable, damage))
            return;
        damageable.RequestDamage(damage);
        OnHitLanded?.Invoke(owner, other.ClosestPoint(transform.position));
    }
}
