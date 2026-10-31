using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Server-owned health with attacker-side prediction; despawns at zero.
public partial class Enemy : IDamageable, IHealthStats
{
    private const float PredictedDeathTimeout = 1.5f;
    private const float NoPrediction = float.MaxValue;

    private float predictedHealth = NoPrediction;
    private Coroutine predictedDeath;

    private readonly NetworkVariable<float> currentHealth = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float MaxHealth => Kit.MaxHealth;
    public float CurrentHealth => Mathf.Min(currentHealth.Value, predictedHealth);

    public event Action<float, float> OnHealthChanged;

    // Server/offline side only, right before despawn; the ulong is the killer's client id.
    public static event Action<EnemyKit, ulong> OnEnemyKilled;

    private void RaiseHealthChanged()
    {
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    // The server value caught up with the prediction, so the prediction is dropped.
    private void HandleHealthChanged(float previous, float current)
    {
        if (current <= predictedHealth)
            predictedHealth = NoPrediction;
        RaiseHealthChanged();
    }

    // Health is written a frame late, never in Awake (NetworkVariable writes there crash WebGL builds).
    private void AwakeHealth()
    {
        currentHealth.OnValueChanged += HandleHealthChanged;
        StartCoroutine(InitializeHealthNextFrame());
    }

    private IEnumerator InitializeHealthNextFrame()
    {
        yield return null;
        if (this.HasServerAuthority())
            currentHealth.Value = Kit.MaxHealth;
    }

    private void PredictDamage(float amount)
    {
        predictedHealth = Mathf.Max(0f, CurrentHealth - amount);
        RaiseHealthChanged();
        if (predictedHealth <= 0f && predictedDeath == null)
            predictedDeath = StartCoroutine(HideUntilDespawn());
    }

    private void CollectEnabledRenderers(List<Renderer> list)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer.enabled)
                list.Add(renderer);
        }
    }

    private void CollectEnabledParts<T>(List<Behaviour> list) where T : Behaviour
    {
        foreach (T part in GetComponentsInChildren<T>())
        {
            if (part.enabled)
                list.Add(part);
        }
    }

    private static void SetEnabled(List<Renderer> renderers, List<Behaviour> parts, bool enabled)
    {
        foreach (Renderer renderer in renderers)
            renderer.enabled = enabled;
        foreach (Behaviour part in parts)
            part.enabled = enabled;
    }

    // Hides a predicted kill until the server despawns it, restoring it if that never happens.
    private IEnumerator HideUntilDespawn()
    {
        List<Renderer> renderers = new List<Renderer>();
        List<Behaviour> parts = new List<Behaviour>();

        CollectEnabledRenderers(renderers);
        CollectEnabledParts<Canvas>(parts);
        CollectEnabledParts<Collider2D>(parts);
        SetEnabled(renderers, parts, false);
        yield return new WaitForSeconds(PredictedDeathTimeout);
        SetEnabled(renderers, parts, true);
        predictedHealth = NoPrediction;
        RaiseHealthChanged();
        predictedDeath = null;
    }

    private void Die(ulong attackerClientId)
    {
        OnEnemyKilled?.Invoke(Kit, attackerClientId);
        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }

    private void ApplyDamage(float amount, ulong attackerClientId)
    {
        currentHealth.Value = Mathf.Max(0f, currentHealth.Value - amount);
        if (currentHealth.Value <= 0f)
            Die(attackerClientId);
    }

    // Player hitboxes only live on the attacker's own copy, so the RPC sender is the attacker.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDamageServerRpc(float amount, RpcParams rpcParams = default)
    {
        ApplyDamage(amount, rpcParams.Receive.SenderClientId);
    }

    // Callable from any client; applied on the server, predicted on the attacker's screen meanwhile.
    public void RequestDamage(float amount)
    {
        DamageNumberSpawner.MarkLocalHit(this, amount);
        if (!NetworkObject.IsSpawned)
        {
            ApplyDamage(amount, NetworkManager.ServerClientId);
            return;
        }
        if (!IsServer)
            PredictDamage(amount);
        RequestDamageServerRpc(amount);
    }
}
