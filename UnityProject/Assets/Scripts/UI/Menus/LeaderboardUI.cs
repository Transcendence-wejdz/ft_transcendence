using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;

// Leaderboard view (top N + own rank) for one mode at a time, reused by the Game Over screen and the Lobby panel; refreshes on enable and after each submit.
public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text entriesText;
    [SerializeField] private TMP_Text ownRankText;
    [SerializeField] private int topCount = 10;
    [SerializeField] private int maxTeamSize = 4;
    [SerializeField] private LeaderboardMode mode = LeaderboardMode.Coop;
    [SerializeField] private Button[] modeTabs;
    [SerializeField] private TMP_Text modeTitle;
    [SerializeField] private bool columns;
    [SerializeField] private TMP_FontAsset rankFont;

    private static readonly string[] ModeNames = { "Solo", "Coop", "Two vs Two", "Three vs One" };

    private bool refreshing;
    private bool refreshQueued;

    // Tab index matches the LeaderboardMode value.
    void Awake()
    {
        if (rankFont != null)
            MaterialReferenceManager.AddFontAsset(rankFont);
        for (int i = 0; i < modeTabs.Length; i++)
        {
            LeaderboardMode tabMode = (LeaderboardMode)i;
            modeTabs[i].onClick.AddListener(() => SetMode(tabMode));
        }
    }

    // Active tab is non-interactable and pinned to read as selected.
    public void SetMode(LeaderboardMode newMode)
    {
        mode = newMode;
        for (int i = 0; i < modeTabs.Length; i++)
        {
            modeTabs[i].interactable = i != (int)mode;
            HoverColorWipe hover = modeTabs[i].GetComponent<HoverColorWipe>();
            if (hover != null)
                hover.SetPinned(i == (int)mode);
        }
        if (modeTitle != null)
            modeTitle.text = ModeNames[(int)mode];

        if (isActiveAndEnabled)
            Refresh();
    }

    void OnEnable()
    {
        LeaderboardClient.OnScoreSubmitted += Refresh;
        SetMode(mode);
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

        LeaderboardMode fetchedMode = mode;
        List<LeaderboardEntry> top = await LeaderboardClient.GetTopAsync(fetchedMode, topCount * maxTeamSize);
        LeaderboardEntry mine = await LeaderboardClient.GetMyEntryAsync(fetchedMode);

        if (this == null)
            return;

        List<TeamRow> rows = GroupByRun(top, mine);
        entriesText.text = FormatRows(rows, columns);
        if (ownRankText != null)
            ownRankText.text = FormatOwnRank(rows, mine);

        refreshing = false;
        if (refreshQueued || fetchedMode != mode)
        {
            refreshQueued = false;
            Refresh();
        }
    }

    private class TeamRow
    {
        public string names;
        public double score;
        public bool isMine;
    }

    // Teammates each submit the same run, so entries sharing a runId collapse into one row ranked by its best entry.
    private List<TeamRow> GroupByRun(List<LeaderboardEntry> entries, LeaderboardEntry mine)
    {
        List<TeamRow> rows = new List<TeamRow>();
        Dictionary<int, TeamRow> byRun = new Dictionary<int, TeamRow>();

        foreach (LeaderboardEntry entry in entries)
        {
            CoopRunStats stats = CoopRunStats.FromMetadata(entry.Metadata);
            bool isMine = mine != null && entry.PlayerId == mine.PlayerId;
            bool hasRun = stats != null && stats.runId != 0;

            if (hasRun && byRun.TryGetValue(stats.runId, out TeamRow existing))
            {
                existing.isMine |= isMine;
                continue;
            }

            if (rows.Count >= topCount)
                continue;

            TeamRow row = new TeamRow
            {
                names = hasRun && stats.team != null && stats.team.Length > 0
                    ? string.Join(", ", stats.team)
                    : FriendsManager.ShortName(entry.PlayerName),
                score = entry.Score,
                isMine = isMine
            };
            rows.Add(row);
            if (hasRun)
                byRun[stats.runId] = row;
        }
        return rows;
    }

    // Columns mode lays rank, names and score out with TMP pos tags for the Lobby table; the rank uses rankFont when set.
    private string FormatRows(List<TeamRow> rows, bool columns)
    {
        if (rows.Count == 0)
            return "No scores yet";

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < rows.Count; i++)
        {
            string rank = rankFont != null ? $"<font=\"{rankFont.name}\">{i + 1}</font>" : $"{i + 1}";
            string line = columns
                ? $"{rank}<pos=10%>{rows[i].names}<pos=82%>{rows[i].score:0}"
                : $"{i + 1}. {rows[i].names}  {rows[i].score:0}";
            builder.AppendLine(rows[i].isMine ? $"<color=#FFD973>{line}</color>" : line);
        }
        return builder.ToString();
    }

    // Team rank when the player's run is on the board, else just the score since raw ranks count every teammate.
    private static string FormatOwnRank(List<TeamRow> rows, LeaderboardEntry mine)
    {
        if (mine == null)
            return "No score yet";

        int index = rows.FindIndex(row => row.isMine);
        return index >= 0 ? $"Your best: #{index + 1}  {mine.Score:0}" : $"Your best: {mine.Score:0}";
    }
}
