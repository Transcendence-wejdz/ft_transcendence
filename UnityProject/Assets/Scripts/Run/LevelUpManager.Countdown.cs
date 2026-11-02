using Unity.Netcode;
using UnityEngine;

// Countdown between the last level-up choice and the game resuming: time stays frozen and the sequence stays active until it runs out.
public partial class LevelUpManager
{
    [SerializeField] private float resumeCountdownSeconds = 3f;

    private readonly NetworkVariable<double> resumeServerTime = new OfflineSafeNetworkVariable<double>(
        0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float offlineResumeAt;

    public bool IsResuming
    {
        get
        {
            if (IsSpawned)
                return resumeServerTime.Value > 0d;
            return offlineResumeAt > 0f;
        }
    }

    // On the server's clock when networked, unscaled offline: both keep running while time is frozen.
    public float ResumeSecondsLeft
    {
        get
        {
            if (!IsResuming)
                return 0f;
            if (IsSpawned)
                return Mathf.Max(0f, (float)(resumeServerTime.Value - NetworkManager.ServerTime.Time));
            return Mathf.Max(0f, offlineResumeAt - Time.unscaledTime);
        }
    }

    public bool IsChoosing => SequenceActive && !IsResuming;

    private bool HasNextLevelWaiting()
    {
        int level;

        xpManager.GetProgress(out level, out _, out _);
        return level > processedLevel;
    }

    private void StartResumeCountdown()
    {
        if (IsSpawned)
            resumeServerTime.Value = NetworkManager.ServerTime.Time + resumeCountdownSeconds;
        else
            offlineResumeAt = Time.unscaledTime + resumeCountdownSeconds;
    }

    private void TickResumeCountdown()
    {
        if (ResumeSecondsLeft > 0f)
            return;
        if (IsSpawned)
            resumeServerTime.Value = 0d;
        else
            offlineResumeAt = 0f;
        FinishSequence();
    }
}
