using UnityEngine;

// Enemy shot: flies to its max range, damages the first living player it touches, or is stopped by a shield it meets from the front.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    private const float MinDirectionSqr = 0.0001f;

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

    private bool IsAnimated => frames != null && frames.Length > 1 && spriteRenderer != null;

    private void Init(Vector2 velocity, float maxDist, float arc, float damageValue, bool authoritative)
    {
        maxDistance = maxDist;
        arcHeight = arc;
        damage = damageValue;
        isAuthoritative = authoritative;
        startPosition = transform.position;
        rb.linearVelocity = velocity;
    }

    private void InitSprite()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;
        if (frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[0];
        spriteBaseLocalPosition = spriteRenderer.transform.localPosition;
    }

    // Forces a kinematic, continuous trigger body whatever the prefab says, or shields would be missed.
    private void InitBody()
    {
        Collider2D col = GetComponent<Collider2D>();

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f;
        if (col != null)
            col.isTrigger = true;
    }

    // The arc is only a visual offset on the child sprite (never the root, which would pin the shot in place).
    private void UpdateArc(float traveled)
    {
        Vector3 offset = spriteBaseLocalPosition;
        float fraction;

        if (spriteRenderer == null || arcHeight <= 0f || spriteRenderer.transform == transform)
            return;
        fraction = Mathf.Clamp01(traveled / maxDistance);
        offset.y += Mathf.Sin(fraction * Mathf.PI) * arcHeight;
        spriteRenderer.transform.localPosition = offset;
    }

    private void UpdateFrames()
    {
        float frameDuration = 1f / frameRate;

        frameTimer += Time.deltaTime;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }
    }

    // Ends the shot, bursting its impact FX at the same spot and scale when the prefab has one.
    private void Impact()
    {
        GameObject impact;

        if (impactPrefab != null)
        {
            impact = Instantiate(impactPrefab, transform.position, Quaternion.identity);
            impact.transform.localScale = transform.localScale;
        }
        Destroy(gameObject);
    }

    // Checks this physics step's path ahead, so a fast shot never skips the wall; only the server/offline copy damages the shield.
    private void CheckShieldBlock()
    {
        Vector2 from = rb.position;
        ShieldHoldFX shield = Player.FindBlockingShield(from, from + rb.linearVelocity * Time.fixedDeltaTime);

        if (shield == null)
            return;
        resolved = true;
        if (isAuthoritative)
            shield.Owner.ApplyShieldDamage(damage);
        Impact();
    }

    // A player hit is judged on that player's machine; trigger zones are ignored.
    private void TryHitPlayer(Collider2D other)
    {
        Player player;

        if (other.isTrigger)
            return;
        player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead)
            return;
        resolved = true;
        if (player.IsLocallyControlled())
            player.TakeDamage(damage);
        Impact();
    }

    // isAuthoritative is true only on the server/offline copy, which alone applies shield damage; player hits are judged per machine.
    public static void Fire(GameObject prefab, Vector3 from, Vector2 direction, float speed, float maxDistance, float arcHeight, float damage, bool isAuthoritative)
    {
        GameObject go;
        EnemyProjectile projectile;

        if (prefab == null || speed <= 0f || maxDistance <= 0f || direction.sqrMagnitude < MinDirectionSqr)
            return;
        go = Instantiate(prefab, from, Quaternion.identity);
        projectile = go.GetComponent<EnemyProjectile>();
        if (projectile == null)
        {
            Destroy(go);
            return;
        }
        projectile.Init(direction.normalized * speed, maxDistance, arcHeight, damage, isAuthoritative);
    }

    void Awake()
    {
        InitSprite();
        InitBody();
    }

    // Moves by Rigidbody2D velocity, stopped behind the Game Over screen; the hitbox flies straight.
    void Update()
    {
        float traveled;

        if (GameOverCheck.IsRunOver)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= maxDistance)
        {
            Impact();
            return;
        }
        UpdateArc(traveled);
        if (IsAnimated)
            UpdateFrames();
    }

    void FixedUpdate()
    {
        if (!resolved && !GameOverCheck.IsRunOver)
            CheckShieldBlock();
    }

    // The shield wall is a child of its owner, so touching it must never count as hitting that player.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (resolved || GameOverCheck.IsRunOver || other.GetComponent<ShieldHoldFX>() != null)
            return;
        TryHitPlayer(other);
    }
}
