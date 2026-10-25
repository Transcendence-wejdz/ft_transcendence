using System.Collections;
using UnityEngine;

// Spawns a floating damage number FX each time this actor's health drops.
public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.5f, 0f);

    private const float LocalHitWindow = 1f;

    private IHealthStats stats;
    private float lastHealth;
    private float pendingLocalDamage;
    private float pendingLocalTime;

    void Awake()
    {
        stats = GetComponent<IHealthStats>();
    }

    void OnEnable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged += HandleHealthChanged;
            StartCoroutine(CaptureBaselineNextFrame());
        }
    }

    // Deferred a frame: OnEnable can run before a sibling component's Awake sets the real starting health.
    private IEnumerator CaptureBaselineNextFrame()
    {
        yield return null;
        lastHealth = stats.CurrentHealth;
    }

    // Called from the target's RequestDamage, which only runs on the attacker's machine, so the next drop reads as the local player's hit.
    public static void MarkLocalHit(Component target, float amount)
    {
        DamageNumberSpawner spawner = target.GetComponent<DamageNumberSpawner>();
        if (spawner == null)
            return;

        if (Time.time - spawner.pendingLocalTime > LocalHitWindow)
            spawner.pendingLocalDamage = 0f;
        spawner.pendingLocalDamage += amount;
        spawner.pendingLocalTime = Time.time;
    }

    void OnDisable()
    {
        if (stats != null)
            stats.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (current < lastHealth)
        {
            float amount = lastHealth - current;
            bool own = pendingLocalDamage > 0f && Time.time - pendingLocalTime <= LocalHitWindow;
            if (own)
                pendingLocalDamage = Mathf.Max(0f, pendingLocalDamage - amount);
            if (GameSettings.ShowDamageNumbers)
                Spawn(amount, own);
        }
        lastHealth = current;
    }

    private void Spawn(float amount, bool own)
    {
        if (damageNumberPrefab == null)
            return;

        GameObject fx = Instantiate(damageNumberPrefab, transform.position + spawnOffset, Quaternion.identity);
        fx.GetComponent<DamageNumberPopup>()?.Show(amount, own);
    }
}
