using System;
using System.Collections.Generic;
using UnityEngine;

// Non-networked practice target, e.g. the Lobby dummy; hits are mirrored to other players via Player.BroadcastDummyHit.
public class DummyStats : MonoBehaviour, IDamageable, IHealthStats
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float regenPerSecond = 5f;
    [SerializeField] private float regenDelay = 3f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public event Action<float, float> OnHealthChanged;

    private static readonly List<DummyStats> All = new List<DummyStats>();

    private float lastHitTime = -999f;

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
        if (CurrentHealth >= maxHealth || Time.time < lastHitTime + regenDelay)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + regenPerSecond * Time.deltaTime);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void RequestDamage(float amount)
    {
        ApplyDamage(amount);
        LocalPlayer.GetPlayer()?.BroadcastDummyHit(transform.position, amount);
    }

    // Remote hits identify their dummy by position, since dummies have no network id.
    public static void ApplyRemoteHit(Vector2 position, float amount)
    {
        DummyStats nearest = null;
        float bestSqrDist = float.MaxValue;
        foreach (DummyStats dummy in All)
        {
            float sqrDist = ((Vector2)dummy.transform.position - position).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                nearest = dummy;
            }
        }

        if (nearest != null)
            nearest.ApplyDamage(amount);
    }

    private void ApplyDamage(float amount)
    {
        lastHitTime = Time.time;
        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
