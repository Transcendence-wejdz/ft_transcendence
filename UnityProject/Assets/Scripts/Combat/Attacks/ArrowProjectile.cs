using System.Collections.Generic;
using UnityEngine;

// Local one-shot ranged FX, travels independently and breaks on a shield met from the front; kinematic Rigidbody2D avoids tunneling through small colliders at speed.
[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour, IAttackFX
{
    private const float DefaultSpeed = 12f;
    private const float DefaultMaxDistance = 10f;
    private const int DefaultPierceCount = 1;

    [Tooltip("Optional renderer the frames animate on top of the base sprite; unset, they animate the main sprite.")]
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

    private bool IsAnimated => frames != null && frames.Length > 1;

    private SpriteRenderer GetAnimatedRenderer()
    {
        if (fxSpriteRenderer != null)
            return fxSpriteRenderer;
        return GetComponent<SpriteRenderer>();
    }

    private void ReadAbility(AbilityKit ability)
    {
        speed = DefaultSpeed;
        maxDistance = DefaultMaxDistance;
        pierceCount = DefaultPierceCount;
        if (ability == null)
            return;
        speed = ability.ProjectileSpeed;
        maxDistance = ability.ProjectileMaxDistance;
        pierceCount = ability.PierceCount;
    }

    private void UpdateFrames()
    {
        float frameDuration = 1f / frameRate;

        frameTimer += Time.deltaTime;
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

    private bool IsOwnerOrMount(IDamageable damageable)
    {
        MonoBehaviour behaviour = damageable as MonoBehaviour;

        if (behaviour == null)
            return false;
        return behaviour.gameObject == owner || IsOwnerMount(behaviour.transform);
    }

    // Each new target uses up one pierce; the arrow despawns once none is left.
    private void HitTarget(IDamageable damageable)
    {
        if (!hitTargets.Add(damageable))
            return;
        if (hasHitbox)
            damageable.RequestDamage(damage);
        enemiesHit++;
        if (enemiesHit >= pierceCount)
            Destroy(gameObject);
    }

    // Checks this physics step's path ahead; a blocked arrow breaks, and the attacker's copy (the only one with a hitbox) damages the shield.
    private void CheckShieldBlock()
    {
        Vector2 from = rb.position;
        ShieldHoldFX shield = Player.FindBlockingShield(from, from + rb.linearVelocity * Time.fixedDeltaTime);

        if (shield == null)
            return;
        if (hasHitbox)
            shield.Owner.ApplyShieldDamage(damage);
        Destroy(gameObject);
    }

    public void Init(GameObject attacker, bool hasHitboxValue, AbilityKit ability, float damageMultiplier)
    {
        owner = attacker;
        hasHitbox = hasHitboxValue;
        GetAnimatedRenderer().color = CharacterCatalog.FxColorOf(attacker);
        FxGlow.Setup(gameObject, attacker, ability);
        damage = ability.Damage * damageMultiplier;
        ReadAbility(ability);
        rb.linearVelocity = transform.right * speed;
    }

    void Awake()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        animatedRenderer = GetAnimatedRenderer();
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
        if (IsAnimated)
            UpdateFrames();
    }

    void FixedUpdate()
    {
        CheckShieldBlock();
    }

    // Stops on the first non-damageable solid thing it hits, but pierces up to pierceCount damageables before despawning; trigger-only zones are ignored.
    void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable;

        if (other.isTrigger)
            return;
        damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            Destroy(gameObject);
            return;
        }
        if (!IsOwnerOrMount(damageable))
            HitTarget(damageable);
    }
}
