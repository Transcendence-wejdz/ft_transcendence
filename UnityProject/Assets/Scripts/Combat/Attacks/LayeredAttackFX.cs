using System.Collections.Generic;
using UnityEngine;

// One-shot FX with synced back and front layers; AbilityKit FollowCaster decides if it follows the caster.
public class LayeredAttackFX : MonoBehaviour, IAttackFX
{
    [SerializeField] private SpriteRenderer frontRenderer;
    [SerializeField] private SpriteRenderer backRenderer;
    [SerializeField] private Sprite[] frontFrames;
    [SerializeField] private Sprite[] backFrames;
    [SerializeField] private float frameRate = 24f;

    private GameObject owner;
    private float damage;
    private int frontBaseOrder;
    private int frameIndex;
    private float frameTimer;
    private Collider2D hitbox;
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    // The front layer drives the length; the back layer only when there is no front.
    private int FrameCount
    {
        get
        {
            if (frontFrames != null)
                return frontFrames.Length;
            if (backFrames != null)
                return backFrames.Length;
            return 0;
        }
    }

    private static bool HasFrame(Sprite[] frames, int index)
    {
        return frames != null && index < frames.Length;
    }

    private static void SetFrame(SpriteRenderer renderer, Sprite[] frames, int index)
    {
        if (renderer != null && HasFrame(frames, index))
            renderer.sprite = frames[index];
    }

    private void SetTint(Color tint)
    {
        if (frontRenderer != null)
            frontRenderer.color = tint;
        if (backRenderer != null)
            backRenderer.color = tint;
    }

    // Sandwiches the attacker between the front/back layers using its current auto Y-sort order as the anchor.
    private void SortAroundAttacker(GameObject attacker)
    {
        SortingLayer_Auto attackerSorting = attacker.GetComponent<SortingLayer_Auto>();
        int baseOrder = 0;

        if (attackerSorting != null)
            baseOrder = attackerSorting.CurrentSortingOrder;
        frontBaseOrder = baseOrder + 1;
        if (frontRenderer != null)
            frontRenderer.sortingOrder = frontBaseOrder;
        if (backRenderer != null)
            backRenderer.sortingOrder = baseOrder - 1;
    }

    // Already hit targets and the attacker itself never take the hit.
    private bool CanHit(IDamageable damageable)
    {
        MonoBehaviour behaviour;

        if (damageable == null || hitTargets.Contains(damageable))
            return false;
        behaviour = damageable as MonoBehaviour;
        return behaviour == null || behaviour.gameObject != owner;
    }

    public void Init(GameObject attacker, bool hasHitbox, AbilityKit ability, float damageMultiplier)
    {
        owner = attacker;
        SetTint(CharacterCatalog.FxColorOf(attacker));
        FxGlow.Setup(gameObject, attacker, ability);
        damage = ability.Damage * damageMultiplier;
        if (ability.FollowCaster)
            transform.SetParent(attacker.transform);
        SortAroundAttacker(attacker);
        if (!hasHitbox && hitbox != null)
            hitbox.enabled = false;
    }

    void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        SetFrame(frontRenderer, frontFrames, 0);
        SetFrame(backRenderer, backFrames, 0);
    }

    void Update()
    {
        int frameCount = FrameCount;
        float frameDuration = 1f / frameRate;

        if (frameCount == 0)
        {
            Destroy(gameObject);
            return;
        }
        frameTimer += Time.deltaTime;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;
            if (frameIndex >= frameCount)
            {
                Destroy(gameObject);
                return;
            }
            SetFrame(frontRenderer, frontFrames, frameIndex);
            SetFrame(backRenderer, backFrames, frameIndex);
        }
    }

    // Keeps the front layer over any fxDrawsOver decor it currently overlaps.
    void LateUpdate()
    {
        if (frontRenderer != null)
            frontRenderer.sortingOrder = SortingLayer_Auto.RaiseOverFxUnderlays(frontBaseOrder, frontRenderer.bounds);
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
    }
}
