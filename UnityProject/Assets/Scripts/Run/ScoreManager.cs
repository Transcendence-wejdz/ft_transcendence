using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Run score split by source, so the Game Over screen can show where the points came from.
public struct ScoreBreakdown : INetworkSerializeByMemcpy
{
    public int kills;
    public int killPoints;
    public int wavePoints;
    public int bossPoints;

    public int Total => killPoints + wavePoints + bossPoints;
}

// One player's own kills, kept by client id so it survives the move to the boss scene.
public struct PlayerKillScore : INetworkSerializeByMemcpy
{
    public ulong clientId;
    public FixedString32Bytes name;
    public int kills;
    public int killPoints;
}

// Fixed four slots (the session cap), since a memcpy NetworkVariable stays safe offline where a NetworkList does not.
public struct PlayerKillTable : INetworkSerializeByMemcpy
{
    public const int Capacity = 4;

    public int count;
    public PlayerKillScore slot0;
    public PlayerKillScore slot1;
    public PlayerKillScore slot2;
    public PlayerKillScore slot3;

    public PlayerKillScore this[int index]
    {
        get => index switch { 0 => slot0, 1 => slot1, 2 => slot2, _ => slot3 };
        set
        {
            switch (index)
            {
                case 0: slot0 = value; break;
                case 1: slot1 = value; break;
                case 2: slot2 = value; break;
                default: slot3 = value; break;
            }
        }
    }

    public int IndexOf(ulong clientId)
    {
        for (int i = 0; i < count; i++)
        {
            if (this[i].clientId == clientId)
                return i;
        }
        return -1;
    }

    // Returns the player's slot, adding it when there is room, or -1 once all four are taken.
    public int GetOrAdd(ulong clientId, string playerName)
    {
        int index = IndexOf(clientId);
        if (index < 0)
        {
            if (count >= Capacity)
                return -1;
            index = count++;
        }

        PlayerKillScore entry = this[index];
        entry.clientId = clientId;
        if (!string.IsNullOrEmpty(playerName))
            entry.name = new FixedString32Bytes(playerName);
        this[index] = entry;
        return index;
    }
}

// Server-side run score: kills, waves reached and the boss kill.
public class ScoreManager : NetworkBehaviour
{
    [SerializeField] private int pointsPerWave = 100;
    [SerializeField] private bool bossOnly;

    private readonly NetworkVariable<ScoreBreakdown> breakdown = new OfflineSafeNetworkVariable<ScoreBreakdown>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<PlayerKillTable> playerKills = new OfflineSafeNetworkVariable<PlayerKillTable>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private ScoreBreakdown offlineBreakdown;
    private PlayerKillTable offlinePlayerKills;
    private bool subscribed;
    private bool frozen;

    public ScoreBreakdown Breakdown => IsSpawned ? breakdown.Value : offlineBreakdown;

    public int TotalScore => Breakdown.Total;

    public PlayerKillTable PlayerKills => IsSpawned ? playerKills.Value : offlinePlayerKills;

    void Start()
    {
        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!isNetworked)
        {
            offlineBreakdown = RunCarryOver.IsForActiveScene ? RunCarryOver.Score : default;
            offlinePlayerKills = RunCarryOver.IsForActiveScene ? RunCarryOver.Kills : default;
            Subscribe();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        if (RunCarryOver.IsForActiveScene)
        {
            breakdown.Value = RunCarryOver.Score;
            playerKills.Value = RunCarryOver.Kills;
        }
        Subscribe();
    }

    public override void OnNetworkDespawn()
    {
        Unsubscribe();
    }

    public override void OnDestroy()
    {
        Unsubscribe();
        base.OnDestroy();
    }

    // Guarded like XPManager, so an early defensive OnNetworkDespawn can't drop the offline subscription.
    private void Subscribe()
    {
        if (subscribed)
            return;
        subscribed = true;
        EnemyStats.OnEnemyKilled += HandleEnemyKilled;
        BossStats.OnBossKilled += HandleBossKilled;
        WaveSpawner.OnWaveStarted += HandleWaveStarted;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;
        subscribed = false;
        EnemyStats.OnEnemyKilled -= HandleEnemyKilled;
        BossStats.OnBossKilled -= HandleBossKilled;
        WaveSpawner.OnWaveStarted -= HandleWaveStarted;
    }

    private void HandleEnemyKilled(EnemyKit kit, ulong killerClientId)
    {
        if (frozen || bossOnly)
            return;

        ScoreBreakdown score = Breakdown;
        score.kills++;
        score.killPoints += kit.ScoreReward;
        Write(score);

        PlayerKillTable table = PlayerKills;
        int index = table.GetOrAdd(killerClientId, FindPlayerName(killerClientId));
        if (index < 0)
            return;

        PlayerKillScore entry = table[index];
        entry.kills++;
        entry.killPoints += kit.ScoreReward;
        table[index] = entry;
        WriteKills(table);
    }

    private void HandleBossKilled(BossKit kit)
    {
        if (frozen)
            return;

        ScoreBreakdown score = Breakdown;
        score.bossPoints += kit.ScoreReward;
        Write(score);
    }

    private void HandleWaveStarted(int wave)
    {
        if (frozen || bossOnly)
            return;

        ScoreBreakdown score = Breakdown;
        score.wavePoints += wave * pointsPerWave;
        Write(score);
    }

    // Server only, called at the wipe or victory: every player still present gets a row (even with no kill) and nothing scores after this.
    public void Freeze()
    {
        if (frozen)
            return;

        frozen = true;
        PlayerKillTable table = PlayerKills;
        foreach (Player player in Player.AllActiveInstances)
            table.GetOrAdd(player.OwnerClientId, player.PlayerName);
        if (Player.AllActiveInstances.Count == 0)
        {
            Player local = LocalPlayer.GetPlayer();
            if (local != null)
                table.GetOrAdd(NetworkManager.ServerClientId, local.PlayerName);
        }
        WriteKills(table);
    }

    // Offline the only player is the local one, whatever client id the kill reported.
    private static string FindPlayerName(ulong clientId)
    {
        foreach (Player player in Player.AllActiveInstances)
        {
            if (player.OwnerClientId == clientId)
                return player.PlayerName;
        }

        Player local = Player.AllActiveInstances.Count == 0 ? LocalPlayer.GetPlayer() : null;
        return local != null ? local.PlayerName : null;
    }

    private void WriteKills(PlayerKillTable table)
    {
        if (IsSpawned)
            playerKills.Value = table;
        else
            offlinePlayerKills = table;
    }

    private void Write(ScoreBreakdown score)
    {
        if (IsSpawned)
            breakdown.Value = score;
        else
            offlineBreakdown = score;
    }
}
