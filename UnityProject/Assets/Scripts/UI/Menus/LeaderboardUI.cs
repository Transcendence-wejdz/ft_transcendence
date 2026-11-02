using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;

// Leaderboard view (top N + own rank) for one mode at a time, reused by the Game Over screen and the Lobby panel; refreshes on enable and after each submit.
public partial class LeaderboardUI : MonoBehaviour
{
    private class TeamRow
    {
        public string names;
        public double score;
        public bool isMine;
    }

    private const string OwnRowColor = "#FFD973";

    private static readonly string[] ModeNames = { "Solo", "Coop", "Two vs Two", "Three vs One" };

    [SerializeField] private TMP_Text entriesText;
    [SerializeField] private TMP_Text ownRankText;
    [SerializeField] private int topCount = 10;
    [SerializeField] private int maxTeamSize = 4;
    [SerializeField] private LeaderboardMode mode = LeaderboardMode.Coop;
    [SerializeField] private Button[] modeTabs;
    [SerializeField] private TMP_Text modeTitle;
    [SerializeField] private bool columns;
    [SerializeField] private TMP_FontAsset rankFont;

    private bool refreshing;
    private bool refreshQueued;

    private void ShowLoading()
    {
        entriesText.text = "Loading...";
        if (ownRankText != null)
            ownRankText.text = string.Empty;
    }

    private void ShowBoard(List<LeaderboardEntry> top, LeaderboardEntry mine)
    {
        List<TeamRow> rows = GroupByRun(top, mine);

        entriesText.text = FormatRows(rows);
        if (ownRankText != null)
            ownRankText.text = FormatOwnRank(rows, mine);
    }

    // Active tab is non-interactable and pinned to read as selected.
    private void PinTab(int index, bool active)
    {
        HoverColorWipe hover = modeTabs[index].GetComponent<HoverColorWipe>();

        modeTabs[index].interactable = !active;
        if (hover != null)
            hover.SetPinned(active);
    }

    // Separate so each listener captures its own mode.
    private void BindTab(Button tab, LeaderboardMode tabMode)
    {
        tab.onClick.AddListener(() => SetMode(tabMode));
    }

    public void SetMode(LeaderboardMode newMode)
    {
        mode = newMode;
        for (int i = 0; i < modeTabs.Length; i++)
            PinTab(i, i == (int)mode);
        if (modeTitle != null)
            modeTitle.text = ModeNames[(int)mode];
        if (isActiveAndEnabled)
            Refresh();
    }

    // Coalesces overlapping calls so a submit landing mid-fetch still triggers one fresh fetch afterwards.
    public async void Refresh()
    {
        LeaderboardMode fetchedMode;
        List<LeaderboardEntry> top;
        LeaderboardEntry mine;

        if (refreshing)
        {
            refreshQueued = true;
            return;
        }
        refreshing = true;
        ShowLoading();
        fetchedMode = mode;
        top = await LeaderboardClient.GetTopAsync(fetchedMode, topCount * maxTeamSize);
        mine = await LeaderboardClient.GetMyEntryAsync(fetchedMode);
        if (this == null)
            return;
        ShowBoard(top, mine);
        refreshing = false;
        if (!refreshQueued && fetchedMode == mode)
            return;
        refreshQueued = false;
        Refresh();
    }

    // Tab index matches the LeaderboardMode value.
    void Awake()
    {
        if (rankFont != null)
            MaterialReferenceManager.AddFontAsset(rankFont);
        for (int i = 0; i < modeTabs.Length; i++)
            BindTab(modeTabs[i], (LeaderboardMode)i);
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
}
