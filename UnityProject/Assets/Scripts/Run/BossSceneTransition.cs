using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Moves the whole party to the boss scene once a run has lasted bossSceneSeconds, carrying the run state over.
[RequireComponent(typeof(WaveSpawner))]
public class BossSceneTransition : MonoBehaviour
{
    [SerializeField] private string bossSceneName = "Boss";
    [SerializeField] private float bossSceneSeconds = 300f;

    private WaveSpawner spawner;
    private bool started;

    // OnWaveStarted only fires server/offline side, so clients never start the transition.
    private void HandleWaveStarted(int wave)
    {
        if (started || string.IsNullOrEmpty(bossSceneName))
            return;
        started = true;
        StartCoroutine(TransitionWhenDue());
    }

    private bool IsDue(LevelUpManager levelUp)
    {
        if (spawner.ElapsedSeconds < bossSceneSeconds)
            return false;
        return levelUp == null || !levelUp.SequenceActive;
    }

    // Offline, the solo player is a scene object, so it hands its bonus stacks over itself.
    private static Player GetOfflinePlayer()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            return null;
        return LocalPlayer.GetPlayer();
    }

    private void CaptureRun(LevelUpManager levelUp)
    {
        XPManager xp = FindAnyObjectByType<XPManager>();
        ScoreManager score = FindAnyObjectByType<ScoreManager>();
        int totalXP = 0;
        ScoreBreakdown breakdown = default;
        PlayerKillTable kills = default;
        int processedLevel = 1;

        if (xp != null)
            totalXP = xp.TotalXP;
        if (score != null)
        {
            breakdown = score.Breakdown;
            kills = score.PlayerKills;
        }
        if (levelUp != null)
            processedLevel = levelUp.ProcessedLevel;
        RunCarryOver.Capture(bossSceneName, totalXP, breakdown, kills, spawner.CurrentWave,
            processedLevel, spawner.ElapsedSeconds, GetOfflinePlayer());
    }

    // Waits for the deadline outside any level-up choice; a wipe meanwhile cancels it.
    private IEnumerator TransitionWhenDue()
    {
        LevelUpManager levelUp = FindAnyObjectByType<LevelUpManager>();
        GameOverCheck gameOver = FindAnyObjectByType<GameOverCheck>();

        yield return new WaitUntil(() => IsDue(levelUp));
        if (gameOver != null && gameOver.IsGameOver)
            yield break;
        CaptureRun(levelUp);
        GameModeLoader.LoadNow(bossSceneName);
    }

    void Awake()
    {
        spawner = GetComponent<WaveSpawner>();
    }

    void OnEnable()
    {
        WaveSpawner.OnWaveStarted += HandleWaveStarted;
    }

    void OnDisable()
    {
        WaveSpawner.OnWaveStarted -= HandleWaveStarted;
    }
}
