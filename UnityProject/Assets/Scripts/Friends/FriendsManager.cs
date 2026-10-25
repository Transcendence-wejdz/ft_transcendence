using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using Unity.Services.Multiplayer;

// SessionCode is empty for the instant first message sent while the inviter's session is still being created; a second message brings the code.
[Serializable]
public class FriendInvite
{
    public string SessionCode;
    public string SenderName;
    public bool FollowUp;
    [NonSerialized] public string SenderId;
}

[Serializable]
public class PresenceActivity
{
    public string Status = "Lobby";
}

// Static wrapper over Unity Friends with lazy init and an OnChanged event.
public static class FriendsManager
{
    public static event Action OnChanged;
    public static event Action<FriendInvite> OnInviteReceived;

    private static readonly IReadOnlyList<Relationship> Empty = new List<Relationship>();
    private static Task initTask;

    public static bool IsReady { get; private set; }
    public static string OwnName { get; private set; } = string.Empty;

    public static IReadOnlyList<Relationship> Friends => IsReady ? FriendsService.Instance.Friends : Empty;
    public static IReadOnlyList<Relationship> Incoming => IsReady ? FriendsService.Instance.IncomingFriendRequests : Empty;
    public static IReadOnlyList<Relationship> Outgoing => IsReady ? FriendsService.Instance.OutgoingFriendRequests : Empty;

    // Connects at boot in any scene so friends lists open already filled; failures are retried by the lists themselves.
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
    private static async void Preload()
    {
        try
        {
            await InitializeAsync();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"FriendsManager: preload failed, friends lists will retry - {e.Message}");
        }
    }

    // Safe to call repeatedly; concurrent callers share one init.
    public static Task InitializeAsync()
    {
        if (IsReady)
            return Task.CompletedTask;

        if (initTask == null || initTask.IsFaulted || initTask.IsCanceled)
            initTask = InitializeInternalAsync();
        return initTask;
    }

    private static async Task InitializeInternalAsync()
    {
        await ServicesAuth.EnsureSignedInAsync();
        await FriendsService.Instance.InitializeAsync();

        OwnName = await AuthenticationService.Instance.GetPlayerNameAsync();

        ServicesAuth.OnPlayerNameChanged += name => { OwnName = name; OnChanged?.Invoke(); };
        FriendsService.Instance.RelationshipAdded += _ => OnChanged?.Invoke();
        FriendsService.Instance.RelationshipDeleted += _ => OnChanged?.Invoke();
        FriendsService.Instance.PresenceUpdated += _ => OnChanged?.Invoke();
        FriendsService.Instance.MessageReceived += HandleMessage;

        await FriendsService.Instance.SetPresenceAsync(Availability.Online, new PresenceActivity());

        IsReady = true;
        OnChanged?.Invoke();
    }

    // Ignores anything that isn't a friend invite; a code-less invite is valid (its code follows in a second message).
    private static void HandleMessage(IMessageReceivedEvent message)
    {
        FriendInvite invite;
        try
        {
            invite = message.GetAs<FriendInvite>();
        }
        catch (Exception)
        {
            return;
        }

        if (invite == null || string.IsNullOrEmpty(invite.SenderName))
            return;

        invite.SenderId = message.UserId;
        OnInviteReceived?.Invoke(invite);
    }

    // Code of the session we are really in (see ActiveSession), or null when solo.
    public static string CurrentSessionCode
    {
        get
        {
            ISession session = ActiveSession.Current;
            return session != null && !string.IsNullOrEmpty(session.Code) ? session.Code : null;
        }
    }

    // True if that friend is already a member of the session we are really in.
    public static bool IsMemberOfCurrentSession(string memberId)
    {
        ISession session = ActiveSession.Current;
        if (session == null)
            return false;

        foreach (var player in session.Players)
            if (player.Id == memberId)
                return true;
        return false;
    }

    // Changes when the live session starts, ends, or gains or loses a player; open UIs poll it to refresh.
    public static int SessionSignature()
    {
        ISession session = ActiveSession.Current;
        if (session == null)
            return 0;

        int hash = 17;
        foreach (var player in session.Players)
            hash = hash * 31 + player.Id.GetHashCode();
        return hash;
    }

    // Name without the "#1234" suffix.
    public static string ShortName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return string.Empty;

        int hash = fullName.IndexOf('#');
        return hash > 0 ? fullName.Substring(0, hash) : fullName;
    }

    // Sends the session code to an online friend.
    // followUp marks the code-carrying message that completes an earlier instant invite, so a dismissed popup isn't reopened by it.
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
        OnChanged?.Invoke();
    }

    public static async Task AcceptAsync(string memberId)
    {
        await FriendsService.Instance.AddFriendAsync(memberId);
        OnChanged?.Invoke();
    }

    public static async Task DeclineAsync(string memberId)
    {
        await FriendsService.Instance.DeleteIncomingFriendRequestAsync(memberId);
        OnChanged?.Invoke();
    }

    public static async Task CancelRequestAsync(string memberId)
    {
        await FriendsService.Instance.DeleteOutgoingFriendRequestAsync(memberId);
        OnChanged?.Invoke();
    }

    public static async Task RemoveAsync(string memberId)
    {
        await FriendsService.Instance.DeleteFriendAsync(memberId);
        OnChanged?.Invoke();
    }
}
