using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Multiplayer extras of the level-up screen: countdown with its warning heartbeat and the ready list.
public partial class LevelUpUI
{
    private static string ReadyMark(Player player)
    {
        if (player.HasChosenLevelUpBonus)
            return "Ready";
        return "...";
    }

    private void ResetTimerLook()
    {
        timerText.color = timerBaseColor;
        timerText.rectTransform.localScale = Vector3.one;
    }

    // Last seconds: red heartbeat timed locally from each tick, since the server only replicates whole seconds.
    private void PaintTimerWarning(int second)
    {
        float sinceTick;
        float beat;
        Color color = warningColor;

        if (second != lastTimerSecond)
        {
            lastTimerSecond = second;
            lastTimerTickTime = Time.unscaledTime;
        }
        if (second > warningSeconds || second <= 0)
        {
            ResetTimerLook();
            return;
        }
        sinceTick = Time.unscaledTime - lastTimerTickTime;
        beat = 1f - Mathf.Clamp01(sinceTick / Mathf.Max(warningBeatSeconds, MinBeatSeconds));
        color.a = Mathf.Lerp(WarningMinAlpha, 1f, 1f - Mathf.Clamp01(sinceTick));
        timerText.color = color;
        timerText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, warningPulseScale, beat * beat);
    }

    private void RefreshReadyList()
    {
        readyListBuilder.Clear();
        foreach (Player player in Player.AllActiveInstances)
            readyListBuilder.AppendLine($"{player.PlayerName}: {ReadyMark(player)}");
        readyListText.text = readyListBuilder.ToString();
    }

    // Multiplayer only: the countdown and who already chose.
    private void UpdateMultiplayerExtras()
    {
        bool multi = Player.AllActiveInstances.Count > 1;
        int second;

        timerGroup.SetActive(multi);
        readyListGroup.SetActive(multi);
        if (!multi)
            return;
        second = Mathf.CeilToInt(manager.SecondsRemaining);
        timerText.text = second.ToString();
        PaintTimerWarning(second);
        RefreshReadyList();
    }
}
