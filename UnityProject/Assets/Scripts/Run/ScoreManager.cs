using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Server-side run score: kills, waves reached and the boss kill.
public partial class ScoreManager : NetworkBehaviour
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

    public ScoreBreakdown Breakdown
    {
        get
        {
            if (IsSpawned)
                return breakdown.Value;
            return offlineBreakdown;
        }
    }

    public int TotalScore => Breakdown.Total;

    public PlayerKillTable PlayerKills
    {
        get
        {
            if (IsSpawned)
                return playerKills.Value;
            return offlinePlayerKills;
        }
    }

    // Guarded like XPManager, so an early defensive OnNetworkDespawn can't drop the offline subscription.
    private void Subscribe()
    {
        if (subscribed)
            return;
        subscribed = true;
        Enemy.OnEnemyKilled += HandleEnemyKilled;
        Boss.OnBossKilled += HandleBossKilled;
        WaveSpawner.OnWaveStarted += HandleWaveStarted;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;
        subscribed = false;
        Enemy.OnEnemyKilled -= HandleEnemyKilled;
        Boss.OnBossKilled -= HandleBossKilled;
        WaveSpawner.OnWaveStarted -= HandleWaveStarted;
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

    // Server only, called at the wipe or victory: every player still present gets a row (even with no kill) and nothing scores after this.
    public void Freeze()
    {
        PlayerKillTable table;

        if (frozen)
            return;
        frozen = true;
        table = PlayerKills;
        foreach (Player player in Player.ActivePlayers)
            table.GetOrAdd(player.OwnerClientId, player.PlayerName);
        WriteKills(table);
    }

    void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            return;
        offlineBreakdown = default;
        offlinePlayerKills = default;
        if (RunCarryOver.IsForActiveScene)
        {
            offlineBreakdown = RunCarryOver.Score;
            offlinePlayerKills = RunCarryOver.Kills;
        }
        Subscribe();
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
}
