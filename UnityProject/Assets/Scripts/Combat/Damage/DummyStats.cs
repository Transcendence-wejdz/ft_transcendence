using System;
using System.Collections.Generic;
using UnityEngine;

// Non-networked practice target, e.g. the Lobby dummy; hits are mirrored to other players via Player.BroadcastDummyHit.
public class DummyStats : MonoBehaviour, IDamageable, IHealthStats
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float regenPerSecond = 5f;
    [SerializeField] private float regenDelay = 3f;

    private static readonly List<DummyStats> All = new List<DummyStats>();

    private float lastHitTime = -999f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public event Action<float, float> OnHealthChanged;

    private static DummyStats FindNearest(Vector2 position)
    {
        DummyStats nearest = null;
        float bestSqrDist = float.MaxValue;
        float sqrDist;

        foreach (DummyStats dummy in All)
        {
            sqrDist = ((Vector2)dummy.transform.position - position).sqrMagnitude;
            if (sqrDist >= bestSqrDist)
                continue;
            bestSqrDist = sqrDist;
            nearest = dummy;
        }
        return nearest;
    }

    private void ApplyDamage(float amount)
    {
        lastHitTime = Time.time;
        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private bool CanRegen()
    {
        if (CurrentHealth >= maxHealth)
            return false;
        return Time.time >= lastHitTime + regenDelay;
    }

    public void RequestDamage(float amount)
    {
        DamageNumberSpawner.MarkLocalHit(this, amount);
        ApplyDamage(amount);
        LocalPlayer.GetPlayer()?.BroadcastDummyHit(transform.position, amount);
    }

    // Remote hits identify their dummy by position, since dummies have no network id.
    public static void ApplyRemoteHit(Vector2 position, float amount)
    {
        DummyStats nearest = FindNearest(position);

        if (nearest != null)
            nearest.ApplyDamage(amount);
    }

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Update()
    {
        if (!CanRegen())
            return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + regenPerSecond * Time.deltaTime);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
