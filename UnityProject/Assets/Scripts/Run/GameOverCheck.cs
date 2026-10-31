using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run totals frozen by the server at the wipe, so every player submits the exact same result.
public struct RunResult : INetworkSerializeByMemcpy
{
    public int runId;
    public int wave;
    public int xp;
    public int seconds;
    public ScoreBreakdown score;
    public bool victory;
}

// Detects a full wipe (or the boss's death, a victory) and lets the host (or solo player) restart the mode or return to the Lobby.
public partial class GameOverCheck : NetworkBehaviour
{
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string restartSceneName;
    [SerializeField] private float startupGracePeriod = 0.5f;

    private readonly NetworkVariable<bool> gameOver = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<RunResult> result = new OfflineSafeNetworkVariable<RunResult>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private static GameOverCheck instance;

    private bool offlineGameOver;
    private RunResult offlineResult;
    private bool isLoading;
    private bool sceneReady;
    private float readyTime;
    private bool bossEngaged;

    public bool IsGameOver
    {
        get
        {
            if (IsSpawned)
                return gameOver.Value;
            return offlineGameOver;
        }
    }

    // True on every peer once the run ended (wipe or victory); spawning and enemies stop on it.
    public static bool IsRunOver => instance != null && instance.IsGameOver;

    public bool CanChoose => this.HasServerAuthority();

    public RunResult Result
    {
        get
        {
            if (IsSpawned)
                return result.Value;
            return offlineResult;
        }
    }

    public bool IsVictory => IsGameOver && Result.victory;

    private void HandleLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        ArmReadiness();
    }

    private void ArmReadiness()
    {
        sceneReady = true;
        readyTime = Time.time + startupGracePeriod;
    }

    private bool CanEvaluate()
    {
        if (!this.HasServerAuthority() || IsGameOver)
            return false;
        return sceneReady && Time.time >= readyTime;
    }

    private static bool AllPlayersDead()
    {
        IReadOnlyList<Player> players = Player.ActivePlayers;

        foreach (Player player in players)
        {
            if (player.CurrentHealth > 0f)
                return false;
        }
        return players.Count > 0;
    }

    // The boss scene is won once a boss was seen and is gone.
    private bool IsBossDefeated()
    {
        if (Boss.Active != null)
            bossEngaged = true;
        return bossEngaged && Boss.Active == null;
    }

    // Only the host or a solo player may change scene, and only once.
    private void LoadScene(string sceneName)
    {
        if (!CanChoose || isLoading)
            return;
        isLoading = true;
        if (sceneName == lobbySceneName)
            ModeReadyCheck.Instance?.CancelReadyCheck();
        GameModeLoader.LoadNow(sceneName);
    }

    public void Restart()
    {
        if (string.IsNullOrEmpty(restartSceneName))
            LoadScene(SceneManager.GetActiveScene().name);
        else
            LoadScene(restartSceneName);
    }

    public void ReturnToLobby()
    {
        LoadScene(lobbySceneName);
    }

    void Awake()
    {
        instance = this;
    }

    // Networked: waits for every client's scene load to finish before evaluating wipes, so stale health can't false-trigger it.
    void Start()
    {
        NetworkManager network = NetworkManager.Singleton;

        if (network != null && network.IsListening && network.SceneManager != null)
            network.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
        else
            ArmReadiness();
    }

    void Update()
    {
        bool victory;

        if (!CanEvaluate())
            return;
        victory = IsBossDefeated();
        if (victory || AllPlayersDead())
            EndRun(CaptureResult(victory));
    }

    public override void OnDestroy()
    {
        if (instance == this)
            instance = null;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        base.OnDestroy();
    }
}
