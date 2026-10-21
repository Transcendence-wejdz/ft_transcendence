using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Game over screen: the host picks Restart or Return to Lobby, everyone else waits.
public class GameOverUI : MonoBehaviour
{
    private const float ClosedScale = 0.85f;

    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform window;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private TMP_Text waitingText;
    [SerializeField] private float showDelay = 0.3f;
    [SerializeField] private float fadeDuration = 0.25f;

    private GameOverCheck check;
    private CanvasGroup panelGroup;
    private float triggerTime = -1f;
    private bool shown;
    private CoopRunStats runStats;

    public static bool IsShowing { get; private set; }

    void Awake()
    {
        IsShowing = false;
        panelGroup = panel.GetComponent<CanvasGroup>();
        panel.SetActive(false);

        restartButton.onClick.AddListener(() => Choose(restart: true));
        lobbyButton.onClick.AddListener(() => Choose(restart: false));
    }

    void OnDestroy()
    {
        IsShowing = false;
    }

    // Waits a beat after the wipe, then shows; polled so a restart's fresh GameOverCheck is picked up automatically.
    void Update()
    {
        if (check == null)
            check = FindAnyObjectByType<GameOverCheck>();

        bool isOver = check != null && check.IsGameOver;
        if (!isOver)
        {
            if (shown || triggerTime >= 0f)
                Reset();
            return;
        }

        if (shown)
            return;

        if (triggerTime < 0f)
        {
            triggerTime = Time.unscaledTime;
            runStats = CaptureRunStats();
        }

        if (Time.unscaledTime - triggerTime >= showDelay)
            Show();
    }

    // Restart/ReturnToLobby spawn a fresh GameOverCheck with gameOver back at false; close and re-arm for it.
    private void Reset()
    {
        shown = false;
        IsShowing = false;
        triggerTime = -1f;
        runStats = null;
        panel.SetActive(false);
        PauseManager.SetExternalPause(false);
        restartButton.interactable = true;
        lobbyButton.interactable = true;
    }

    private void Show()
    {
        shown = true;
        IsShowing = true;
        PauseManager.SetExternalPause(true);

        bool canChoose = check.CanChoose;
        restartButton.gameObject.SetActive(canChoose);
        lobbyButton.gameObject.SetActive(canChoose);
        waitingText.gameObject.SetActive(!canChoose);

        LeaderboardUI board = panel.GetComponentInChildren<LeaderboardUI>(true);
        if (board != null && runStats != null)
            board.SetMode(runStats.Mode());

        panel.SetActive(true);
        StartCoroutine(FadeIn());
        SubmitScore();
    }

    // Built from the server's frozen result plus the synced team names, so every teammate's entry groups under one runId.
    private CoopRunStats CaptureRunStats()
    {
        RunResult result = check.Result;
        if (result.wave <= 0)
            return null;

        List<string> team = new List<string>();
        foreach (Player player in Player.AllActiveInstances)
            team.Add(player.PlayerName);
        if (team.Count == 0)
        {
            Player local = LocalPlayer.GetPlayer();
            team.Add(local != null ? local.PlayerName : "Player");
        }
        team.Sort(System.StringComparer.Ordinal);

        return new CoopRunStats
        {
            runId = result.runId,
            wave = result.wave,
            xp = result.xp,
            seconds = result.seconds,
            players = team.Count,
            team = team.ToArray()
        };
    }

    // Every player submits the shared team result on their own account; the board refreshes itself once it lands.
    private async void SubmitScore()
    {
        if (runStats == null)
            return;

        int score = runStats.ComputeScore();
        await LeaderboardClient.SubmitScoreAsync(runStats.Mode(), score, runStats);
    }

    private void Choose(bool restart)
    {
        restartButton.interactable = false;
        lobbyButton.interactable = false;

        if (restart)
            check.Restart();
        else
            check.ReturnToLobby();
    }

    // Unscaled time, so it plays whatever Time.timeScale is.
    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / fadeDuration), 3f);
            window.localScale = Vector3.one * Mathf.Lerp(ClosedScale, 1f, eased);
            panelGroup.alpha = eased;
            yield return null;
        }

        window.localScale = Vector3.one;
        panelGroup.alpha = 1f;
    }
}
