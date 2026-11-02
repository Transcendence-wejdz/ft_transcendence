using System;
using System.Threading.Tasks;
using UnityEngine;

// Run rewards on the website: the report is saved until the server answers, so a lost connection costs no gold.
public static partial class GoldWallet
{
    private const string RewardPath = "/api/game/reward";
    private const string PendingRewardKeyPrefix = "gold.pendingReward.";
    private const int RewardAttempts = 3;
    private const float RewardRetrySeconds = 3f;
    private const int HttpConflict = 409;
    private const int HttpServerError = 500;

    private static bool rewardInFlight;

    private static string PendingRewardKey => PendingRewardKeyPrefix + WebProfileBridge.UserId;

    private static bool IsSuccess(int status)
    {
        return status >= 200 && status < 300;
    }

    private static bool IsTemporaryFailure(int status)
    {
        return status == 0 || status >= HttpServerError;
    }

    private static Task<int> PostRewardAsync(string json)
    {
        TaskCompletionSource<int> answer = new TaskCompletionSource<int>();

        WebBackend.Post(RewardPath, json, answer.SetResult);
        return answer.Task;
    }

    // Unscaled, since the Game Over screen can sit on a frozen game.
    private static async Task WaitRealtimeAsync(float seconds)
    {
        float deadline = Time.realtimeSinceStartup + seconds;

        while (Time.realtimeSinceStartup < deadline)
            await Awaitable.NextFrameAsync();
    }

    private static void ForgetPendingReward()
    {
        PlayerPrefs.DeleteKey(PendingRewardKey);
        PlayerPrefs.Save();
    }

    private static void FinishReward(int status, bool retried, int earned, Action<int> onDone)
    {
        ForgetPendingReward();
        if (!IsSuccess(status) && !(retried && status == HttpConflict))
        {
            onDone?.Invoke(Refused);
            return;
        }
        Credit(earned);
        onDone?.Invoke(earned);
    }

    // A conflict after a retry means an earlier try landed and only its answer was lost, so the gold is paid.
    private static async Task DeliverRewardAsync(string json, int earned, Action<int> onDone)
    {
        bool retried = false;
        int status;

        rewardInFlight = true;
        status = await PostRewardAsync(json);
        for (int i = 1; i < RewardAttempts && IsTemporaryFailure(status); i++)
        {
            retried = true;
            await WaitRealtimeAsync(RewardRetrySeconds);
            status = await PostRewardAsync(json);
        }
        rewardInFlight = false;
        if (IsTemporaryFailure(status))
            onDone?.Invoke(Pending);
        else
            FinishReward(status, retried, earned, onDone);
    }

    // The token is spent on the first claim, so a run can only be rewarded once.
    private static async void ClaimOnServer(RunResult result, Action<int> onDone)
    {
        RunReport report = BuildReport(result);
        string json = JsonUtility.ToJson(report);

        runToken = null;
        PlayerPrefs.DeleteKey(RunTokenKey);
        PlayerPrefs.SetString(PendingRewardKey, json);
        PlayerPrefs.Save();
        await DeliverRewardAsync(json, GoldForScore(report.score), onDone);
    }

    private static async Task SendPendingRewardAsync()
    {
        string json = PlayerPrefs.GetString(PendingRewardKey, string.Empty);

        if (rewardInFlight || string.IsNullOrEmpty(json))
            return;
        await DeliverRewardAsync(json, GoldForScore(JsonUtility.FromJson<RunReport>(json).score), null);
    }

    // Picks up a reward an earlier visit couldn't deliver.
    private static async void ResendPendingReward()
    {
        await SendPendingRewardAsync();
    }

    // Starting a run abandons the unfinished one on the server, so any reward still owed goes first.
    private static async void StartRunOnServer(string json)
    {
        await SendPendingRewardAsync();
        while (rewardInFlight)
            await Awaitable.NextFrameAsync();
        WebBackend.Post("/api/game/run/start", json);
    }
}
