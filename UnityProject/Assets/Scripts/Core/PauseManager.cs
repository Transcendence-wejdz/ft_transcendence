using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pause menu and its settings page, plus the shared input block NPC menus use.
public partial class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button quitButton;

    private static bool menuPaused;
    private static bool externalPaused;
    private static PauseManager current;

    private bool frozeTime;
    private bool buttonsShownOnline;

    // Pause menu and NPC menus (SetExternalPause) block input independently, so closing one never unblocks the other.
    public static bool IsPaused => menuPaused || externalPaused;

    // True while the pause menu or its settings page is up (not while closing); it owns the close input over any other open menu.
    public static bool IsMenuOpen
    {
        get
        {
            if (current == null)
                return false;
            return IsPanelOpen(current.pausePanel) || IsPanelOpen(current.settingsPanel);
        }
    }

    private static bool IsOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private bool InLobby => SceneManager.GetActiveScene().name == lobbySceneName;

    // Everyone except a mode-scene host leaves the session.
    private bool LeavesSession => IsOnline && (InLobby || !NetworkManager.Singleton.IsServer);

    // Only undoes a freeze this manager made, so it never unfreezes someone else's (e.g. level-up).
    private void SetFrozen(bool frozen)
    {
        frozeTime = frozen;
        if (frozen)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }

    // Pause only blocks input in the Lobby and in multiplayer; freezing timeScale is for solo mode scenes (would desync online).
    private void SetPaused(bool paused)
    {
        menuPaused = paused;
        if (paused && !IsOnline && !InLobby)
            SetFrozen(true);
        else if (!paused && frozeTime)
            SetFrozen(false);
        if (paused)
            RefreshButtons();
        SetPanelVisible(pausePanel, paused);
    }

    private async Task LeaveSessionThenLoadLobbyAsync()
    {
        if (IsOnline)
            await SessionDisconnectHandler.LeaveSessionsAsync();
        LoadingScreenManager.LoadScene(lobbySceneName);
    }

    private void BringEveryoneToLobby()
    {
        ModeReadyCheck.Instance?.CancelReadyCheck();
        NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }

    // The pause key closes this menu first, then any other open menu, and only then opens the pause menu.
    public void Pause(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || GameOverUI.IsShowing || LevelUpUI.IsShowing)
            return;
        if (IsMenuOpen)
        {
            Resume();
            return;
        }
        if (MenuPanel.CurrentOpen != null)
        {
            MenuPanel.CurrentOpen.Close();
            return;
        }
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
        else if (menuPaused)
            Resume();
    }

    // Host in a mode scene brings everyone to the Lobby; others leave the session first.
    public void LeaveOrReturn()
    {
        bool hostBringsEveryoneBack = IsOnline && !LeavesSession;

        SetPaused(false);
        if (hostBringsEveryoneBack)
            BringEveryoneToLobby();
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
        externalPaused = paused;
    }

    void Awake()
    {
        menuPaused = false;
        externalPaused = false;
        current = this;
    }

    // A session started while paused solo (someone joined, or we hosted to invite) must not stay frozen; buttons follow the new online state.
    private void Update()
    {
        if (frozeTime && IsOnline)
            SetFrozen(false);
        if (IsPaused && IsOnline != buttonsShownOnline)
            RefreshButtons();
    }

    // A scene change while paused solo would otherwise carry timeScale 0 into the next scene.
    private void OnDestroy()
    {
        if (current == this)
            current = null;
        if (frozeTime)
            SetFrozen(false);
    }
}
