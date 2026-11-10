using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Decor hits mirrored to every peer, since decor has no network id; a joining player gets the host's totals.
public partial class Player
{
    private const int DecorSyncBatchSize = 200;

    [Rpc(SendTo.NotMe)]
    private void DecorHitRpc(int x, int y, float amount)
    {
        DestructibleDecor.ApplyHit(new Vector2Int(x, y), amount);
    }

    // The first batch wipes what this client broke while solo; reliable RPCs arrive in order.
    [ClientRpc]
    private void SyncDecorDamageClientRpc(int[] xs, int[] ys, float[] totals, bool isFirstBatch, ClientRpcParams rpcParams = default)
    {
        if (isFirstBatch)
            DestructibleDecor.ClearAll();
        DestructibleDecor.ApplyTotals(xs, ys, totals);
    }

    private void SendDecorBatch(List<KeyValuePair<Vector2Int, float>> entries, int start)
    {
        int count = Mathf.Min(DecorSyncBatchSize, entries.Count - start);
        int[] xs = new int[count];
        int[] ys = new int[count];
        float[] totals = new float[count];

        for (int i = 0; i < count; i++)
        {
            xs[i] = entries[start + i].Key.x;
            ys[i] = entries[start + i].Key.y;
            totals[i] = entries[start + i].Value;
        }
        SyncDecorDamageClientRpc(xs, ys, totals, start == 0, OwnerOnlyParams());
    }

    // Server: in batches under the transport's payload limit, and at least one, since the joiner may have broken decor while solo.
    private void SpawnDecorSync()
    {
        List<KeyValuePair<Vector2Int, float>> entries;
        int start = 0;

        if (!IsServer || IsOwner)
            return;
        entries = new List<KeyValuePair<Vector2Int, float>>(DestructibleDecor.Damage);
        do
        {
            SendDecorBatch(entries, start);
            start += DecorSyncBatchSize;
        }
        while (start < entries.Count);
    }

    public void BroadcastDecorHit(Vector2Int cell, float amount)
    {
        if (NetworkObject.IsSpawned && IsOwner)
            DecorHitRpc(cell.x, cell.y, amount);
    }
}
