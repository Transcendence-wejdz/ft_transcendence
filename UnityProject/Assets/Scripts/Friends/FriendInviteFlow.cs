using System;
using System.Threading.Tasks;
using UnityEngine;

// Invite flow for every friends UI, one at a time: solo, the friend is told at once and the code follows once hosting is done.
public static class FriendInviteFlow
{
    public static bool IsBusy { get; private set; }

    // A game can only be joined from the Lobby, so invites stop once the run has started.
    public static bool CanInvite => ActiveSession.InLobby;

    // Fires when an invite starts or ends, so every open list can lock its buttons.
    public static event Action<bool> OnBusyChanged;

    private static void SetBusy(bool busy)
    {
        IsBusy = busy;
        OnBusyChanged?.Invoke(busy);
    }

    // The instant heads-up is best effort: if it fails, the code invite is still sent once hosting is done.
    private static async Task<bool> TrySendPendingAsync(Task notify)
    {
        try
        {
            await notify;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"FriendInviteFlow: instant invite failed, the code invite will still be sent - {e.Message}");
            return false;
        }
    }

    // Solo: tells the friend at once while hosting starts; returns an error message, or null once hosting is up.
    private static async Task<(string error, bool notified)> HostAndNotifyAsync(string memberId, Action<string> progress)
    {
        NetworkBootstrap bootstrap = UnityEngine.Object.FindAnyObjectByType<NetworkBootstrap>(FindObjectsInactive.Include);
        Task notify;
        Task<bool> host;
        bool notified;

        if (bootstrap == null)
            return ("Can't host from here.", false);
        progress?.Invoke("Invite sent, creating your game...");
        notify = FriendsManager.SendPendingInviteAsync(memberId);
        host = bootstrap.HostGameAsync(showLoadingScreen: false);
        notified = await TrySendPendingAsync(notify);
        if (!await host)
            return ("Couldn't start hosting, try again.", notified);
        return (null, notified);
    }

    private static async Task<(bool ok, string message)> RunInviteAsync(string memberId, Action<string> progress)
    {
        (string error, bool notified) hosting = (null, false);

        if (string.IsNullOrEmpty(FriendsManager.CurrentSessionCode))
        {
            hosting = await HostAndNotifyAsync(memberId, progress);
            if (hosting.error != null)
                return (false, hosting.error);
        }
        await FriendsManager.InviteAsync(memberId, followUp: hosting.notified);
        return (true, "Invite sent.");
    }

    // Returns whether it worked and the message to show; progress reports steps like hosting.
    public static async Task<(bool ok, string message)> InviteAsync(string memberId, Action<string> progress = null)
    {
        if (IsBusy)
            return (false, string.Empty);
        if (!CanInvite)
            return (false, "You can only invite from the Lobby.");
        SetBusy(true);
        try
        {
            return await RunInviteAsync(memberId, progress);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"FriendInviteFlow: invite failed - {e.Message}");
            return (false, "That didn't work, try again.");
        }
        finally
        {
            SetBusy(false);
        }
    }
}
