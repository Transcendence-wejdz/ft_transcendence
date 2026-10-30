using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

// Server-owned health with attacker-side prediction; despawns at zero.
public partial class Enemy : IDamageable, IHealthStats
{
    private const float PredictedDeathTimeout = 1.5f;

    private float predictedHealth = float.MaxValue;
    private Coroutine predictedDeath;

    private readonly NetworkVariable<float> currentHealth = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float MaxHealth => Kit.MaxHealth;
    public float CurrentHealth => Mathf.Min(currentHealth.Value, predictedHealth);

    public event Action<float, float> OnHealthChanged;

    // Server/offline side only, right before despawn; the ulong is the killer's client id.
    public static event Action<EnemyKit, ulong> OnEnemyKilled;

    // Health is written a frame late, never in Awake (NetworkVariable writes there crash WebGL builds).
    private void AwakeHealth()
    {
        currentHealth.OnValueChanged += (_, value) =>
        {
            if (value <= predictedHealth)
                predictedHealth = float.MaxValue;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        };
        StartCoroutine(InitializeHealthNextFrame());
    }

    private IEnumerator InitializeHealthNextFrame()
    {
        yield return null;
        if (this.HasServerAuthority())
            currentHealth.Value = Kit.MaxHealth;
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

    private void PredictDamage(float amount)
    {
        predictedHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        if (predictedHealth <= 0f && predictedDeath == null)
            predictedDeath = StartCoroutine(HideUntilDespawn());
    }

    // Hides a predicted kill until the server despawns it, restoring it if that never happens.
    private IEnumerator HideUntilDespawn()
    {
        List<Renderer> renderers = GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToList();
        List<Behaviour> parts = GetComponentsInChildren<Canvas>().Cast<Behaviour>()
            .Concat(GetComponentsInChildren<Collider2D>()).Where(b => b.enabled).ToList();
        SetVisible(renderers, parts, false);

        yield return new WaitForSeconds(PredictedDeathTimeout);

        SetVisible(renderers, parts, true);
        predictedHealth = float.MaxValue;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        predictedDeath = null;
    }

    private static void SetVisible(List<Renderer> renderers, List<Behaviour> parts, bool visible)
    {
        foreach (Renderer r in renderers)
            r.enabled = visible;
        foreach (Behaviour b in parts)
            b.enabled = visible;
    }

    // Player hitboxes only live on the attacker's own copy, so the RPC sender is the attacker.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDamageServerRpc(float amount, RpcParams rpcParams = default)
    {
        ApplyDamage(amount, rpcParams.Receive.SenderClientId);
    }

    private void ApplyDamage(float amount, ulong attackerClientId)
    {
        currentHealth.Value = Mathf.Max(0f, currentHealth.Value - amount);
        if (currentHealth.Value > 0f)
            return;

        OnEnemyKilled?.Invoke(Kit, attackerClientId);
        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
