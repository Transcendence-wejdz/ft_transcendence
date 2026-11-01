using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Freezes the run for everyone when a player drops, until nobody is awaited and everyone is ready, then counts down before resuming.
public partial class GameOverCheck
{
    private readonly NetworkVariable<bool> pauseActive = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<double> resumeServerTime = new OfflineSafeNetworkVariable<double>(
        0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private float resumeCountdownSeconds = 3f;

    private readonly Dictionary<string, AwaitedPlayer> awaitedById = new Dictionary<string, AwaitedPlayer>();

    private bool frozeForPlayers;
    private double pausedSince;

    private bool IsCountingDown => IsSpawned && resumeServerTime.Value > 0d;

    private bool IsFrozenForPlayers => IsSpawned && pauseActive.Value;

    public static bool IsWaitingForPlayers => instance != null && instance.IsFrozenForPlayers;

    public static bool IsResuming => instance != null && instance.IsCountingDown;

    // Seconds before the run resumes, on the shared server clock, which keeps running while time is frozen.
    public static float ResumeSecondsLeft
    {
        get
        {
            if (!IsResuming)
                return 0f;
            return Mathf.Max(0f, (float)(instance.resumeServerTime.Value - instance.NetworkManager.ServerTime.Time));
        }
    }

    public static bool CanContinue => IsWaitingForPlayers && instance.IsServer && !instance.IsCountingDown;

    // Host: stops waiting for anyone, absent or not ready, and goes straight to the countdown.
    public static void ContinueWithoutWaiting()
    {
        if (!CanContinue)
            return;
        instance.awaitedById.Clear();
        instance.RefreshPauseReady();
        if (!instance.IsCountingDown)
            instance.StartResumeCountdown();
    }

    private void HandleAwaitedChanged()
    {
        if (awaitedById.Count > 0)
            resumeServerTime.Value = 0d;
        RefreshPauseReady();
    }

    // Server: another drop during the countdown cancels it through HandleAwaitedChanged, so reaching zero means everyone is settled.
    private void TickResumeCountdown()
    {
        double now;

        if (!IsSpawned || !IsServer || !IsCountingDown)
            return;
        now = NetworkManager.ServerTime.Time;
        if (now < resumeServerTime.Value)
            return;
        resumeServerTime.Value = 0d;
        pauseActive.Value = false;
        FindAnyObjectByType<WaveSpawner>()?.SkipPausedTime(now - pausedSince);
    }

    private void BeginPause()
    {
        if (IsFrozenForPlayers)
            return;
        pausedSince = NetworkManager.ServerTime.Time;
        readyClients.Clear();
        pauseActive.Value = true;
    }

    // A player who left on purpose or was kicked isn't coming back, and an old body released by a rejoin has no id left.
    private void HandlePlayerLeft(Player player)
    {
        if (!IsServer || !IsSpawned || IsGameOver || NetworkManager.ShutdownInProgress)
            return;
        readyClients.Remove(player.OwnerClientId);
        if (player.LeftOnPurpose || string.IsNullOrEmpty(player.ServicesId))
        {
            RefreshPauseReady();
            return;
        }
        BeginPause();
        awaitedById[player.ServicesId] = new AwaitedPlayer { name = player.PlayerName, classIndex = player.ClassIndex, colorIndex = player.ColorIndex };
        HandleAwaitedChanged();
    }

    private void HandleServicesIdClaimed(string servicesId)
    {
        if (IsServer && IsSpawned && awaitedById.Remove(servicesId))
            HandleAwaitedChanged();
    }

    // A level-up choice open under the pause keeps its own freeze once the pause ends.
    private void ApplyPauseFreeze(bool frozen)
    {
        LevelUpManager levelUp;

        if (frozen)
        {
            frozeForPlayers = true;
            Time.timeScale = 0f;
            return;
        }
        if (!frozeForPlayers)
            return;
        frozeForPlayers = false;
        levelUp = FindAnyObjectByType<LevelUpManager>();
        if (levelUp == null || !levelUp.SequenceActive)
            Time.timeScale = 1f;
    }

    private void HandlePauseActiveChanged(bool previous, bool current)
    {
        ApplyPauseFreeze(current);
    }

    public override void OnNetworkSpawn()
    {
        pauseActive.OnValueChanged += HandlePauseActiveChanged;
        ApplyPauseFreeze(IsFrozenForPlayers);
    }

    public override void OnNetworkDespawn()
    {
        pauseActive.OnValueChanged -= HandlePauseActiveChanged;
    }
}
