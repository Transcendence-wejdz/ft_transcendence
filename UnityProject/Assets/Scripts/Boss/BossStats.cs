using System;
using Unity.Netcode;
using UnityEngine;

// Networked boss health, scaled by party size; the server owns it and despawns the boss at zero.
public class BossStats : NetworkBehaviour, IDamageable, IHealthStats
{
    private Boss boss;
    private float predictedHealth = float.MaxValue;

    private readonly NetworkVariable<float> currentHealth = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> maxHealth = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float MaxHealth => maxHealth.Value > 0f ? maxHealth.Value : boss.Kit.MaxHealth;
    public float CurrentHealth => Mathf.Min(currentHealth.Value, predictedHealth);

    public event Action<float, float> OnHealthChanged;

    // Fired once, server/offline-authoritative side only, right before the boss despawns.
    public static event Action<BossKit> OnBossKilled;

    // Health is written a frame late, never in Awake (NetworkVariable writes there crash WebGL builds).
    void Awake()
    {
        boss = GetComponent<Boss>();
        currentHealth.OnValueChanged += (_, value) =>
        {
            if (value <= predictedHealth)
                predictedHealth = float.MaxValue;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        };
        maxHealth.OnValueChanged += (_, value) => OnHealthChanged?.Invoke(CurrentHealth, value);
        StartCoroutine(InitializeHealthNextFrame());
    }

    private System.Collections.IEnumerator InitializeHealthNextFrame()
    {
        yield return null;
        if (!this.HasServerAuthority())
            yield break;

        int extraPlayers = Mathf.Max(0, Player.AllActiveInstances.Count - 1);
        maxHealth.Value = boss.Kit.MaxHealth * (1f + boss.Kit.HealthPerExtraPlayer * extraPlayers);
        currentHealth.Value = maxHealth.Value;
    }

    // Callable from any client; applied on the server, predicted on the attacker's screen until the server value arrives.
    public void RequestDamage(float amount)
    {
        if (!NetworkObject.IsSpawned)
        {
            ApplyDamage(amount);
            return;
        }

        if (!IsServer)
        {
            predictedHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
        RequestDamageServerRpc(amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDamageServerRpc(float amount)
    {
        ApplyDamage(amount);
    }

    private void ApplyDamage(float amount)
    {
        if (currentHealth.Value <= 0f)
            return;

        currentHealth.Value = Mathf.Max(0f, currentHealth.Value - amount);
        if (currentHealth.Value > 0f)
            return;

        OnBossKilled?.Invoke(boss.Kit);

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
