using System.Collections.Generic;
using UnityEngine;

// One-shot FX split into a back/front layer sharing one frame index/timing; ClassKit's FollowCaster decides whether it parents to the caster or stays at its world spawn position.
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

    // Sandwiches the attacker between the front/back layers using its current auto Y-sort order as the anchor.
    public void Init(GameObject attacker, bool hasHitbox, ClassKit kit, AbilityType ability, float damageMultiplier)
    {
        owner = attacker;
        Color tint = CharacterCatalog.FxColorOf(attacker);
        if (frontRenderer != null)
            frontRenderer.color = tint;
        if (backRenderer != null)
            backRenderer.color = tint;
        FxGlow.Setup(gameObject, attacker, kit, ability);
        bool followCaster;
        (damage, followCaster) = ability switch
        {
            AbilityType.Special => (kit.SpecialDamage, kit.SpecialFollowCaster),
            AbilityType.Ultimate => (kit.UltimateDamage, kit.UltimateFollowCaster),
            _ => (kit.AttackDamage, kit.AttackFollowCaster),
        };
        damage *= damageMultiplier;

        if (followCaster)
            transform.SetParent(attacker.transform);

        SortingLayer_Auto attackerSorting = attacker.GetComponent<SortingLayer_Auto>();
        int baseOrder = attackerSorting != null ? attackerSorting.CurrentSortingOrder : 0;
        frontBaseOrder = baseOrder + 1;
        if (frontRenderer != null)
            frontRenderer.sortingOrder = frontBaseOrder;
        if (backRenderer != null)
            backRenderer.sortingOrder = baseOrder - 1;

        if (!hasHitbox && hitbox != null)
            hitbox.enabled = false;
    }

    void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        if (frontRenderer != null && frontFrames != null && frontFrames.Length > 0)
            frontRenderer.sprite = frontFrames[0];
        if (backRenderer != null && backFrames != null && backFrames.Length > 0)
            backRenderer.sprite = backFrames[0];
    }

    void Update()
    {
        int frameCount = frontFrames != null ? frontFrames.Length : (backFrames != null ? backFrames.Length : 0);
        if (frameCount == 0)
        {
            Destroy(gameObject);
            return;
        }

        float frameDuration = 1f / frameRate;
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
            if (frontRenderer != null && frontFrames != null && frameIndex < frontFrames.Length)
                frontRenderer.sprite = frontFrames[frameIndex];
            if (backRenderer != null && backFrames != null && frameIndex < backFrames.Length)
                backRenderer.sprite = backFrames[frameIndex];
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
        if (other.GetComponent<ShieldHoldFX>() != null)
            return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || hitTargets.Contains(damageable))
            return;

        var behaviour = damageable as MonoBehaviour;
        if (behaviour != null && behaviour.gameObject == owner)
            return;

        hitTargets.Add(damageable);
        damageable.RequestDamage(damage);
    }
}
