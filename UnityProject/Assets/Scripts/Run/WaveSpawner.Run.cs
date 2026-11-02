using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Run start: the first wave opens the gold run, applies bought upgrades and starts the clock.
public partial class WaveSpawner
{
    // Clients see the first wave through the replicated counter, since RunWaves only runs on the server.
    private void HandleWaveChanged(int previous, int current)
    {
        if (previous == 0 && current > 0)
            NotifyRunStarted();
        ReportCheckpoint(current);
    }

    // The session is locked during a run, so a client spawning into one already started is a player coming back; the host hands back their stacks.
    private void HandleRejoinedRun()
    {
        GoldWallet.RestoreRun();
    }

    // Each peer reports the shared progress on its own account, every few waves of a scene that runs waves.
    private void ReportCheckpoint(int wave)
    {
        ScoreManager score;

        if (!startsRun || wave % GoldWallet.CheckpointEveryWaves != 0)
            return;
        score = FindAnyObjectByType<ScoreManager>();
        if (score != null)
            GoldWallet.ReportCheckpoint(wave, score.Breakdown);
    }

    // Each peer opens the gold run on its own web account and starts its own player at its bought upgrades.
    private void NotifyRunStarted()
    {
        string mode = "coop";

        if (!startsRun)
            return;
        Player.ClearRejoinSnapshots();
        if (Player.AllActiveInstances.Count <= 1)
            mode = "solo";
        GoldWallet.BeginRun(mode);
        StartCoroutine(ApplyPermanentUpgradesWhenReady());
    }

    // Offline the first wave can start before PlayerSpawner has spawned the player, so this waits for it plus one frame of its own setup.
    private IEnumerator ApplyPermanentUpgradesWhenReady()
    {
        Player player = LocalPlayer.GetPlayer();

        while (player == null)
        {
            yield return null;
            player = LocalPlayer.GetPlayer();
        }
        yield return null;
        player.ApplyPermanentUpgrades();
    }

    // The run clock follows the server's, which keeps going while the run waits for a dropped player.
    public void SkipPausedTime(double seconds)
    {
        if (IsSpawned && IsServer)
            startServerTime.Value += seconds;
    }

    // Backdated by alreadyElapsed so a carried-over run keeps counting from where the previous scene stopped.
    private void MarkStartTime(float alreadyElapsed)
    {
        if (IsSpawned)
            startServerTime.Value = NetworkManager.ServerTime.Time - alreadyElapsed;
        else
            offlineStartTime = Time.time - alreadyElapsed;
    }
}
