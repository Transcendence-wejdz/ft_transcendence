using System.Collections.Generic;
using UnityEngine;

// Local one-shot ranged FX, travels independently; kinematic Rigidbody2D avoids tunneling through small colliders at speed.
[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour, IAttackFX
{
    // Optional renderer the frames animate on top of the base sprite; unset, they animate the main sprite.
    [SerializeField] private SpriteRenderer fxSpriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 24f;

    private GameObject owner;
    private bool hasHitbox;
    private float damage;
    private float speed;
    private float maxDistance;
    private int pierceCount;
    private int enemiesHit;
    private Vector3 startPosition;
    private Rigidbody2D rb;
    private SpriteRenderer animatedRenderer;
    private float frameTimer;
    private int frameIndex;
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    public void Init(GameObject attacker, bool hasHitboxValue, ClassKit kit, AbilityType ability, float damageMultiplier)
    {
        owner = attacker;
        hasHitbox = hasHitboxValue;
        Color tint = CharacterCatalog.FxColorOf(attacker);
        (fxSpriteRenderer != null ? fxSpriteRenderer : GetComponent<SpriteRenderer>()).color = tint;
        FxGlow.Setup(gameObject, attacker, kit, ability);
        damage = ability switch
        {
            AbilityType.Special => kit.SpecialDamage,
            AbilityType.Ultimate => kit.UltimateDamage,
            _ => kit.AttackDamage,
        } * damageMultiplier;
        speed = kit.ProjectileSpeed;
        maxDistance = kit.ProjectileMaxDistance;
        pierceCount = kit.PierceCount;
        rb.linearVelocity = transform.right * speed;
    }

    void Awake()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        animatedRenderer = fxSpriteRenderer != null ? fxSpriteRenderer : GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0)
            animatedRenderer.sprite = frames[0];
    }

    // Loops the frames array when set; with no frames the assigned sprite stays static.
    void Update()
    {
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
            return;
        }

        if (frames == null || frames.Length <= 1)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / frameRate;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
            animatedRenderer.sprite = frames[frameIndex];
        }
    }

    // A rider is parented to the pig it rides, so its shots fly through that pig.
    private bool IsOwnerMount(Transform target)
    {
        return owner != null && owner.transform.parent == target;
    }

    // Stops on the first non-damageable solid thing it hits, but pierces up to pierceCount damageables before despawning; trigger-only zones are ignored.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger || other.GetComponent<ShieldHoldFX>() != null)
            return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            Destroy(gameObject);
            return;
        }

        var behaviour = damageable as MonoBehaviour;
        if (behaviour != null && (behaviour.gameObject == owner || IsOwnerMount(behaviour.transform)))
            return;

        if (!hitTargets.Add(damageable))
            return;

        if (hasHitbox)
            damageable.RequestDamage(damage);

        enemiesHit++;
        if (enemiesHit >= pierceCount)
            Destroy(gameObject);
    }
}
