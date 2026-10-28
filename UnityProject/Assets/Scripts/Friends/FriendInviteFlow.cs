using System;
using System.Threading.Tasks;
using UnityEngine;

// Invite flow for every friends UI, one at a time: solo, the friend is told at once and the code follows once hosting is done.
public static class FriendInviteFlow
{
    public static bool IsBusy { get; private set; }

    // Fires when an invite starts or ends, so every open list can lock its buttons.
    public static event Action<bool> OnBusyChanged;

    // Returns whether it worked and the message to show; progress reports steps like hosting.
    public static async Task<(bool ok, string message)> InviteAsync(string memberId, Action<string> progress = null)
    {
        if (IsBusy)
            return (false, string.Empty);

        SetBusy(true);
        try
        {
            bool notified = false;
            if (string.IsNullOrEmpty(FriendsManager.CurrentSessionCode))
            {
                NetworkBootstrap bootstrap = UnityEngine.Object.FindAnyObjectByType<NetworkBootstrap>(FindObjectsInactive.Include);
                if (bootstrap == null)
                    return (false, "Can't host from here.");

                progress?.Invoke("Invite sent, creating your game...");
                Task notify = FriendsManager.SendPendingInviteAsync(memberId);
                Task<bool> host = bootstrap.HostGameAsync(showLoadingScreen: false);

                try
                {
                    await notify;
                    notified = true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"FriendInviteFlow: instant invite failed, the code invite will still be sent - {e.Message}");
                }

                if (!await host)
                    return (false, "Couldn't start hosting, try again.");
            }

            await FriendsManager.InviteAsync(memberId, followUp: notified);
            return (true, "Invite sent.");
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

    private static void SetBusy(bool busy)
    {
        IsBusy = busy;
        OnBusyChanged?.Invoke(busy);
    }
}
