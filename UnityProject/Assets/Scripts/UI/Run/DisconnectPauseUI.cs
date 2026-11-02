using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Shown to everyone while the run is paused for a dropped player: each player's state, the ready button, then ResumeCountdownUI takes over.
public class DisconnectPauseUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private DisconnectPauseRowUI rowTemplate;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject waitingContent;
    [SerializeField] private GameObject dim;

    private readonly List<DisconnectPauseRowUI> rows = new List<DisconnectPauseRowUI>();

    private bool shown;
    private bool localReady;
    private string shownRoster;

    private static bool IsLevelUpOpen()
    {
        LevelUpManager levelUp = FindAnyObjectByType<LevelUpManager>();

        return levelUp != null && levelUp.SequenceActive;
    }

    private void SetShown(bool value)
    {
        shown = value;
        localReady = false;
        panel.SetActive(value);
        if (value)
            PauseManager.SetExternalPause(true);
        else if (!IsLevelUpOpen())
            PauseManager.SetExternalPause(false);
    }

    private void SetCounting(bool counting)
    {
        waitingContent.SetActive(!counting);
        dim.SetActive(!counting);
    }

    private DisconnectPauseRowUI RowAt(int index)
    {
        DisconnectPauseRowUI row;

        if (index < rows.Count)
            return rows[index];
        row = Instantiate(rowTemplate, rowTemplate.transform.parent);
        rows.Add(row);
        return row;
    }

    // One "state|class|color|name" line per player; rows past the roster's end are hidden and reused later.
    private void ShowRoster(string roster)
    {
        string[] lines = roster.Split(GameOverCheck.PauseRosterLineSeparator);
        string[] parts;
        DisconnectPauseRowUI row;

        for (int i = 0; i < lines.Length; i++)
        {
            parts = lines[i].Split(GameOverCheck.PauseRosterFieldSeparator);
            row = RowAt(i);
            row.gameObject.SetActive(true);
            row.Set(parts[3], int.Parse(parts[1]), int.Parse(parts[2]), (GameOverCheck.PausePlayerState)int.Parse(parts[0]));
        }
        for (int i = lines.Length; i < rows.Count; i++)
            rows[i].gameObject.SetActive(false);
        shownRoster = roster;
    }

    private void ShowWaiting()
    {
        string roster = GameOverCheck.PauseRoster;

        if (!string.IsNullOrEmpty(roster) && roster != shownRoster)
            ShowRoster(roster);
        readyButton.gameObject.SetActive(!localReady);
        continueButton.gameObject.SetActive(GameOverCheck.CanContinue);
    }

    private void HandleReadyClicked()
    {
        localReady = true;
        GameOverCheck.SetLocalReady();
    }

    private void Awake()
    {
        panel.SetActive(false);
        rowTemplate.gameObject.SetActive(false);
        readyButton.onClick.AddListener(HandleReadyClicked);
        continueButton.onClick.AddListener(GameOverCheck.ContinueWithoutWaiting);
    }

    private void Update()
    {
        bool waiting = GameOverCheck.IsWaitingForPlayers;
        bool counting = GameOverCheck.IsResuming;

        if (waiting != shown)
            SetShown(waiting);
        if (!waiting)
            return;
        SetCounting(counting);
        if (counting)
            continueButton.gameObject.SetActive(false);
        else
            ShowWaiting();
    }
}
