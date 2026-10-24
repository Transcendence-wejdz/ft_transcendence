using System;
using UnityEngine;

// Lobby currency: on the website the account holds it and the server checks each run against its own clock; desktop builds and the Editor keep it in a local file.
public static class GoldWallet
{
    public const int ScorePerGold = 1000;

    public const int Offline = -1;
    public const int Refused = -2;

    [Serializable]
    private class RunStart
    {
        public string runToken;
        public string mode;
    }

    [Serializable]
    private class RunReport
    {
        public string runToken;
        public int runId;
        public int score;
        public int wave;
        public int seconds;
        public int kills;
        public bool victory;
    }

    private static string runToken;
    private static int serverBalance;

    public static event Action<int> OnChanged;

#if UNITY_WEBGL && !UNITY_EDITOR
    public static bool UsesServer => true;
#else
    public static bool UsesServer => false;
#endif

    public static bool CanEarn => !UsesServer || WebProfileBridge.HasProfile;

    public static int Balance => UsesServer ? serverBalance : LocalProgressStore.Gold;

    public static int GoldForScore(int score)
    {
        return Mathf.Max(0, score) / ScorePerGold;
    }

    // Called on every peer when the first wave of a fresh run starts; on the website a new token abandons any unfinished one server-side.
    public static void BeginRun(string mode)
    {
        if (!UsesServer || !CanEarn)
            return;

        runToken = Guid.NewGuid().ToString();
        WebBackend.Post("/api/game/run/start", JsonUtility.ToJson(new RunStart { runToken = runToken, mode = mode }));
    }

    // onDone gets the gold credited, Offline on the website without an account, or Refused when the server rejects the run.
    public static void ClaimRunReward(RunResult result, Action<int> onDone)
    {
        if (!UsesServer)
        {
            int localEarned = GoldForScore(result.score.Total);
            Credit(localEarned);
            onDone?.Invoke(localEarned);
            return;
        }
        if (!CanEarn)
        {
            onDone?.Invoke(Offline);
            return;
        }
        if (string.IsNullOrEmpty(runToken))
        {
            onDone?.Invoke(Refused);
            return;
        }

        RunReport report = new RunReport
        {
            runToken = runToken,
            runId = result.runId,
            score = result.score.Total,
            wave = result.wave,
            seconds = result.seconds,
            kills = result.score.kills,
            victory = result.victory
        };
        runToken = null;
        int earned = GoldForScore(report.score);
        WebBackend.Post("/api/game/reward", JsonUtility.ToJson(report), status =>
        {
            bool accepted = status >= 200 && status < 300;
            if (accepted)
                Credit(earned);
            onDone?.Invoke(accepted ? earned : Refused);
        });
    }

    // Called with the account's balance whenever the web page sends the profile; ignored outside the website.
    public static void SetFromServer(int gold)
    {
        if (!UsesServer)
            return;

        serverBalance = Mathf.Max(0, gold);
        OnChanged?.Invoke(serverBalance);
    }

    // Desktop/Editor only: takes the gold from the local save, or returns false when there isn't enough.
    public static bool TrySpendLocal(int cost)
    {
        if (UsesServer || cost < 0 || LocalProgressStore.Gold < cost)
            return false;

        LocalProgressStore.Gold -= cost;
        OnChanged?.Invoke(Balance);
        return true;
    }

    // Website only: mirrors a purchase the server already charged.
    public static void ApplyServerSpend(int cost)
    {
        if (!UsesServer)
            return;

        serverBalance = Mathf.Max(0, serverBalance - cost);
        OnChanged?.Invoke(serverBalance);
    }

    private static void Credit(int gold)
    {
        if (gold <= 0)
            return;

        if (UsesServer)
            serverBalance += gold;
        else
            LocalProgressStore.Gold += gold;
        OnChanged?.Invoke(Balance);
    }
}
