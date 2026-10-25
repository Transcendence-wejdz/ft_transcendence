using System;
using System.Threading.Tasks;
using UnityEngine;

// Shared invite flow for every friends UI: hosts a session first when solo, then sends the invite, one invite at a time.
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
            if (string.IsNullOrEmpty(FriendsManager.CurrentSessionCode))
            {
                NetworkBootstrap bootstrap = UnityEngine.Object.FindAnyObjectByType<NetworkBootstrap>(FindObjectsInactive.Include);
                if (bootstrap == null)
                    return (false, "Can't host from here.");

                progress?.Invoke("Creating your game...");
                if (!await bootstrap.HostGameAsync())
                    return (false, "Couldn't start hosting, try again.");
            }

            await FriendsManager.InviteAsync(memberId);
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
