using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Game over screen: the host picks Restart or Return to Lobby, everyone else waits.
public partial class GameOverUI : MonoBehaviour
{
    private const float ClosedScale = 0.85f;
    private const string FallbackPlayerName = "Player";

    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform window;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private GameObject waitingBanner;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string victoryTitle = "Victory";
    [SerializeField] private float showDelay = 0.3f;
    [SerializeField] private float fadeDuration = 0.25f;

    private GameOverCheck check;
    private CanvasGroup panelGroup;
    private float triggerTime = -1f;
    private bool shown;
    private CoopRunStats runStats;
    private string defeatTitle;

    public static bool IsShowing { get; private set; }

    // Restart/ReturnToLobby spawn a fresh GameOverCheck with gameOver back at false; close and re-arm for it.
    private void ResetScreen()
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

    private void ShowChoiceOrWait()
    {
        bool canChoose = check.CanChoose;

        restartButton.gameObject.SetActive(canChoose);
        lobbyButton.gameObject.SetActive(canChoose);
        waitingBanner.SetActive(!canChoose);
    }

    private void ShowTitle()
    {
        if (titleText == null)
            return;
        if (check.IsVictory)
            titleText.text = victoryTitle;
        else
            titleText.text = defeatTitle;
    }

    // Unscaled time, so it plays whatever Time.timeScale is.
    private IEnumerator FadeIn()
    {
        float t = 0f;
        float eased;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / fadeDuration), 3f);
            window.localScale = Vector3.one * Mathf.Lerp(ClosedScale, 1f, eased);
            panelGroup.alpha = eased;
            yield return null;
        }
        window.localScale = Vector3.one;
        panelGroup.alpha = 1f;
    }

    private void Show()
    {
        shown = true;
        IsShowing = true;
        PauseManager.SetExternalPause(true);
        ShowChoiceOrWait();
        ShowTitle();
        ShowResults();
        panel.SetActive(true);
        StartCoroutine(FadeIn());
        SubmitScore();
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

    private void ChooseRestart()
    {
        Choose(true);
    }

    private void ChooseLobby()
    {
        Choose(false);
    }

    private bool IsRunOver()
    {
        if (check == null)
            check = FindAnyObjectByType<GameOverCheck>();
        return check != null && check.IsGameOver;
    }

    // The stats are frozen at the wipe itself, then the screen waits a beat before showing.
    private void TickPendingShow()
    {
        if (triggerTime < 0f)
        {
            triggerTime = Time.unscaledTime;
            runStats = CaptureRunStats();
        }
        if (Time.unscaledTime - triggerTime >= showDelay)
            Show();
    }

    void Awake()
    {
        IsShowing = false;
        panelGroup = panel.GetComponent<CanvasGroup>();
        panel.SetActive(false);
        if (titleText != null)
            defeatTitle = titleText.text;
        restartButton.onClick.AddListener(ChooseRestart);
        lobbyButton.onClick.AddListener(ChooseLobby);
    }

    void OnDestroy()
    {
        IsShowing = false;
    }

    // Polled so a restart's fresh GameOverCheck is picked up automatically.
    void Update()
    {
        if (!IsRunOver())
        {
            if (shown || triggerTime >= 0f)
                ResetScreen();
            return;
        }
        if (!shown)
            TickPendingShow();
    }
}
