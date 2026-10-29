using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Pattern effects: charge and slam hits (server for its own player, each client for itself) and minion summons.
public partial class Boss
{
    private const float ContactMargin = 0.5f;

    private readonly HashSet<Player> chargeHits = new HashSet<Player>();
    private readonly List<Player> victims = new List<Player>();
    private float localChargeEndTime;
    private bool localChargeHit;
    private WaveSpawner waveSpawner;

    // Lane width actually hit: never narrower than the body plus a contact margin, since colliders keep players outside the body.
    private float ChargeHitWidth => Mathf.Max(kit.ChargeWidth, 2f * (bodyRadius + ContactMargin));

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

    // Snapshot first: a killing blow removes the player from LivingPlayers mid-loop (Player.OnPlayerDied), which would break the enumeration.
    private void CollectVictims(float radius)
    {
        victims.Clear();
        foreach (Player player in EnemyFlowFieldManager.Instance.LivingPlayers)
        {
            if ((player.Center - BodyCenter).sqrMagnitude <= radius * radius)
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

    // Client side: hits this machine's own player if it stands inside the radius around the boss as displayed here.
    private bool JudgeLocalHit(float radius, float damage)
    {
        Player local = LocalPlayer.GetPlayer();
        if (local == null || local.IsDead || (local.Center - BodyCenter).sqrMagnitude > radius * radius)
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

    // Each minion rolls its own kit from the summon list; spawn spots are server-picked, so they travel to clients for the dust FX.
    private void Summon()
    {
        if (waveSpawner == null)
            waveSpawner = FindAnyObjectByType<WaveSpawner>();
        if (waveSpawner == null)
            return;

        Vector2[] spots = new Vector2[kit.SummonCount];
        for (int i = 0; i < spots.Length; i++)
            spots[i] = waveSpawner.SpawnEnemyNear(kit.SummonKits[Random.Range(0, kit.SummonKits.Length)], BodyCenter, kit.SummonRadius);

        PlaySummonFx(spots);
        if (IsSpawned)
            SummonFxRpc(spots);
    }

    [Rpc(SendTo.NotServer)]
    private void SummonFxRpc(Vector2[] spots)
    {
        PlaySummonFx(spots);
    }

    // One random dust puff at each minion's feet.
    private void PlaySummonFx(Vector2[] spots)
    {
        GameObject[] fx = kit.SummonFxPrefabs;
        if (fx == null || fx.Length == 0)
            return;

        foreach (Vector2 spot in spots)
        {
            GameObject prefab = fx[Random.Range(0, fx.Length)];
            if (prefab != null)
                Instantiate(prefab, spot + kit.SummonFxOffset, Quaternion.identity);
        }
    }
}
