using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Ability FX: posed from the kit and the aim, spawned locally with its hitbox, then mirrored without one on other peers.
public partial class Player
{
    private GameObject GetFxPrefab(AbilityType ability)
    {
        AbilityKit kit = Ability(ability);

        if (kit == null)
            return null;
        return kit.FxPrefab;
    }

    private float FxScaleMultiplier(AbilityType ability)
    {
        return Ability(ability).Size * SizeBonusMultiplier;
    }

    // Spawn position and rotation of an action's FX, shared by the real spawn and the aim preview.
    private void FxPose(AbilityType ability, float angle, float distance, out Vector3 position, out Quaternion rotation)
    {
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        position = transform.position + (Vector3)FxOffset(ability, dir, distance);
        rotation = Quaternion.Euler(0f, 0f, angle);
        if (Ability(ability).IgnoreAimRotation)
            rotation = Quaternion.identity;
    }

    // Position is computed locally from the attacker's current transform, not sent over RPC - round-trip latency would otherwise bake a stale offset into the spawn point.
    private void SpawnLocalFx(float angle, bool hasHitbox, AbilityType ability, float distance)
    {
        GameObject prefab = GetFxPrefab(ability);
        Vector3 spawnPos;
        Quaternion rotation;
        GameObject fx;

        if (prefab == null)
            return;
        FxPose(ability, angle, distance, out spawnPos, out rotation);
        fx = Instantiate(prefab, spawnPos, rotation);
        fx.transform.localScale *= FxScaleMultiplier(ability);
        fx.GetComponent<IAttackFX>()?.Init(gameObject, hasHitbox, Ability(ability), DamageMultiplier);
    }

    private void SpawnAbilityFx(AbilityType ability, float distance = -1f)
    {
        float angle;

        if (GetFxPrefab(ability) == null)
            return;
        angle = AssistedAimAngle(ability);
        SpawnLocalFx(angle, hasHitbox: true, ability, distance);
        if (NetworkObject.IsSpawned)
            SpawnAbilityFxServerRpc(angle, ability, distance);
    }

    [ServerRpc]
    private void SpawnAbilityFxServerRpc(float angle, AbilityType ability, float distance)
    {
        SpawnAbilityFxClientRpc(angle, OwnerClientId, ability, distance);
    }

    // Mirrors the FX on the other clients only, the attacker already spawned its own (with the hitbox) instantly.
    [ClientRpc]
    private void SpawnAbilityFxClientRpc(float angle, ulong attackerClientId, AbilityType ability, float distance)
    {
        if (NetworkManager.Singleton.LocalClientId != attackerClientId)
            SpawnLocalFx(angle, hasHitbox: false, ability, distance);
    }

    // Where this player's FX for an action spawns, relative to its feet (see AbilityKit.FxOffset).
    public Vector2 FxOffset(AbilityType ability, Vector2 aim, float distance = -1f)
    {
        AbilityKit kit = Ability(ability);

        if (kit == null)
            return Vector2.zero;
        return kit.FxOffset(aim, AimIndicatorCenter, distance);
    }
}
