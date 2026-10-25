using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button quitButton;

    public static bool IsPaused { get; private set; }

    private static bool IsOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private bool InLobby => SceneManager.GetActiveScene().name == lobbySceneName;

    // Everyone except a mode-scene host leaves the session.
    private bool LeavesSession => IsOnline && (InLobby || !NetworkManager.Singleton.IsServer);

    void Awake()
    {
        IsPaused = false;
    }

    public void Pause(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || GameOverUI.IsShowing || LevelUpUI.IsShowing)
            return;

        if (MenuPanel.CurrentOpen != null)
        {
            MenuPanel.CurrentOpen.Close();
            return;
        }

        if (IsPaused)
            Resume();
        else
            SetPaused(true);
    }

    public void Resume()
    {
        SetPanelVisible(settingsPanel, false);
        SetPaused(false);
    }

    public void OpenSettings()
    {
        SetPanelVisible(pausePanel, false);
        SetPanelVisible(settingsPanel, true);
    }

    public void CloseSettings()
    {
        SetPanelVisible(settingsPanel, false);
        SetPanelVisible(pausePanel, true);
    }

    // Gamepad B: Settings goes back to Pause, Pause resumes.
    public void Back()
    {
        if (IsPanelOpen(settingsPanel))
            CloseSettings();
        else if (IsPaused)
            Resume();
    }

    // Host in a mode scene brings everyone to the Lobby; others leave the session first.
    public void LeaveOrReturn()
    {
        bool hostBringsEveryoneBack = IsOnline && !LeavesSession;
        SetPaused(false);

        if (hostBringsEveryoneBack)
        {
            ModeReadyCheck.Instance?.CancelReadyCheck();
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
        }
        else
            _ = LeaveSessionThenLoadLobbyAsync();
    }

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Used by NPC menu panels: blocks input via IsPaused but leaves Time.timeScale running.
    public static void SetExternalPause(bool paused)
    {
        IsPaused = paused;
    }

    private async Task LeaveSessionThenLoadLobbyAsync()
    {
        if (IsOnline)
            await SessionDisconnectHandler.LeaveSessionsAsync();

        LoadingScreenManager.LoadScene(lobbySceneName);
    }

    // Shows the leave button when there is somewhere to go; hides Quit on WebGL.
    private void RefreshButtons()
    {
        bool showLeave = !InLobby || IsOnline;
        bool showQuit = Application.platform != RuntimePlatform.WebGLPlayer;

        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(showLeave);
            TMP_Text label = leaveButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = LeavesSession ? "Leave Session" : "Return to Lobby";
        }

        if (quitButton != null)
            quitButton.gameObject.SetActive(showQuit);
    }

    // Multiplayer pause only blocks input; freezing timeScale would desync.
    private void SetPaused(bool paused)
    {
        IsPaused = paused;

        bool isMultiplayerSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!isMultiplayerSession)
            Time.timeScale = paused ? 0f : 1f;

        if (paused)
            RefreshButtons();

        SetPanelVisible(pausePanel, paused);
    }

    // Animated when the panel has a PanelSlideIn, instant otherwise.
    private static void SetPanelVisible(GameObject panel, bool show)
    {
        if (panel == null)
            return;

        PanelSlideIn slide = panel.GetComponent<PanelSlideIn>();
        if (slide == null)
            panel.SetActive(show);
        else if (show)
            slide.Show();
        else
            slide.Hide();
    }

    // A panel still playing its close animation counts as closed.
    private static bool IsPanelOpen(GameObject panel)
    {
        if (panel == null || !panel.activeSelf)
            return false;

        PanelSlideIn slide = panel.GetComponent<PanelSlideIn>();
        return slide == null || slide.IsOpen;
    }
}
