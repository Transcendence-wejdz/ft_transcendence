using System;
using Unity.Netcode;
using UnityEngine;

// Networked enemy health; the server owns it and writes directly.
public class EnemyStats : NetworkBehaviour, IDamageable, IHealthStats
{
    private const float PredictedDeathTimeout = 1.5f;

    private EnemyKit kit;
    private float predictedHealth = float.MaxValue;
    private Coroutine predictedDeath;

    private readonly NetworkVariable<float> currentHealth = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Assigned at spawn time by whoever instantiates this enemy (see Enemy.Initialize); never baked into the prefab.
    public EnemyKit Kit { get => kit; set => kit = value; }

    public float MaxHealth => kit.MaxHealth;
    public float CurrentHealth => Mathf.Min(currentHealth.Value, predictedHealth);

    public event Action<float, float> OnHealthChanged;

    // Fired once, server/offline-authoritative side only, right before the enemy despawns; the ulong is the killer's client id.
    public static event Action<EnemyKit, ulong> OnEnemyKilled;

    void Awake()
    {
        currentHealth.OnValueChanged += (_, newValue) =>
        {
            if (newValue <= predictedHealth)
                predictedHealth = float.MaxValue;
            OnHealthChanged?.Invoke(CurrentHealth, kit.MaxHealth);
        };
        StartCoroutine(InitializeHealthNextFrame());
    }

    private System.Collections.IEnumerator InitializeHealthNextFrame()
    {
        yield return null;
        if (this.HasServerAuthority())
            currentHealth.Value = kit.MaxHealth;
    }

    // Callable from any client; applied on the server.
    public void RequestDamage(float amount)
    {
        if (!NetworkObject.IsSpawned)
        {
            ApplyDamage(amount, NetworkManager.ServerClientId);
            return;
        }

        if (!IsServer)
            PredictDamage(amount);
        RequestDamageServerRpc(amount);
    }

    // Shows the hit on the attacker's screen right away; the server value replaces it once it arrives.
    private void PredictDamage(float amount)
    {
        predictedHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, kit.MaxHealth);

        if (predictedHealth <= 0f && predictedDeath == null)
            predictedDeath = StartCoroutine(HideUntilDespawn());
    }

    // Hides a locally predicted kill until the server despawns it, restoring it if that never happens.
    private System.Collections.IEnumerator HideUntilDespawn()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Canvas[] canvases = GetComponentsInChildren<Canvas>();
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        bool[] rendererStates = System.Array.ConvertAll(renderers, r => r.enabled);
        bool[] canvasStates = System.Array.ConvertAll(canvases, c => c.enabled);
        bool[] colliderStates = System.Array.ConvertAll(colliders, c => c.enabled);

        foreach (Renderer r in renderers)
            r.enabled = false;
        foreach (Canvas c in canvases)
            c.enabled = false;
        foreach (Collider2D c in colliders)
            c.enabled = false;

        yield return new WaitForSeconds(PredictedDeathTimeout);

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = rendererStates[i];
        for (int i = 0; i < canvases.Length; i++)
            canvases[i].enabled = canvasStates[i];
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = colliderStates[i];

        predictedHealth = float.MaxValue;
        OnHealthChanged?.Invoke(CurrentHealth, kit.MaxHealth);
        predictedDeath = null;
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

        OnEnemyKilled?.Invoke(kit, attackerClientId);

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
