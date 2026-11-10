using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Where enemies spawn: valid land points around the players, summons.
public partial class WaveSpawner
{
    // The kit index is set before Spawn() so remote clients get it in the spawn payload.
    private void SpawnEnemy(int kitIndex, Vector3 position)
    {
        EnemyKit kit = GetKit(kitIndex);
        GameObject instance = Instantiate(kit.EnemyPrefab, position, Quaternion.identity);
        Enemy enemy = instance.GetComponent<Enemy>();

        enemy.Initialize(kit);
        ApplyStatScale(enemy);
        if (!IsNetworked())
            return;
        enemy.SetNetworkedKitIndex(kitIndex);
        instance.GetComponent<NetworkObject>().Spawn(true);
    }

    // Queues the wave; a wave overlapping the previous one adds to the same queue, so the spawn rate stays capped.
    private void SpawnWave(int count)
    {
        RefreshUnlockedKitIndices();
        pendingSpawns += count;
        if (!spawningBatches)
            StartCoroutine(SpawnPendingEnemies());
    }

    // Spreads spawns over several frames to avoid a lag spike on every peer; each enemy rolls a random kit among the tiers unlocked this wave.
    private IEnumerator SpawnPendingEnemies()
    {
        WaitForSeconds delay = new WaitForSeconds(spawnBatchInterval);
        int batch;

        spawningBatches = true;
        while (pendingSpawns > 0 && !GameOverCheck.IsRunOver)
        {
            batch = Mathf.Min(spawnBatchSize, pendingSpawns, RoomForEnemies());
            pendingSpawns -= batch;
            for (int i = 0; i < batch; i++)
                SpawnEnemy(unlockedKitIndices[Random.Range(0, unlockedKitIndices.Count)], FindSpawnPosition());
            yield return delay;
        }
        spawningBatches = false;
    }

    // Called before leaving the scene, so queued enemies don't spawn during the load.
    public void CancelPendingSpawns()
    {
        pendingSpawns = 0;
    }

    // Spawns just off a random living player's screen.
    private Vector3 GetSpawnCenter()
    {
        EnemyFlowFieldManager manager = EnemyFlowFieldManager.Instance;
        IReadOnlyList<Player> living;

        manager.RefreshLivingPlayers();
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

    private bool IsAnyProbeOffMainland(Vector3 point, ProceduralMapGenerator mapGenerator)
    {
        Vector3[] probes =
        {
            point,
            point + Vector3.right * spawnClearance, point + Vector3.left * spawnClearance,
            point + Vector3.up * spawnClearance, point + Vector3.down * spawnClearance,
        };

        foreach (Vector3 probe in probes)
        {
            if (!mapGenerator.IsOnMainland(probe))
                return true;
        }
        return false;
    }

    // Valid when the point and its four sides are on the players' landmass with no static collider; nothing is valid before the map knows its water.
    private bool IsValidSpawnPoint(Vector3 root, ProceduralMapGenerator mapGenerator)
    {
        Vector3 point = root + (Vector3)GetEnemyBodyOffset();

        if (ObstacleQuery.IsBlocked(point, spawnClearance * 0.5f))
            return false;
        if (mapGenerator == null)
            return true;
        if (!mapGenerator.IsReady)
            return false;
        return !IsAnyProbeOffMainland(point, mapGenerator);
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

    // True when a body center is over water or on an islet; water colliders are outlines only, so an enemy pushed there would stay trapped.
    public bool IsStranded(Vector2 bodyCenter)
    {
        if (cachedMapGenerator == null)
            cachedMapGenerator = FindAnyObjectByType<ProceduralMapGenerator>();
        if (cachedMapGenerator == null || !cachedMapGenerator.IsReady)
            return false;
        return !cachedMapGenerator.IsOnMainland(bodyCenter);
    }
}
