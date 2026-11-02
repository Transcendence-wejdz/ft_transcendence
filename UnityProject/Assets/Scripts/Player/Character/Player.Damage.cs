using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Damage and revive routed to the owner, who alone writes its health.
public partial class Player
{
    public void TakeDamage(float amount)
    {
        float minHealth = 0f;
        float health;

        if (!this.IsLocallyControlled() || IsInvulnerable)
            return;
        if (DeathProtected)
            minHealth = 1f;
        health = Mathf.Clamp(currentHealth.Value - amount, Mathf.Min(minHealth, currentHealth.Value), MaxHealth);
        if (health <= 0f && currentHealth.Value > 0f && TryConsumeExtraLife())
            health = MaxHealth * LevelUpBonusCatalog.ExtraLifeHealthFraction;
        currentHealth.Value = health;
    }

    public void RequestDamage(float amount)
    {
        if (!NetworkObject.IsSpawned)
        {
            TakeDamage(amount);
            return;
        }
        RequestDamageServerRpc(amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDamageServerRpc(float amount)
    {
        ApplyDamageClientRpc(amount, OwnerOnlyParams());
    }

    [ClientRpc]
    private void ApplyDamageClientRpc(float amount, ClientRpcParams rpcParams = default)
    {
        TakeDamage(amount);
    }

    // Mirrors a local dummy hit onto the other players' copies of that non-networked dummy.
    public void BroadcastDummyHit(Vector2 position, float amount)
    {
        if (NetworkObject.IsSpawned && IsOwner)
            DummyHitRpc(position, amount);
    }

    [Rpc(SendTo.NotMe)]
    private void DummyHitRpc(Vector2 position, float amount)
    {
        DummyStats.ApplyRemoteHit(position, amount);
    }

    // Called by a rider once the mount channel completes; routed to the downed player's owner, like damage.
    public void RequestRevive()
    {
        if (!NetworkObject.IsSpawned)
        {
            Revive();
            return;
        }
        RequestReviveServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestReviveServerRpc()
    {
        ApplyReviveClientRpc(OwnerOnlyParams());
    }

    [ClientRpc]
    private void ApplyReviveClientRpc(ClientRpcParams rpcParams = default)
    {
        Revive();
    }

    private void Revive()
    {
        if (!this.IsLocallyControlled())
            return;
        currentHealth.Value = MaxHealth;
    }
}
