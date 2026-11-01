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
public static partial class FriendsManager
{
    public static event Action OnChanged;
    public static event Action<FriendInvite> OnInviteReceived;

    // Live incoming friend request (member id, display name); pending ones from before login are only in Incoming.
    public static event Action<string, string> OnFriendRequestReceived;

    private static readonly IReadOnlyList<Relationship> Empty = new List<Relationship>();
    private static Task initTask;

    public static bool IsReady { get; private set; }
    public static string OwnName { get; private set; } = string.Empty;

    public static IReadOnlyList<Relationship> Friends
    {
        get
        {
            if (!IsReady)
                return Empty;
            return FriendsService.Instance.Friends;
        }
    }

    public static IReadOnlyList<Relationship> Incoming
    {
        get
        {
            if (!IsReady)
                return Empty;
            return FriendsService.Instance.IncomingFriendRequests;
        }
    }

    public static IReadOnlyList<Relationship> Outgoing
    {
        get
        {
            if (!IsReady)
                return Empty;
            return FriendsService.Instance.OutgoingFriendRequests;
        }
    }

    // Code of the session we are really in (see ActiveSession), or null when solo.
    public static string CurrentSessionCode
    {
        get
        {
            ISession session = ActiveSession.Current;

            if (session == null || string.IsNullOrEmpty(session.Code))
                return null;
            return session.Code;
        }
    }

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
        SubscribeToService();
        await FriendsService.Instance.SetPresenceAsync(Availability.Online, new PresenceActivity());
        IsReady = true;
        RaiseChanged();
    }

    // True if that friend is already a member of the session we are really in.
    public static bool IsMemberOfCurrentSession(string memberId)
    {
        ISession session = ActiveSession.Current;

        if (session == null)
            return false;
        foreach (IReadOnlyPlayer player in session.Players)
        {
            if (player.Id == memberId)
                return true;
        }
        return false;
    }

    // Changes when the live session starts, ends, or gains or loses a player; open UIs poll it to refresh.
    public static int SessionSignature()
    {
        ISession session = ActiveSession.Current;
        int hash = 17;

        if (session == null)
            return 0;
        foreach (IReadOnlyPlayer player in session.Players)
            hash = hash * 31 + player.Id.GetHashCode();
        return hash;
    }

    // Name without the "#1234" suffix.
    public static string ShortName(string fullName)
    {
        int hash;

        if (string.IsNullOrEmpty(fullName))
            return string.Empty;
        hash = fullName.IndexOf('#');
        if (hash <= 0)
            return fullName;
        return fullName.Substring(0, hash);
    }

}
