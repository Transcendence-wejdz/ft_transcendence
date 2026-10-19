using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

// Thin wrapper over the Unity Leaderboards service for the Coop score board; every call fails soft (null/empty) when offline.
public static class LeaderboardClient
{
    public const string CoopLeaderboardId = "leaderboard_test";

    public static event Action OnScoreSubmitted;

    // Submits the run score; the dashboard's update strategy decides whether it replaces the player's previous best.
    public static async Task<LeaderboardEntry> SubmitCoopScoreAsync(int score, CoopRunStats stats)
    {
        try
        {
            await ServicesAuth.EnsureSignedInAsync();
            AddPlayerScoreOptions options = new AddPlayerScoreOptions { Metadata = stats };
            LeaderboardEntry entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(CoopLeaderboardId, score, options);
            OnScoreSubmitted?.Invoke();
            return entry;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LeaderboardClient: could not submit score {score}. {e.Message}");
            return null;
        }
    }

    public static async Task<List<LeaderboardEntry>> GetTopAsync(int limit)
    {
        try
        {
            await ServicesAuth.EnsureSignedInAsync();
            GetScoresOptions options = new GetScoresOptions { Limit = limit, IncludeMetadata = true };
            LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(CoopLeaderboardId, options);
            return page.Results ?? new List<LeaderboardEntry>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LeaderboardClient: could not fetch top scores. {e.Message}");
            return new List<LeaderboardEntry>();
        }
    }

    // Null when the player has no score yet (EntryNotFound) or the service is unreachable.
    public static async Task<LeaderboardEntry> GetMyEntryAsync()
    {
        try
        {
            await ServicesAuth.EnsureSignedInAsync();
            return await LeaderboardsService.Instance.GetPlayerScoreAsync(CoopLeaderboardId);
        }
        catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.EntryNotFound)
        {
            return null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LeaderboardClient: could not fetch own entry. {e.Message}");
            return null;
        }
    }
}

[Serializable]
public class CoopRunStats
{
    public int wave;
    public int xp;
    public int seconds;
    public int players;

    // Composite Coop score: waves dominate, XP (kills weighted by enemy type) and survival time break ties.
    public int ComputeScore()
    {
        return wave * 1000 + xp + seconds;
    }
}
