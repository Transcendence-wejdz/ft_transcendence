using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;

// Ready check on the disconnect pause: the run resumes once nobody is awaited and every present player is ready.
public partial class GameOverCheck
{
    public enum PausePlayerState { Disconnected, NotReady, Ready }

    // What the roster still shows of a player whose object is gone.
    private struct AwaitedPlayer
    {
        public string name;
        public int classIndex;
        public int colorIndex;
    }

    public const char PauseRosterLineSeparator = '\n';
    public const char PauseRosterFieldSeparator = '|';

    private readonly NetworkVariable<FixedString512Bytes> pauseRoster = new OfflineSafeNetworkVariable<FixedString512Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly HashSet<ulong> readyClients = new HashSet<ulong>();

    public static string PauseRoster
    {
        get
        {
            if (!IsWaitingForPlayers)
                return string.Empty;
            return instance.pauseRoster.Value.ToString();
        }
    }

    public static void SetLocalReady()
    {
        if (IsWaitingForPlayers)
            instance.SetReadyServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetReadyServerRpc(RpcParams rpcParams = default)
    {
        readyClients.Add(rpcParams.Receive.SenderClientId);
        RefreshPauseReady();
    }

    private void StartResumeCountdown()
    {
        resumeServerTime.Value = NetworkManager.ServerTime.Time + resumeCountdownSeconds;
    }

    // One "state|class|color|name" line, the name last and cleaned of separators.
    private static void AppendRosterLine(StringBuilder roster, PausePlayerState state, int classIndex, int colorIndex, string playerName)
    {
        if (roster.Length > 0)
            roster.Append(PauseRosterLineSeparator);
        roster.Append((int)state).Append(PauseRosterFieldSeparator);
        roster.Append(classIndex).Append(PauseRosterFieldSeparator);
        roster.Append(colorIndex).Append(PauseRosterFieldSeparator);
        roster.Append(playerName.Replace(PauseRosterFieldSeparator, ' '));
    }

    // Server, on every change of who is present, awaited or ready.
    private void RefreshPauseReady()
    {
        IReadOnlyList<Player> players = Player.ActivePlayers;
        StringBuilder roster = new StringBuilder();
        FixedString512Bytes published = default;
        PausePlayerState state;
        int ready = 0;

        if (!IsFrozenForPlayers)
            return;
        foreach (Player player in players)
        {
            state = PausePlayerState.NotReady;
            if (readyClients.Contains(player.OwnerClientId))
            {
                ready++;
                state = PausePlayerState.Ready;
            }
            AppendRosterLine(roster, state, player.ClassIndex, player.ColorIndex, player.CurrentDisplayName);
        }
        foreach (AwaitedPlayer absent in awaitedById.Values)
            AppendRosterLine(roster, PausePlayerState.Disconnected, absent.classIndex, absent.colorIndex, absent.name);
        published.CopyFromTruncated(roster.ToString());
        pauseRoster.Value = published;
        if (awaitedById.Count == 0 && ready >= players.Count && !IsCountingDown)
            StartResumeCountdown();
    }
}
