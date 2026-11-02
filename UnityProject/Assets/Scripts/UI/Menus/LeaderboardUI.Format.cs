using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;

// Board text: teammates' entries grouped by run, ranked lines and the player's own best.
public partial class LeaderboardUI
{
    private static string RowNames(CoopRunStats stats, bool hasRun, LeaderboardEntry entry)
    {
        if (hasRun && stats.team != null && stats.team.Length > 0)
            return string.Join(", ", stats.team);
        return FriendsManager.ShortName(entry.PlayerName);
    }

    // Teammates each submit the same run, so entries sharing a runId collapse into one row ranked by its best entry.
    private List<TeamRow> GroupByRun(List<LeaderboardEntry> entries, LeaderboardEntry mine)
    {
        List<TeamRow> rows = new List<TeamRow>();
        Dictionary<int, TeamRow> byRun = new Dictionary<int, TeamRow>();
        CoopRunStats stats;
        bool isMine;
        bool hasRun;
        TeamRow row;

        foreach (LeaderboardEntry entry in entries)
        {
            stats = CoopRunStats.FromMetadata(entry.Metadata);
            isMine = mine != null && entry.PlayerId == mine.PlayerId;
            hasRun = stats != null && stats.runId != 0;
            if (hasRun && byRun.TryGetValue(stats.runId, out row))
            {
                row.isMine |= isMine;
                continue;
            }
            if (rows.Count >= topCount)
                continue;
            row = new TeamRow { names = RowNames(stats, hasRun, entry), score = entry.Score, isMine = isMine };
            rows.Add(row);
            if (hasRun)
                byRun[stats.runId] = row;
        }
        return rows;
    }

    private string RankLabel(int rank)
    {
        if (rankFont != null)
            return $"<font=\"{rankFont.name}\">{rank}</font>";
        return rank.ToString();
    }

    // Columns mode lays rank, names and score out with TMP pos tags for the Lobby table; the rank uses rankFont when set.
    private string FormatRow(TeamRow row, int rank)
    {
        string line = $"{rank}. {row.names}  {row.score:0}";

        if (columns)
            line = $"{RankLabel(rank)}<pos=10%>{row.names}<pos=82%>{row.score:0}";
        if (row.isMine)
            return $"<color={OwnRowColor}>{line}</color>";
        return line;
    }

    private string FormatRows(List<TeamRow> rows)
    {
        StringBuilder builder = new StringBuilder();

        if (rows.Count == 0)
            return "No scores yet";
        for (int i = 0; i < rows.Count; i++)
            builder.AppendLine(FormatRow(rows[i], i + 1));
        return builder.ToString();
    }

    // Team rank when the player's run is on the board, else just the score since raw ranks count every teammate.
    private static string FormatOwnRank(List<TeamRow> rows, LeaderboardEntry mine)
    {
        int index;

        if (mine == null)
            return "No score yet";
        index = rows.FindIndex(row => row.isMine);
        if (index < 0)
            return $"Your best: {mine.Score:0}";
        return $"Your best: #{index + 1}  {mine.Score:0}";
    }
}
