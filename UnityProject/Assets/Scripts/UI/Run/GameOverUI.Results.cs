using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Run results on the Game Over screen: score card, gold claim and leaderboard submission.
public partial class GameOverUI
{
    private static ulong LocalClientId()
    {
        NetworkManager network = NetworkManager.Singleton;

        if (network == null || !network.IsListening)
            return NetworkManager.ServerClientId;
        return network.LocalClientId;
    }

    // Session players' names, or the local one solo, sorted so every teammate builds the same list.
    private static List<string> TeamNames()
    {
        List<string> team = new List<string>();
        Player local;

        foreach (Player player in Player.AllActiveInstances)
            team.Add(player.PlayerName);
        if (team.Count == 0)
        {
            local = LocalPlayer.GetPlayer();
            if (local != null)
                team.Add(local.PlayerName);
            else
                team.Add(FallbackPlayerName);
        }
        team.Sort(System.StringComparer.Ordinal);
        return team;
    }

    // Built from the server's frozen result plus the synced team names, so every teammate's entry groups under one runId.
    private CoopRunStats CaptureRunStats()
    {
        RunResult result = check.Result;
        List<string> team;

        if (result.wave <= 0)
            return null;
        team = TeamNames();
        return new CoopRunStats
        {
            runId = result.runId,
            wave = result.wave,
            xp = result.xp,
            seconds = result.seconds,
            score = result.score.Total,
            players = team.Count,
            team = team.ToArray()
        };
    }

    private static PlayerKillTable CurrentKills()
    {
        ScoreManager score = FindAnyObjectByType<ScoreManager>();

        if (score == null)
            return default;
        return score.PlayerKills;
    }

    // Every player reports the shared run to the server on their own account; the card shows the outcome once it answers.
    private void ClaimGold(RunScoreUI scoreCard)
    {
        if (runStats != null)
            GoldWallet.ClaimRunReward(check.Result, gold => ShowGoldResult(scoreCard, gold));
    }

    private static void ShowGoldResult(RunScoreUI scoreCard, int gold)
    {
        if (scoreCard != null)
            scoreCard.SetGoldResult(gold);
    }

    private void ShowResults()
    {
        LeaderboardUI board = panel.GetComponentInChildren<LeaderboardUI>(true);
        RunScoreUI scoreCard = panel.GetComponentInChildren<RunScoreUI>(true);

        if (board != null && runStats != null)
            board.SetMode(runStats.Mode());
        if (scoreCard != null)
            scoreCard.Show(check.Result, CurrentKills(), LocalClientId());
        ClaimGold(scoreCard);
    }

    // Every player submits the shared team result on their own account; the board refreshes itself once it lands.
    private async void SubmitScore()
    {
        if (runStats != null)
            await LeaderboardClient.SubmitScoreAsync(runStats.Mode(), runStats.score, runStats);
    }
}
