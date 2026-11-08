using Unity.Netcode;
using UnityEngine;

// Spawn-time stat multipliers, replicated since damage is judged on every peer; also counts the enemies alive for the spawner's cap.
public partial class Enemy
{
    private readonly NetworkVariable<float> healthScale = new OfflineSafeNetworkVariable<float>(
        1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> damageScale = new OfflineSafeNetworkVariable<float>(
        1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public static int ActiveCount { get; private set; }

    public float Damage => Kit.Damage * damageScale.Value;

    // Called by WaveSpawner before Spawn(), so remote clients get the multipliers in the first payload.
    public void SetStatScale(float health, float damage)
    {
        healthScale.Value = health;
        damageScale.Value = damage;
    }

    void OnEnable()
    {
        ActiveCount++;
    }

    void OnDisable()
    {
        ActiveCount--;
    }
}
