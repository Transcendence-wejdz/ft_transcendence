using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;

// Board text: teammates' entries grouped by run, ranked lines and the player's own best.
public partial class LeaderboardUI
{
    private const int IconLength = 2;
    private const int MinNameLength = 3;
    private const string EvenRowHeight = "<line-height=125%>";
    private const string NoValue = "-";

    // Shortened with a dot so a full team fits its column.
    private static string FitName(string name, int maxLength)
    {
        if (name.Length <= maxLength)
            return name;
        return name.Substring(0, maxLength - 1) + ".";
    }

    // Entries older than the class data have no icon; a team too large for its column keeps only the icons.
    private static string ClassIcon(CoopRunStats stats, int index)
    {
        if (stats.classes == null || index >= stats.classes.Length)
            return string.Empty;
        return $"<sprite index={stats.classes[index]}>";
    }

    private string RowNames(CoopRunStats stats, bool hasRun, LeaderboardEntry entry)
    {
        string[] parts;
        int maxLength;

        if (!hasRun || stats.team == null || stats.team.Length == 0)
            return FriendsManager.ShortName(entry.PlayerName);
        parts = new string[stats.team.Length];
        maxLength = nameColumnLength / parts.Length - IconLength;
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = ClassIcon(stats, i);
            if (maxLength >= MinNameLength || parts[i].Length == 0)
                parts[i] += FitName(stats.team[i], Mathf.Max(MinNameLength, maxLength));
        }
        return string.Join(" ", parts);
    }

    private static string FormatWave(int wave)
    {
        if (wave <= 0)
            return NoValue;
        return wave.ToString();
    }

    private static string FormatTime(int seconds)
    {
        if (seconds <= 0)
            return NoValue;
        return $"{seconds / 60}:{seconds % 60:00}";
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
            if (!PassesClassFilter(stats))
                continue;
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
            if (stats != null)
            {
                row.wave = stats.wave;
                row.seconds = stats.seconds;
            }
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
            line = $"{RankLabel(rank)}<pos=10%>{row.names}<pos=58%>{FormatWave(row.wave)}<pos=70%>{FormatTime(row.seconds)}<pos=82%>{row.score:0}";
        if (row.isMine)
            return $"<color={OwnRowColor}>{line}</color>";
        return line;
    }

    private string FormatRows(List<TeamRow> rows)
    {
        StringBuilder builder = new StringBuilder();

        if (rows.Count == 0)
            return "No scores yet";
        if (columns)
            builder.Append(EvenRowHeight);
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
