using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Score sources: enemy kills (credited per player), waves reached and the boss kill.
public partial class ScoreManager
{
    private void HandleEnemyKilled(EnemyKit kit, ulong killerClientId)
    {
        ScoreBreakdown score = Breakdown;

        if (frozen || bossOnly)
            return;
        score.kills++;
        score.killPoints += kit.ScoreReward;
        Write(score);
        AddPlayerKill(killerClientId, kit.ScoreReward);
    }

    // Credits the kill to the killer's own row, when the table still has room for it.
    private void AddPlayerKill(ulong killerClientId, int points)
    {
        PlayerKillTable table = PlayerKills;
        int index = table.GetOrAdd(killerClientId, FindPlayerName(killerClientId));
        PlayerKillScore entry;

        if (index < 0)
            return;
        entry = table[index];
        entry.kills++;
        entry.killPoints += points;
        table[index] = entry;
        WriteKills(table);
    }

    private void HandleBossKilled(BossKit kit)
    {
        ScoreBreakdown score = Breakdown;

        if (frozen)
            return;
        score.bossPoints += kit.ScoreReward;
        Write(score);
    }

    private void HandleWaveStarted(int wave)
    {
        ScoreBreakdown score = Breakdown;

        if (frozen || bossOnly)
            return;
        score.wavePoints += wave * pointsPerWave;
        Write(score);
    }

    // The offline player has the server's client id (0), the id offline kills report.
    private static string FindPlayerName(ulong clientId)
    {
        foreach (Player player in Player.ActivePlayers)
        {
            if (player.OwnerClientId == clientId)
                return player.PlayerName;
        }
        return null;
    }
}
