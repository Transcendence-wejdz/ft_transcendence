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

    // OnWaveStarted only fires server/offline side, so clients never start the transition.
    private void HandleWaveStarted(int wave)
    {
        if (started || string.IsNullOrEmpty(bossSceneName))
            return;

        started = true;
        StartCoroutine(TransitionWhenDue());
    }

    // Waits for the deadline outside any level-up choice; a wipe meanwhile cancels it.
    private IEnumerator TransitionWhenDue()
    {
        LevelUpManager levelUp = FindAnyObjectByType<LevelUpManager>();
        GameOverCheck gameOver = FindAnyObjectByType<GameOverCheck>();
        yield return new WaitUntil(() => spawner.ElapsedSeconds >= bossSceneSeconds && (levelUp == null || !levelUp.SequenceActive));
        if (gameOver != null && gameOver.IsGameOver)
            yield break;

        XPManager xp = FindAnyObjectByType<XPManager>();
        ScoreManager score = FindAnyObjectByType<ScoreManager>();
        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        RunCarryOver.Capture(bossSceneName,
            xp != null ? xp.TotalXP : 0,
            score != null ? score.Breakdown : default,
            score != null ? score.PlayerKills : default,
            spawner.CurrentWave,
            levelUp != null ? levelUp.ProcessedLevel : 1,
            spawner.ElapsedSeconds,
            isNetworked ? null : LocalPlayer.GetPlayer());
        GameModeLoader.LoadNow(bossSceneName);
    }
}
