using TMPro;
using UnityEngine;

// Top-center Coop HUD: survival timer, current wave and run score, shown once the waves start.
public class CoopHUD : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text scoreText;

    private WaveSpawner spawner;
    private GameOverCheck gameOver;
    private ScoreManager score;
    private bool frozen;
    private float frozenElapsed;
    private int shownSeconds = -1;
    private int shownWave = -1;
    private int shownScore = -1;

    private bool FindRunManagers()
    {
        if (spawner != null)
            return true;
        spawner = FindAnyObjectByType<WaveSpawner>();
        gameOver = FindAnyObjectByType<GameOverCheck>();
        score = FindAnyObjectByType<ScoreManager>();
        return spawner != null;
    }

    // Stops the clock at the wipe so the final time stays readable.
    private void FreezeOnGameOver()
    {
        if (frozen || gameOver == null || !gameOver.IsGameOver)
            return;
        frozen = true;
        frozenElapsed = spawner.ElapsedSeconds;
    }

    private float ShownElapsed()
    {
        if (frozen)
            return frozenElapsed;
        return spawner.ElapsedSeconds;
    }

    private int ShownScore()
    {
        if (score == null)
            return 0;
        return score.TotalScore;
    }

    private void RefreshTimer(float elapsed)
    {
        int seconds = Mathf.Max(0, Mathf.FloorToInt(elapsed));

        if (seconds == shownSeconds)
            return;
        shownSeconds = seconds;
        timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
    }

    // Text only changes when a value does, so nothing allocates per frame.
    private void Refresh(float elapsed, int wave, int points)
    {
        RefreshTimer(elapsed);
        if (wave != shownWave)
        {
            shownWave = wave;
            waveText.text = $"Wave {wave}";
        }
        if (scoreText != null && points != shownScore)
        {
            shownScore = points;
            scoreText.text = $"Score {points}";
        }
    }

    void Awake()
    {
        content.SetActive(false);
    }

    void Update()
    {
        bool started;

        if (!FindRunManagers())
            return;
        started = spawner.CurrentWave > 0;
        if (content.activeSelf != started)
            content.SetActive(started);
        if (!started)
            return;
        FreezeOnGameOver();
        Refresh(ShownElapsed(), spawner.CurrentWave, ShownScore());
    }
}
