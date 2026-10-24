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
public class GameOverCheck : NetworkBehaviour
{
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string restartSceneName;
    [SerializeField] private float startupGracePeriod = 0.5f;

    private readonly NetworkVariable<bool> gameOver = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<RunResult> result = new OfflineSafeNetworkVariable<RunResult>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool offlineGameOver;
    private RunResult offlineResult;
    private bool isLoading;
    private bool sceneReady;
    private float readyTime;
    private bool bossEngaged;

    public bool IsGameOver => IsSpawned ? gameOver.Value : offlineGameOver;

    public bool CanChoose => this.HasServerAuthority();

    public RunResult Result => IsSpawned ? result.Value : offlineResult;

    public bool IsVictory => IsGameOver && Result.victory;

    // Networked: waits for every client's scene load to finish before evaluating wipes, so stale health can't false-trigger it.
    void Start()
    {
        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworked && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
        else
            ArmReadiness();
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        base.OnDestroy();
    }

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

    void Update()
    {
        if (!this.HasServerAuthority() || IsGameOver || !sceneReady || Time.time < readyTime)
            return;

        if (Boss.Active != null)
            bossEngaged = true;

        bool victory = bossEngaged && Boss.Active == null;
        if (!victory && !AllPlayersDead())
            return;

        RunResult snapshot = CaptureResult(victory);
        if (IsSpawned)
        {
            result.Value = snapshot;
            gameOver.Value = true;
        }
        else
        {
            offlineResult = snapshot;
            offlineGameOver = true;
        }
    }

    // Snapshotted at the wipe itself, since the wave timer keeps running behind the Game Over screen; the score stops counting here.
    private static RunResult CaptureResult(bool victory)
    {
        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        XPManager xp = FindAnyObjectByType<XPManager>();
        ScoreManager score = FindAnyObjectByType<ScoreManager>();
        float elapsed = spawner != null ? spawner.ElapsedSeconds : 0f;
        score?.Freeze();

        return new RunResult
        {
            runId = Random.Range(1, int.MaxValue),
            wave = spawner != null ? spawner.CurrentWave : 0,
            xp = xp != null ? xp.TotalXP : RunCarryOver.IsForActiveScene ? RunCarryOver.TotalXP : 0,
            seconds = Mathf.FloorToInt(elapsed),
            score = score != null ? score.Breakdown : RunCarryOver.IsForActiveScene ? RunCarryOver.Score : default,
            victory = victory,
        };
    }

    public void Restart()
    {
        LoadScene(string.IsNullOrEmpty(restartSceneName) ? SceneManager.GetActiveScene().name : restartSceneName);
    }

    public void ReturnToLobby()
    {
        LoadScene(lobbySceneName);
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

    private bool AllPlayersDead()
    {
        if (Player.AllActiveInstances.Count == 0)
        {
            GameObject localPlayer = LocalPlayer.Get();
            Player localInstance = localPlayer != null ? localPlayer.GetComponent<Player>() : null;
            return localInstance != null && localInstance.CurrentHealth <= 0f;
        }

        foreach (Player player in Player.AllActiveInstances)
        {
            if (player.CurrentHealth > 0f)
                return false;
        }
        return true;
    }
}
