using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Friends.Models;
using UnityEngine;

// Friends list rows: presence lines, invites, the per-friend menu and the request actions.
public partial class PauseFriendsListUI
{
    private readonly List<PauseFriendRowUI> rows = new List<PauseFriendRowUI>();
    private readonly List<PauseRequestRowUI> requestRows = new List<PauseRequestRowUI>();

    private static Availability GetAvailability(Relationship friend)
    {
        return friend.Member.Presence?.Availability ?? Availability.Unknown;
    }

    // Online, Away and Busy friends can receive invites.
    private static bool IsReachable(Relationship friend)
    {
        Availability availability = GetAvailability(friend);

        return availability == Availability.Online || availability == Availability.Away || availability == Availability.Busy;
    }

    private static int CompareByName(Relationship a, Relationship b)
    {
        return string.Compare(a.Member.Profile.Name, b.Member.Profile.Name, StringComparison.OrdinalIgnoreCase);
    }

    private void Describe(Relationship friend, bool inGame, out string status, out Color color)
    {
        if (inGame)
        {
            status = "In your game";
            color = inGameColor;
            return;
        }
        switch (GetAvailability(friend))
        {
            case Availability.Online: status = "Online"; color = onlineColor; break;
            case Availability.Away: status = "Away"; color = awayColor; break;
            case Availability.Busy: status = "Busy"; color = busyColor; break;
            default: status = "Offline"; color = offlineColor; break;
        }
    }

    // Friends already in our game or offline, and everyone outside the Lobby, get no Invite button.
    private Action InviteFor(string memberId, bool reachable, bool inGame)
    {
        if (!reachable || inGame || !FriendInviteFlow.CanInvite)
            return null;
        return () => Invite(memberId);
    }

    private void AddRow(Relationship friend, RectTransform parent)
    {
        string memberId = friend.Member.Id;
        bool reachable = IsReachable(friend);
        bool inGame = reachable && FriendsManager.IsMemberOfCurrentSession(memberId);
        Action invite = InviteFor(memberId, reachable, inGame);
        PauseFriendRowUI row = Instantiate(rowTemplate, parent);
        string status;
        Color color;

        Describe(friend, inGame, out status, out color);
        row.gameObject.SetActive(true);
        row.Setup(memberId, FriendsManager.ShortName(friend.Member.Profile.Name), status, color, reachable, invite, rect => OpenFriendMenu(rect, memberId, inGame, invite));
        rows.Add(row);
    }

    private void AddRequestRow(Relationship request, string status, Color color, Action onAccept, Action onRemove)
    {
        PauseRequestRowUI row = Instantiate(requestRowTemplate, requestContent);

        row.gameObject.SetActive(true);
        row.Setup(request.Member.Id, FriendsManager.ShortName(request.Member.Profile?.Name), status, color, onAccept, onRemove);
        requestRows.Add(row);
    }

    private void AddIncomingRow(Relationship request)
    {
        string memberId = request.Member.Id;

        AddRequestRow(request, "Wants to be your friend", requestColor,
            () => RunAction(FriendsManager.AcceptAsync(memberId), "Friend added."),
            () => RunAction(FriendsManager.DeclineAsync(memberId), "Request declined."));
    }

    private void AddOutgoingRow(Relationship request)
    {
        string memberId = request.Member.Id;

        AddRequestRow(request, "Request sent", offlineColor, null,
            () => RunAction(FriendsManager.CancelRequestAsync(memberId), "Request cancelled."));
    }

    // Incoming requests can be accepted or declined; our own sent ones can be cancelled.
    private void BuildRequestRows()
    {
        requestCount = FriendsManager.Incoming.Count + FriendsManager.Outgoing.Count;
        requestCountText.text = requestCount.ToString();
        requestToggle.gameObject.SetActive(requestCount > 0);
        if (requestsCollapsed)
            return;
        foreach (Relationship request in FriendsManager.Incoming)
            AddIncomingRow(request);
        foreach (Relationship request in FriendsManager.Outgoing)
            AddOutgoingRow(request);
    }

    // Old rows are deactivated before Destroy, since Destroy is deferred and FitSection measures this frame.
    private static void ClearRows<T>(List<T> list) where T : Component
    {
        foreach (T row in list)
        {
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }
        list.Clear();
    }

    private static void SplitByPresence(List<Relationship> online, List<Relationship> offline)
    {
        foreach (Relationship friend in FriendsManager.Friends)
        {
            if (IsReachable(friend))
                online.Add(friend);
            else
                offline.Add(friend);
        }
        online.Sort(CompareByName);
        offline.Sort(CompareByName);
    }

    private void AddSection(List<Relationship> friends, RectTransform content, bool collapsed)
    {
        if (collapsed)
            return;
        foreach (Relationship friend in friends)
            AddRow(friend, content);
    }

    private void BuildRows()
    {
        List<Relationship> online = new List<Relationship>();
        List<Relationship> offline = new List<Relationship>();

        ClearRows(rows);
        ClearRows(requestRows);
        BuildRequestRows();
        SplitByPresence(online, offline);
        onlineCount = online.Count;
        offlineCount = offline.Count;
        onlineCountText.text = online.Count.ToString();
        offlineCountText.text = offline.Count.ToString();
        offlineToggle.gameObject.SetActive(offline.Count > 0);
        AddSection(online, onlineContent, onlineCollapsed);
        AddSection(offline, offlineContent, offlineCollapsed);
        if (FriendsManager.IsReady && online.Count + offline.Count == 0)
            SetStatus("No friends yet. Add some by name above.", isError: false);
    }
}
