using UnityEngine;

// Enemy's thrown shot (same shape as the player's ArrowProjectile): flies straight to its max range and deals its damage on contact with a living player's body, or is absorbed by a shield in its path.
[RequireComponent(typeof(Rigidbody2D))]
public class RangedAttackVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 16f;
    [SerializeField] private GameObject impactPrefab;

    private Rigidbody2D rb;
    private float damage;
    private float maxDistance;
    private float arcHeight;
    private bool isAuthoritative;
    private bool resolved;
    private float frameTimer;
    private int frameIndex;
    private Vector3 startPosition;
    private Vector3 spriteBaseLocalPosition;

    // isAuthoritative is true only on the server/offline copy, which alone applies shield damage; player hits are judged per machine.
    public static void Show(GameObject prefab, Vector3 from, Vector2 direction, float speed, float maxDistance, float arcHeight, float damage, bool isAuthoritative)
    {
        if (prefab == null || speed <= 0f || maxDistance <= 0f || direction.sqrMagnitude < 0.0001f)
            return;

        GameObject go = Instantiate(prefab, from, Quaternion.identity);
        RangedAttackVisual visual = go.GetComponent<RangedAttackVisual>();
        if (visual == null)
        {
            Destroy(go);
            return;
        }

        visual.Init(direction.normalized * speed, maxDistance, arcHeight, damage, isAuthoritative);
    }

    private void Init(Vector2 velocity, float maxDist, float arc, float damageValue, bool authoritative)
    {
        maxDistance = maxDist;
        arcHeight = arc;
        damage = damageValue;
        isAuthoritative = authoritative;
        startPosition = transform.position;
        rb.linearVelocity = velocity;
    }

    // Re-asserts the physics setup this needs regardless of what the prefab's Inspector values happen to be, since a kinematic+continuous+trigger setup is load-bearing for hitting the shield.
    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0 && spriteRenderer != null)
            spriteRenderer.sprite = frames[0];
        if (spriteRenderer != null)
            spriteBaseLocalPosition = spriteRenderer.transform.localPosition;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    // Travels via the Rigidbody2D's own velocity (set once in Init) instead of a per-frame Transform/MovePosition write, matching ArrowProjectile; the arc lob is a visual-only offset on the child sprite so it doesn't disturb the physics path the hitbox travels along.
    void Update()
    {
        float traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= maxDistance)
        {
            Impact();
            return;
        }

        if (spriteRenderer != null && arcHeight > 0f)
        {
            float fraction = Mathf.Clamp01(traveled / maxDistance);
            Vector3 offset = spriteBaseLocalPosition;
            offset.y += Mathf.Sin(fraction * Mathf.PI) * arcHeight;
            spriteRenderer.transform.localPosition = offset;
        }

        if (frames == null || frames.Length <= 1 || spriteRenderer == null)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / frameRate;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }
    }

    // Shields absorb the shot (server-resolved); a player's hit is judged by that player's own machine, never trigger zones (e.g. revive/interaction areas).
    void OnTriggerEnter2D(Collider2D other)
    {
        if (resolved)
            return;

        ShieldHoldFX shield = other.GetComponent<ShieldHoldFX>();
        if (shield != null)
        {
            resolved = true;
            if (isAuthoritative && shield.Owner != null)
                shield.Owner.ApplyShieldDamage(damage);
            Impact();
            return;
        }

        if (other.isTrigger)
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead)
            return;

        resolved = true;
        if (player.IsLocallyControlled())
            player.TakeDamage(damage);
        Impact();
    }

    // Ends the shot, bursting its impact FX at the same spot and scale when the prefab has one.
    private void Impact()
    {
        if (impactPrefab != null)
            Instantiate(impactPrefab, transform.position, Quaternion.identity).transform.localScale = transform.localScale;
        Destroy(gameObject);
    }
}
