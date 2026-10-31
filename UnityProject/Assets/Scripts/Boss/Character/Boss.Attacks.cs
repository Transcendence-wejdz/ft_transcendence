using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Pattern effects: charge and slam hits (server for its own player, each client for itself) and minion summons.
public partial class Boss
{
    private const float ContactMargin = 0.5f;
    private const float MinionPushSpeed = 9f;
    private const float MinionPushDuration = 0.25f;
    private const float MinionForwardPush = 0.5f;

    private static readonly Collider2D[] minionBuffer = new Collider2D[32];
    private static int enemyLayer = -1;

    private readonly HashSet<Player> chargeHits = new HashSet<Player>();
    private readonly List<Player> victims = new List<Player>();
    private float localChargeEndTime;
    private bool localChargeHit;
    private bool passingThroughMinions;

    private WaveSpawner waveSpawner;

    // Lane width actually hit: never narrower than the body plus a contact margin, since colliders keep players outside the body.
    private float ChargeHitWidth => Mathf.Max(kit.ChargeWidth, 2f * (bodyRadius + ContactMargin));

    // While charging, the boss body ignores enemies so its minions never slow it.
    private void SetChargePassThrough(bool charging)
    {
        if (enemyLayer < 0)
            enemyLayer = LayerMask.NameToLayer("Enemy");
        if (charging == passingThroughMinions)
            return;
        passingThroughMinions = charging;
        if (charging)
            rb.excludeLayers = 1 << enemyLayer;
        else
            rb.excludeLayers = 0;
    }

    private static ContactFilter2D GetMinionFilter()
    {
        return new ContactFilter2D { useLayerMask = true, layerMask = 1 << enemyLayer };
    }

    // Shoves a minion to the side it already leans to, with a little forward push.
    private void PushMinion(Enemy minion, Vector2 center, Vector2 side)
    {
        float sideSign = 1f;
        Vector2 push;

        if (Vector2.Dot(minion.BodyCenter - center, side) < 0f)
            sideSign = -1f;
        push = (side * sideSign + patternDirection * MinionForwardPush).normalized;
        minion.ApplyKnockback(push * MinionPushSpeed, MinionPushDuration);
    }

    private void PushMinionsAside()
    {
        Vector2 center = BodyCenter;
        Vector2 side = new Vector2(-patternDirection.y, patternDirection.x);
        int count = Physics2D.OverlapCircle(center, bodyRadius + ContactMargin, GetMinionFilter(), minionBuffer);
        Enemy minion;

        for (int i = 0; i < count; i++)
        {
            minion = minionBuffer[i].GetComponentInParent<Enemy>();
            if (minion != null)
                PushMinion(minion, center, side);
        }
    }

    private bool IsWithin(Player player, float radius)
    {
        return (player.Center - BodyCenter).sqrMagnitude <= radius * radius;
    }

    // Snapshot first: a killing blow removes the player from LivingPlayers mid-loop (Player.OnPlayerDied), which would break the enumeration.
    private void CollectVictims(float radius)
    {
        victims.Clear();
        foreach (Player player in EnemyFlowFieldManager.Instance.LivingPlayers)
        {
            if (IsWithin(player, radius))
                victims.Add(player);
        }
    }

    // The server only hits its own player; remote players judge slams and charges on their own machine (see JudgeLocalHit).
    private void HitPlayer(Player player, float damage)
    {
        if (IsSpawned && player.OwnerClientId != NetworkManager.LocalClientId)
            return;
        if (!Player.TryAbsorbWithShield(BodyCenter, player.Center, damage))
            player.RequestDamage(damage);
    }

    // Each living player the boss body passes over is hit at most once per charge.
    private void HitChargeVictims()
    {
        CollectVictims(ChargeHitWidth * 0.5f);
        foreach (Player player in victims)
        {
            if (chargeHits.Add(player))
                HitPlayer(player, kit.ChargeDamage);
        }
    }

    private void ResolveSlam()
    {
        CollectVictims(kit.SlamRadius);
        foreach (Player player in victims)
            HitPlayer(player, kit.SlamDamage);
    }

    // Client side: hits this machine's own player if it stands inside the radius around the boss as displayed here.
    private bool JudgeLocalHit(float radius, float damage)
    {
        Player local = LocalPlayer.GetPlayer();

        if (local == null || local.IsDead || !IsWithin(local, radius))
            return false;
        if (!Player.TryAbsorbWithShield(BodyCenter, local.Center, damage))
            local.TakeDamage(damage);
        return true;
    }

    // Remote clients check charge contact every frame of the charge they see, at most once per charge.
    private void UpdateLocalCharge()
    {
        if (IsServer || localChargeHit || Time.time >= localChargeEndTime)
            return;
        localChargeHit = JudgeLocalHit(ChargeHitWidth * 0.5f, kit.ChargeDamage);
    }

    // One random dust puff at each minion's feet.
    private void PlaySummonFx(Vector2[] spots)
    {
        GameObject[] fx = kit.SummonFxPrefabs;
        GameObject prefab;

        if (fx == null || fx.Length == 0)
            return;
        foreach (Vector2 spot in spots)
        {
            prefab = fx[Random.Range(0, fx.Length)];
            if (prefab != null)
                Instantiate(prefab, spot + kit.SummonFxOffset, Quaternion.identity);
        }
    }

    [Rpc(SendTo.NotServer)]
    private void SummonFxRpc(Vector2[] spots)
    {
        PlaySummonFx(spots);
    }

    private EnemyKit RollSummonKit()
    {
        return kit.SummonKits[Random.Range(0, kit.SummonKits.Length)];
    }

    // Each minion rolls its own kit; spawn spots are server-picked, so they travel to clients for the dust FX.
    private void Summon()
    {
        Vector2[] spots;

        if (waveSpawner == null)
            waveSpawner = FindAnyObjectByType<WaveSpawner>();
        if (waveSpawner == null)
            return;
        spots = new Vector2[kit.SummonCount];
        for (int i = 0; i < spots.Length; i++)
            spots[i] = waveSpawner.SpawnEnemyNear(RollSummonKit(), BodyCenter, kit.SummonRadius);
        PlaySummonFx(spots);
        if (IsSpawned)
            SummonFxRpc(spots);
    }
}
