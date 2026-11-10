using System.Collections.Generic;
using UnityEngine;

// One-shot flipbook that strikes everything in its hitbox once, on one frame; only the attacker's copy deals damage. A dive waits out its air time first.
public class StrikeFX : MonoBehaviour, IAttackFX
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D hitbox;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 20f;
    [SerializeField] private int hitFrame = 3;

    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<IDamageable> struck = new HashSet<IDamageable>();
    private GameObject owner;
    private float damage;
    private bool hasHitbox;
    private bool hitDone;
    private float elapsed;
    private bool pushesOnStrike;

    public void Init(GameObject attacker, bool hasHitbox, AbilityKit ability, float damageMultiplier)
    {
        owner = attacker;
        this.hasHitbox = hasHitbox;
        damage = ability.Damage * damageMultiplier;
        spriteRenderer.color = CharacterCatalog.FxColorOf(attacker);
        FxGlow.Setup(gameObject, attacker, ability);
        if (ability.Kind != AbilityKind.Dive)
            return;
        pushesOnStrike = true;
        spriteRenderer.enabled = false;
        elapsed = hitFrame / frameRate - ability.DiveAirSeconds;
    }

    private bool IsOwner(IDamageable damageable)
    {
        MonoBehaviour behaviour = damageable as MonoBehaviour;

        return behaviour != null && behaviour.gameObject == owner;
    }

    // A shield's wall is not its owner, and a body with several colliders is only struck once.
    private void Strike(Collider2D other)
    {
        IDamageable damageable;

        if (other.GetComponent<ShieldHoldFX>() != null)
            return;
        damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || IsOwner(damageable) || Player.IsProtectedAlly(owner, damageable) || !struck.Add(damageable))
            return;
        if (Player.TryBlockAttack(owner, damageable, damage))
            return;
        Player.DealAttackDamage(owner, damageable, damage);
        if (struck.Count == 1)
            SlashAttackFX.NotifyHitLanded(owner, other.ClosestPoint(transform.position));
    }

    private void StrikeAlongLine()
    {
        hitDone = true;
        if (!hasHitbox)
            return;
        hitbox.Overlap(ContactFilter2D.noFilter, overlaps);
        foreach (Collider2D other in overlaps)
            Strike(other);
        if (pushesOnStrike)
            owner.GetComponent<Player>().RequestDivePush(hitbox.bounds.center, hitbox.bounds.size);
    }

    void Awake()
    {
        spriteRenderer.sprite = frames[0];
    }

    void Update()
    {
        int frame;

        elapsed += Time.deltaTime;
        if (elapsed < 0f)
            return;
        spriteRenderer.enabled = true;
        frame = Mathf.FloorToInt(elapsed * frameRate);
        if (frame >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }
        spriteRenderer.sprite = frames[frame];
        if (!hitDone && frame >= hitFrame)
            StrikeAlongLine();
    }
}
