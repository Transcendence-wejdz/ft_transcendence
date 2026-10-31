using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run result snapshot taken at the wipe or the boss's death.
public partial class GameOverCheck
{
    private static int CaptureXP(XPManager xp)
    {
        if (xp != null)
            return xp.TotalXP;
        if (RunCarryOver.IsForActiveScene)
            return RunCarryOver.TotalXP;
        return 0;
    }

    private static ScoreBreakdown CaptureScore(ScoreManager score)
    {
        if (score != null)
            return score.Breakdown;
        if (RunCarryOver.IsForActiveScene)
            return RunCarryOver.Score;
        return default;
    }

    // Snapshotted at the wipe itself, since the wave timer keeps running behind the Game Over screen; the score stops counting here.
    private static RunResult CaptureResult(bool victory)
    {
        WaveSpawner spawner = FindAnyObjectByType<WaveSpawner>();
        XPManager xp = FindAnyObjectByType<XPManager>();
        ScoreManager score = FindAnyObjectByType<ScoreManager>();
        RunResult snapshot = new RunResult { victory = victory };

        if (spawner != null)
        {
            snapshot.wave = spawner.CurrentWave;
            snapshot.seconds = Mathf.FloorToInt(spawner.ElapsedSeconds);
        }
        score?.Freeze();
        snapshot.runId = Random.Range(1, int.MaxValue);
        snapshot.xp = CaptureXP(xp);
        snapshot.score = CaptureScore(score);
        return snapshot;
    }

    private void EndRun(RunResult snapshot)
    {
        if (IsSpawned)
        {
            result.Value = snapshot;
            gameOver.Value = true;
            return;
        }
        offlineResult = snapshot;
        offlineGameOver = true;
    }
}
