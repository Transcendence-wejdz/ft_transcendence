using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Friends.Models;
using UnityEngine;

// Pause-menu friends list: Online and Offline sections with presence, and an Invite button on reachable friends.
public class PauseFriendsListUI : MonoBehaviour
{
    [SerializeField] private PauseFriendRowUI rowTemplate;
    [SerializeField] private TMP_Text onlineHeader;
    [SerializeField] private TMP_Text offlineHeader;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Color onlineColor = new Color(0.45f, 0.8f, 0.45f, 1f);
    [SerializeField] private Color awayColor = new Color(0.95f, 0.75f, 0.3f, 1f);
    [SerializeField] private Color busyColor = new Color(0.9f, 0.4f, 0.35f, 1f);
    [SerializeField] private Color inGameColor = new Color(0.45f, 0.65f, 1f, 1f);
    [SerializeField] private Color offlineColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color errorColor = new Color(0.9f, 0.4f, 0.35f, 1f);
    [SerializeField] private Color infoColor = new Color(0.7f, 0.69f, 0.66f, 1f);

    private readonly List<PauseFriendRowUI> rows = new List<PauseFriendRowUI>();

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        FriendsManager.OnChanged += Rebuild;
        FriendInviteFlow.OnBusyChanged += SetRowsBusy;
        Connect();
    }

    private void OnDisable()
    {
        FriendsManager.OnChanged -= Rebuild;
        FriendInviteFlow.OnBusyChanged -= SetRowsBusy;
    }

    private async void Connect()
    {
        if (!FriendsManager.IsReady)
            SetStatus("Connecting...", isError: false);
        try
        {
            await FriendsManager.InitializeAsync();
            if (this != null)
                SetStatus(string.Empty, isError: false);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PauseFriendsListUI: friends service unavailable - {e.Message}");
            if (this != null)
                SetStatus("Friends unavailable.", isError: true);
        }
        if (this != null)
            Rebuild();
    }

    // Reorders headers and rows as siblings: Online header, online rows, Offline header, offline rows.
    private void Rebuild()
    {
        foreach (PauseFriendRowUI row in rows)
            Destroy(row.gameObject);
        rows.Clear();

        var online = new List<Relationship>();
        var offline = new List<Relationship>();
        foreach (Relationship friend in FriendsManager.Friends)
            (IsReachable(friend) ? online : offline).Add(friend);
        online.Sort(CompareByName);
        offline.Sort(CompareByName);

        int index = 0;
        onlineHeader.gameObject.SetActive(true);
        onlineHeader.text = $"Online · {online.Count}";
        onlineHeader.transform.SetSiblingIndex(index++);
        foreach (Relationship friend in online)
            AddRow(friend, ref index);

        offlineHeader.gameObject.SetActive(offline.Count > 0);
        offlineHeader.text = $"Offline · {offline.Count}";
        offlineHeader.transform.SetSiblingIndex(index++);
        foreach (Relationship friend in offline)
            AddRow(friend, ref index);

        if (FriendsManager.IsReady && online.Count + offline.Count == 0)
            SetStatus("No friends yet. Add some from the Lobby.", isError: false);
        SetRowsBusy(FriendInviteFlow.IsBusy);
    }

    private void AddRow(Relationship friend, ref int index)
    {
        string memberId = friend.Member.Id;
        bool reachable = IsReachable(friend);
        bool inGame = reachable && FriendsManager.IsMemberOfCurrentSession(memberId);

        string status;
        Color color;
        Describe(friend, inGame, out status, out color);

        PauseFriendRowUI row = Instantiate(rowTemplate, rowTemplate.transform.parent);
        row.gameObject.SetActive(true);
        row.transform.SetSiblingIndex(index++);
        Action invite = reachable && !inGame ? () => Invite(memberId) : (Action)null;
        row.Setup(FriendsManager.ShortName(friend.Member.Profile.Name), status, color, reachable, invite);
        rows.Add(row);
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

    private static Availability GetAvailability(Relationship friend)
    {
        return friend.Member.Presence?.Availability ?? Availability.Unknown;
    }

    // Same rule as the Lobby friends panel: Online, Away and Busy can receive invites.
    private static bool IsReachable(Relationship friend)
    {
        Availability availability = GetAvailability(friend);
        return availability == Availability.Online
            || availability == Availability.Away
            || availability == Availability.Busy;
    }

    private static int CompareByName(Relationship a, Relationship b)
    {
        return string.Compare(a.Member.Profile.Name, b.Member.Profile.Name, StringComparison.OrdinalIgnoreCase);
    }

    private async void Invite(string memberId)
    {
        if (FriendInviteFlow.IsBusy)
            return;

        var result = await FriendInviteFlow.InviteAsync(memberId, message => SetStatus(message, isError: false));
        if (this != null)
            SetStatus(result.message, isError: !result.ok);
    }

    private void SetRowsBusy(bool busy)
    {
        foreach (PauseFriendRowUI row in rows)
            row.SetInteractable(!busy);
    }

    private void SetStatus(string message, bool isError)
    {
        statusText.text = message;
        statusText.color = isError ? errorColor : infoColor;
    }
}
