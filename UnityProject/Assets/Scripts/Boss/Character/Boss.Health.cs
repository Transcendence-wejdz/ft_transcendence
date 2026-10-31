using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Server-owned health scaled by party size, predicted on the attacker's screen; despawns the boss at zero.
public partial class Boss : IDamageable, IHealthStats
{
    private const float NoPrediction = float.MaxValue;

    private float predictedHealth = NoPrediction;

    private readonly NetworkVariable<float> currentHealth = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> maxHealth = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float MaxHealth
    {
        get
        {
            if (maxHealth.Value > 0f)
                return maxHealth.Value;
            return kit.MaxHealth;
        }
    }
    public float CurrentHealth => Mathf.Min(currentHealth.Value, predictedHealth);

    public event Action<float, float> OnHealthChanged;

    // Server/offline side only, right before the boss despawns.
    public static event Action<BossKit> OnBossKilled;

    // The server value caught up with the prediction, so the prediction is dropped.
    private void HandleHealthChanged(float previous, float current)
    {
        if (current <= predictedHealth)
            predictedHealth = NoPrediction;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    private void HandleMaxHealthChanged(float previous, float current)
    {
        OnHealthChanged?.Invoke(CurrentHealth, current);
    }

    // Health is written a frame late, never in Awake (NetworkVariable writes there crash WebGL builds).
    private void AwakeHealth()
    {
        currentHealth.OnValueChanged += HandleHealthChanged;
        maxHealth.OnValueChanged += HandleMaxHealthChanged;
        StartCoroutine(InitializeHealthNextFrame());
    }

    private float ScaledMaxHealth()
    {
        int extraPlayers = Mathf.Max(0, Player.AllActiveInstances.Count - 1);

        return kit.MaxHealth * (1f + kit.HealthPerExtraPlayer * extraPlayers);
    }

    private IEnumerator InitializeHealthNextFrame()
    {
        yield return null;
        if (!this.HasServerAuthority())
            yield break;
        maxHealth.Value = ScaledMaxHealth();
        currentHealth.Value = maxHealth.Value;
    }

    private void PredictDamage(float amount)
    {
        predictedHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    private void Die()
    {
        OnBossKilled?.Invoke(kit);
        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }

    private void ApplyDamage(float amount)
    {
        if (currentHealth.Value <= 0f)
            return;
        currentHealth.Value = Mathf.Max(0f, currentHealth.Value - amount);
        if (currentHealth.Value <= 0f)
            Die();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDamageServerRpc(float amount)
    {
        ApplyDamage(amount);
    }

    // Callable from any client; applied on the server, predicted on the attacker's screen meanwhile.
    public void RequestDamage(float amount)
    {
        DamageNumberSpawner.MarkLocalHit(this, amount);
        if (!NetworkObject.IsSpawned)
        {
            ApplyDamage(amount);
            return;
        }
        if (!IsServer)
            PredictDamage(amount);
        RequestDamageServerRpc(amount);
    }
}
