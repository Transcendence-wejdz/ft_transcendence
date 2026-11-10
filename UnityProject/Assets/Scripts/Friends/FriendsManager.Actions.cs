using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using Unity.Services.Multiplayer;

// Friend actions: invites carrying our session code, and adding, accepting, declining or removing friends.
public static partial class FriendsManager
{
    // Sends the session code to an online friend; followUp completes an earlier instant invite without reopening a dismissed popup.
    public static Task InviteAsync(string memberId, bool followUp = false)
    {
        string code = CurrentSessionCode;

        if (string.IsNullOrEmpty(code))
            throw new InvalidOperationException("No active session to invite to.");
        return FriendsService.Instance.MessageAsync(memberId, new FriendInvite { SessionCode = code, SenderName = OwnName, FollowUp = followUp });
    }

    // Instant heads-up before our session exists, so the friend sees the invite without waiting; InviteAsync sends the code once hosting is done.
    public static Task SendPendingInviteAsync(string memberId)
    {
        return FriendsService.Instance.MessageAsync(memberId, new FriendInvite { SessionCode = string.Empty, SenderName = OwnName });
    }

    // Sends a friend request, or accepts one already received.
    public static async Task AddByNameAsync(string playerName)
    {
        await FriendsService.Instance.AddFriendByNameAsync(playerName);
        RaiseChanged();
    }

    public static async Task AcceptAsync(string memberId)
    {
        await FriendsService.Instance.AddFriendAsync(memberId);
        RaiseChanged();
    }

    public static async Task DeclineAsync(string memberId)
    {
        await FriendsService.Instance.DeleteIncomingFriendRequestAsync(memberId);
        RaiseChanged();
    }

    public static async Task CancelRequestAsync(string memberId)
    {
        await FriendsService.Instance.DeleteOutgoingFriendRequestAsync(memberId);
        RaiseChanged();
    }

    public static async Task RemoveAsync(string memberId)
    {
        await FriendsService.Instance.DeleteFriendAsync(memberId);
        RaiseChanged();
    }
}
