using System.Collections.Generic;
using UnityEngine;

// Ground zone FX in three phases (start once, loop for a duration, end once) that damages everything inside it every tick during the loop; only the attacker's copy deals damage.
public class ZoneAttackFX : MonoBehaviour, IAttackFX
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] startFrames;
    [SerializeField] private Sprite[] loopFrames;
    [SerializeField] private Sprite[] endFrames;
    [SerializeField] private float frameRate = 12f;
    [Range(0.1f, 20f)]
    [Tooltip("Seconds the loop phase lasts, ticking damage meanwhile.")]
    [SerializeField] private float duration = 2.5f;
    [Range(0.05f, 3f)]
    [Tooltip("Seconds between two damage ticks; each tick hits everything inside once.")]
    [SerializeField] private float tickInterval = 0.5f;

    private enum Phase { Start, Loop, End }

    private GameObject owner;
    private float damage;
    private Collider2D hitbox;
    private Phase phase;
    private int frameIndex;
    private float frameTimer;
    private float loopTimer;
    private float tickTimer;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<IDamageable> hitThisTick = new HashSet<IDamageable>();
    private ContactFilter2D filter;

    public void Init(GameObject attacker, bool hasHitbox, ClassKit kit, AbilityType ability, float damageMultiplier)
    {
        owner = attacker;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = CharacterCatalog.FxColorOf(attacker);
            spriteRenderer.sortingOrder = SortingLayer_Auto.OrderForY(transform.position.y);
        }
        FxGlow.Setup(gameObject, attacker, kit, ability);
        damage = ability switch
        {
            AbilityType.Special => kit.SpecialDamage,
            AbilityType.Ultimate => kit.UltimateDamage,
            _ => kit.AttackDamage,
        } * damageMultiplier;

        if (!hasHitbox && hitbox != null)
            hitbox.enabled = false;
    }

    void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        filter = new ContactFilter2D { useTriggers = true };
        filter.NoFilter();
        EnterPhase(Phase.Start);
    }

    void Update()
    {
        if (phase == Phase.Loop)
        {
            loopTimer += Time.deltaTime;
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;
                DamageInside();
            }
            if (loopTimer >= duration)
            {
                EnterPhase(Phase.End);
                return;
            }
        }

        Sprite[] frames = Frames(phase);
        if (frames.Length == 0)
            return;
        frameTimer += Time.deltaTime;
        float frameDuration = 1f / frameRate;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;
            if (frameIndex < frames.Length)
                SetFrame(frames[frameIndex]);
            else if (phase == Phase.Loop)
            {
                frameIndex = 0;
                SetFrame(frames[0]);
            }
            else
            {
                EnterPhase(phase == Phase.Start ? Phase.Loop : Phase.End + 1);
                return;
            }
        }
    }

    // Skips empty phases, so a sheet without a start or end still works; past End the FX is gone.
    private void EnterPhase(Phase next)
    {
        while (next <= Phase.End && Frames(next).Length == 0 && next != Phase.Loop)
            next++;
        if (next > Phase.End)
        {
            Destroy(gameObject);
            return;
        }

        phase = next;
        frameIndex = 0;
        frameTimer = 0f;
        Sprite[] frames = Frames(phase);
        if (frames.Length > 0)
            SetFrame(frames[0]);
        if (phase == Phase.Loop)
        {
            loopTimer = 0f;
            tickTimer = 0f;
            DamageInside();
        }
    }

    private Sprite[] Frames(Phase p)
    {
        Sprite[] frames = p == Phase.Start ? startFrames : p == Phase.Loop ? loopFrames : endFrames;
        return frames ?? new Sprite[0];
    }

    private void SetFrame(Sprite sprite)
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;
    }

    // One hit per damageable per tick, however many of its colliders overlap the zone.
    private void DamageInside()
    {
        if (hitbox == null || !hitbox.enabled)
            return;

        hitThisTick.Clear();
        hitbox.Overlap(filter, overlaps);
        foreach (Collider2D other in overlaps)
        {
            if (other.GetComponent<ShieldHoldFX>() != null)
                continue;

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || !hitThisTick.Add(damageable))
                continue;

            var behaviour = damageable as MonoBehaviour;
            if (behaviour != null && behaviour.gameObject == owner)
                continue;

            damageable.RequestDamage(damage);
        }
    }
}
