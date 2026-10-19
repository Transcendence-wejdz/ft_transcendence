using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

// Coop leaderboard view (top N + own rank), reused by the Game Over screen and the Lobby panel; refreshes on enable and after each submit.
public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text entriesText;
    [SerializeField] private TMP_Text ownRankText;
    [SerializeField] private int topCount = 10;

    private bool refreshing;
    private bool refreshQueued;

    void OnEnable()
    {
        LeaderboardClient.OnScoreSubmitted += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        LeaderboardClient.OnScoreSubmitted -= Refresh;
    }

    // Coalesces overlapping calls so a submit landing mid-fetch still triggers one fresh fetch afterwards.
    public async void Refresh()
    {
        if (refreshing)
        {
            refreshQueued = true;
            return;
        }

        refreshing = true;
        entriesText.text = "Loading...";
        if (ownRankText != null)
            ownRankText.text = string.Empty;

        List<LeaderboardEntry> top = await LeaderboardClient.GetTopAsync(topCount);
        LeaderboardEntry mine = await LeaderboardClient.GetMyEntryAsync();

        if (this == null)
            return;

        entriesText.text = FormatEntries(top, mine);
        if (ownRankText != null)
            ownRankText.text = mine != null ? $"Your best: #{mine.Rank + 1}  {mine.Score:0}" : "No score yet";

        refreshing = false;
        if (refreshQueued)
        {
            refreshQueued = false;
            Refresh();
        }
    }

    private static string FormatEntries(List<LeaderboardEntry> entries, LeaderboardEntry mine)
    {
        if (entries.Count == 0)
            return "No scores yet";

        StringBuilder builder = new StringBuilder();
        foreach (LeaderboardEntry entry in entries)
        {
            bool isMine = mine != null && entry.PlayerId == mine.PlayerId;
            string line = $"{entry.Rank + 1}. {FriendsManager.ShortName(entry.PlayerName)}  {entry.Score:0}";
            builder.AppendLine(isMine ? $"<color=#FFD54A>{line}</color>" : line);
        }
        return builder.ToString();
    }
}
