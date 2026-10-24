using UnityEngine.SceneManagement;

// Server-side run state handed to the next scene, since XP, timer and level-up managers are scene objects rebuilt on load.
public static class RunCarryOver
{
    private static string targetScene;

    public static int TotalXP { get; private set; }
    public static ScoreBreakdown Score { get; private set; }
    public static PlayerKillTable Kills { get; private set; }
    public static int Wave { get; private set; }
    public static int ProcessedLevel { get; private set; }
    public static float ElapsedSeconds { get; private set; }
    public static bool HasOfflineBonusStacks { get; private set; }
    public static LevelUpBonusStacks OfflineBonusStacks { get; private set; }

    // True only in the scene the state was captured for, so a later fresh run never reads stale values.
    public static bool IsForActiveScene => targetScene != null && targetScene == SceneManager.GetActiveScene().name;

    // Offline solo players are scene objects too, so their bonus stacks travel here instead of on a persistent NetworkObject.
    public static void Capture(string scene, int totalXP, ScoreBreakdown score, PlayerKillTable kills, int wave, int processedLevel, float elapsedSeconds, Player offlinePlayer)
    {
        targetScene = scene;
        TotalXP = totalXP;
        Score = score;
        Kills = kills;
        Wave = wave;
        ProcessedLevel = processedLevel;
        ElapsedSeconds = elapsedSeconds;
        HasOfflineBonusStacks = offlinePlayer != null;
        OfflineBonusStacks = offlinePlayer != null ? offlinePlayer.BonusStacks : default;
    }
}
