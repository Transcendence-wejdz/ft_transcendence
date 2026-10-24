using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

public enum LeaderboardMode
{
    Solo,
    Coop,
    TwoVsTwo,
    ThreeVsOne,
}

// Thin wrapper over the Unity Leaderboards service, one board per mode; every call fails soft (null/empty) when offline.
public static class LeaderboardClient
{
    private static readonly string[] LeaderboardIds = { "solo-leaderboard", "coop-leaderboard", "2v2-leaderboard", "3v1-leaderboard" };

    public static event Action OnScoreSubmitted;

    public static string IdFor(LeaderboardMode mode)
    {
        return LeaderboardIds[(int)mode];
    }

    // Submits the run score; the dashboard's update strategy decides whether it replaces the player's previous best.
    public static async Task<LeaderboardEntry> SubmitScoreAsync(LeaderboardMode mode, int score, object metadata)
    {
        try
        {
            await ServicesAuth.EnsureSignedInAsync();
            AddPlayerScoreOptions options = new AddPlayerScoreOptions { Metadata = metadata };
            LeaderboardEntry entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(IdFor(mode), score, options);
            OnScoreSubmitted?.Invoke();
            return entry;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LeaderboardClient: could not submit score {score}. {e.Message}");
            return null;
        }
    }

    public static async Task<List<LeaderboardEntry>> GetTopAsync(LeaderboardMode mode, int limit)
    {
        try
        {
            await ServicesAuth.EnsureSignedInAsync();
            GetScoresOptions options = new GetScoresOptions { Limit = limit, IncludeMetadata = true };
            LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(IdFor(mode), options);
            return page.Results ?? new List<LeaderboardEntry>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"LeaderboardClient: could not fetch top scores. {e.Message}");
            return new List<LeaderboardEntry>();
        }
    }

    // Null when the player has no score yet (EntryNotFound) or the service is unreachable.
    public static async Task<LeaderboardEntry> GetMyEntryAsync(LeaderboardMode mode)
    {
        try
        {
            await ServicesAuth.EnsureSignedInAsync();
            return await LeaderboardsService.Instance.GetPlayerScoreAsync(IdFor(mode));
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
    public int runId;
    public string[] team;
    public int wave;
    public int xp;
    public int seconds;
    public int score;
    public int players;

    // A Coop run played alone ranks on the Solo board.
    public LeaderboardMode Mode()
    {
        return players <= 1 ? LeaderboardMode.Solo : LeaderboardMode.Coop;
    }

    // Null for entries without metadata, e.g. scores submitted before team grouping existed.
    public static CoopRunStats FromMetadata(string metadata)
    {
        if (string.IsNullOrEmpty(metadata))
            return null;

        try
        {
            return JsonUtility.FromJson<CoopRunStats>(metadata);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
