using System.Collections.Generic;
using UnityEngine;

// Local one-shot ranged FX, travels independently; kinematic Rigidbody2D avoids tunneling through small colliders at speed.
[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour, IAttackFX
{
    // Optional separate renderer so an animated FX can layer on top of the base sprite instead of replacing it; left unset, frames animate the main sprite.
    [SerializeField] private SpriteRenderer fxSpriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 24f;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private float fadeDuration = 0.15f;

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
    private float fadeTimer = -1f;
    private Color fadeColorAlpha;
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    public void Init(GameObject attacker, bool hasHitboxValue, ClassKit kit, AbilityType ability, float damageMultiplier)
    {
        owner = attacker;
        hasHitbox = hasHitboxValue;
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

    // Optional looping flipbook for projectiles with their own animated frames; a single assigned sprite with no frames array behaves exactly as before.
    void Update()
    {
        if (fadeTimer >= 0f)
            UpdateFade();
        else if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            StartFade();

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
            Impact();
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
        Impact();
    }

    // Ends the shot, bursting its impact FX at the same spot and scale when the prefab has one.
    private void Impact()
    {
        if (impactPrefab != null)
            Instantiate(impactPrefab, transform.position, Quaternion.identity).transform.localScale = transform.localScale;
        Destroy(gameObject);
    }

    private void StartFade()
    {
        fadeColorAlpha = animatedRenderer.color;
        fadeTimer = 0f;
    }

    private void UpdateFade()
    {
        fadeTimer += Time.deltaTime; 
        float alpha = 1f - (fadeTimer / fadeDuration);
        Debug.LogError("Hellloooooo: " + alpha);

        if (alpha <= 0) {
            Destroy(gameObject);
            return ;
        }

        fadeColorAlpha.a = alpha;
        animatedRenderer.color = fadeColorAlpha;
    }
    
}
