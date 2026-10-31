using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Where and what enemies spawn: kits unlocked by tier, valid land points around the players, summons.
public partial class WaveSpawner
{
    private int UnlockWaveForTier(int tier)
    {
        if (tier > tierUnlockWave.Length)
            return int.MaxValue;
        return tierUnlockWave[tier - 1];
    }

    // Tier N unlocks at wave tierUnlockWave[N-1]; falls back to kit 0 while none is unlocked.
    private void RefreshUnlockedKitIndices()
    {
        int tier;

        unlockedKitIndices.Clear();
        for (int i = 0; i < enemyKits.Length; i++)
        {
            tier = Mathf.Max(1, enemyKits[i].Tier);
            if (waveNumber >= UnlockWaveForTier(tier))
                unlockedKitIndices.Add(i);
        }
        if (unlockedKitIndices.Count == 0)
            unlockedKitIndices.Add(0);
    }

    // The kit index is set before Spawn() so remote clients get it in the spawn payload.
    private void SpawnEnemy(int kitIndex, Vector3 position)
    {
        EnemyKit kit = GetKit(kitIndex);
        GameObject instance = Instantiate(kit.EnemyPrefab, position, Quaternion.identity);
        Enemy enemy = instance.GetComponent<Enemy>();

        enemy.Initialize(kit);
        if (!IsNetworked())
            return;
        enemy.SetNetworkedKitIndex(kitIndex);
        instance.GetComponent<NetworkObject>().Spawn(true);
    }

    // Each enemy rolls a random kit among the tiers unlocked this wave.
    private void SpawnWave(int count)
    {
        RefreshUnlockedKitIndices();
        for (int i = 0; i < count; i++)
            SpawnEnemy(unlockedKitIndices[Random.Range(0, unlockedKitIndices.Count)], FindSpawnPosition());
    }

    // Spawns just off a random living player's screen.
    private Vector3 GetSpawnCenter()
    {
        EnemyFlowFieldManager manager = EnemyFlowFieldManager.Instance;
        IReadOnlyList<Player> living;

        manager.RefreshPlayers();
        living = manager.LivingPlayers;
        if (living.Count == 0)
            return transform.position;
        return living[Random.Range(0, living.Count)].transform.position;
    }

    // Spawn checks test the body, which sits above the pivot; every variant shares the base collider.
    private Vector2 GetEnemyBodyOffset()
    {
        GameObject prefab;
        CircleCollider2D circle;

        if (enemyKits.Length == 0 || enemyKits[0].EnemyPrefab == null)
            return Vector2.zero;
        prefab = enemyKits[0].EnemyPrefab;
        circle = prefab.GetComponent<CircleCollider2D>();
        if (circle == null)
            return Vector2.zero;
        return circle.offset * (Vector2)prefab.transform.localScale;
    }

    private bool IsAnyProbeInWater(Vector3 point, ProceduralMapGenerator mapGenerator)
    {
        Vector3[] probes =
        {
            point,
            point + Vector3.right * spawnClearance, point + Vector3.left * spawnClearance,
            point + Vector3.up * spawnClearance, point + Vector3.down * spawnClearance,
        };

        foreach (Vector3 probe in probes)
        {
            if (mapGenerator.IsWaterAtWorldPosition(probe))
                return true;
        }
        return false;
    }

    // Valid when the point and its four sides are land with no static collider; nothing is valid before the map knows its water.
    private bool IsValidSpawnPoint(Vector3 root, ProceduralMapGenerator mapGenerator)
    {
        Vector3 point = root + (Vector3)GetEnemyBodyOffset();

        if (ObstacleQuery.IsBlocked(point, spawnClearance * 0.5f))
            return false;
        if (mapGenerator == null)
            return true;
        if (!mapGenerator.IsReady)
            return false;
        return !IsAnyProbeInWater(point, mapGenerator);
    }

    // Public so every peer resolves the same kit from the replicated index (see Enemy.HandleKitIndexChanged).
    public EnemyKit GetKit(int index)
    {
        if (index < 0 || index >= enemyKits.Length)
            return null;
        return enemyKits[index];
    }

    // Server-side summon (e.g. a boss calling minions): spawns kit on a valid point within radius of center, or on center itself; returns where it landed.
    public Vector3 SpawnEnemyNear(EnemyKit kit, Vector3 center, float radius)
    {
        int kitIndex = System.Array.IndexOf(enemyKits, kit);
        ProceduralMapGenerator mapGenerator;
        Vector3 candidate;

        if (kitIndex < 0 || GameOverCheck.IsRunOver)
            return center;
        mapGenerator = FindAnyObjectByType<ProceduralMapGenerator>();
        for (int attempt = 0; attempt < maxSpawnPointAttempts; attempt++)
        {
            candidate = center + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(radius * 0.5f, radius));
            if (IsValidSpawnPoint(candidate, mapGenerator))
            {
                SpawnEnemy(kitIndex, candidate);
                return candidate;
            }
        }
        SpawnEnemy(kitIndex, center);
        return center;
    }

    // Falls back to the spawner's own position if no valid point is found; public so Enemy can reuse it to recycle stray enemies.
    public Vector3 FindSpawnPosition()
    {
        ProceduralMapGenerator mapGenerator = FindAnyObjectByType<ProceduralMapGenerator>();
        Vector3 center = GetSpawnCenter();
        Vector3 candidate;
        float angle;

        for (int attempt = 0; attempt < maxSpawnPointAttempts; attempt++)
        {
            angle = Random.Range(0f, Mathf.PI * 2f);
            candidate = center + (Vector3)(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius);
            if (IsValidSpawnPoint(candidate, mapGenerator))
                return candidate;
        }
        return center;
    }

    // True when a body center is over water; water colliders are outlines only, so an enemy pushed inside a lake would stay trapped there.
    public bool IsInWater(Vector2 bodyCenter)
    {
        if (cachedMapGenerator == null)
            cachedMapGenerator = FindAnyObjectByType<ProceduralMapGenerator>();
        if (cachedMapGenerator == null || !cachedMapGenerator.IsReady)
            return false;
        return cachedMapGenerator.IsWaterAtWorldPosition(bodyCenter);
    }
}
