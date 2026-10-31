using UnityEngine;

// Leash: an enemy left far from every player, or stuck in a lake, is teleported back near the action.
public partial class Enemy
{
    private void TeleportTo(Vector3 position)
    {
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
        if (networkTransform != null && IsSpawned)
            networkTransform.Teleport(position, transform.rotation, transform.localScale);
        else
            transform.position = position;
    }

    private bool IsLeashBroken()
    {
        float maxDistance = Kit.MaxDistanceFromPlayers;

        if ((target.Center - BodyCenter).sqrMagnitude > maxDistance * maxDistance)
            return true;
        return waveSpawner.IsInWater(BodyCenter);
    }

    // Teleports an enemy left far from every player, or stuck in a lake, back near the action.
    private bool CheckLeash()
    {
        if (waveSpawner == null || !IsLeashBroken())
            return false;
        TeleportTo(waveSpawner.FindSpawnPosition());
        return true;
    }

    private bool TickLeash()
    {
        if (Time.time < nextLeashCheckTime)
            return false;
        nextLeashCheckTime = Time.time + Kit.LeashCheckInterval;
        return CheckLeash();
    }
}
