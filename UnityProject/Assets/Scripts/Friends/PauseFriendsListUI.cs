using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.UI;

// Pause-menu friends list: collapsible Online and Offline sections, each scrolling on its own, with presence and an Invite button on reachable friends.
public class PauseFriendsListUI : MonoBehaviour
{
    [SerializeField] private PauseFriendRowUI rowTemplate;
    [SerializeField] private TMP_Text onlineHeader;
    [SerializeField] private TMP_Text offlineHeader;
    [SerializeField] private Button onlineToggle;
    [SerializeField] private Button offlineToggle;
    [SerializeField] private RectTransform onlineArrow;
    [SerializeField] private RectTransform offlineArrow;
    [SerializeField] private RectTransform onlineContent;
    [SerializeField] private RectTransform offlineContent;
    [SerializeField] private LayoutElement onlineScroll;
    [SerializeField] private LayoutElement offlineScroll;
    [SerializeField] private float minScrollHeight = 72f;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Color onlineColor = new Color(0.45f, 0.8f, 0.45f, 1f);
    [SerializeField] private Color awayColor = new Color(0.95f, 0.75f, 0.3f, 1f);
    [SerializeField] private Color busyColor = new Color(0.9f, 0.4f, 0.35f, 1f);
    [SerializeField] private Color inGameColor = new Color(0.45f, 0.65f, 1f, 1f);
    [SerializeField] private Color offlineColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color errorColor = new Color(0.9f, 0.4f, 0.35f, 1f);
    [SerializeField] private Color infoColor = new Color(0.7f, 0.69f, 0.66f, 1f);

    private readonly List<PauseFriendRowUI> rows = new List<PauseFriendRowUI>();
    private int sessionSignature;

    // Static so a collapsed section stays collapsed across pause openings for the rest of the run.
    private static bool onlineCollapsed;
    private static bool offlineCollapsed;
    private int onlineCount;
    private int offlineCount;

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        onlineToggle.onClick.AddListener(() => { onlineCollapsed = !onlineCollapsed; Rebuild(); });
        offlineToggle.onClick.AddListener(() => { offlineCollapsed = !offlineCollapsed; Rebuild(); });
    }

    private void OnEnable()
    {
        FriendsManager.OnChanged += Rebuild;
        FriendInviteFlow.OnBusyChanged += SetRowsBusy;
        sessionSignature = FriendsManager.SessionSignature();
        RefreshArrows();
        Connect();
    }

    // Someone joining or leaving changes "In your game" and who can be invited, with no presence event to tell us.
    private void LateUpdate()
    {
        int signature = FriendsManager.SessionSignature();
        if (signature == sessionSignature)
            return;

        sessionSignature = signature;
        if (FriendsManager.IsReady)
            Rebuild();
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

    // Arrows and section heights are restored even if building the rows fails mid-way (e.g. while a join swaps the session).
    private void Rebuild()
    {
        try
        {
            BuildRows();
        }
        finally
        {
            FitSection(onlineScroll, onlineContent, onlineCollapsed, onlineCount);
            FitSection(offlineScroll, offlineContent, offlineCollapsed, offlineCount);
            RefreshArrows();
        }
        SetRowsBusy(FriendInviteFlow.IsBusy);
    }

    private void BuildRows()
    {
        // Deactivated first: Destroy is deferred, and FitSection measures the content this frame.
        foreach (PauseFriendRowUI row in rows)
        {
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }
        rows.Clear();

        var online = new List<Relationship>();
        var offline = new List<Relationship>();
        foreach (Relationship friend in FriendsManager.Friends)
            (IsReachable(friend) ? online : offline).Add(friend);
        online.Sort(CompareByName);
        offline.Sort(CompareByName);
        onlineCount = online.Count;
        offlineCount = offline.Count;

        onlineHeader.text = $"Online · {online.Count}";
        offlineHeader.text = $"Offline · {offline.Count}";
        offlineToggle.gameObject.SetActive(offline.Count > 0);

        if (!onlineCollapsed)
            foreach (Relationship friend in online)
                AddRow(friend, onlineContent);
        if (!offlineCollapsed)
            foreach (Relationship friend in offline)
                AddRow(friend, offlineContent);

        if (FriendsManager.IsReady && online.Count + offline.Count == 0)
            SetStatus("No friends yet. Add some by name above.", isError: false);
    }

    // Driven only by the collapse flags and counts, so it can never disagree with what the sections show; an empty section has nothing to expand, so its arrow is hidden (image only, keeping the title aligned).
    private void RefreshArrows()
    {
        onlineArrow.localRotation = Quaternion.Euler(0f, 0f, onlineCollapsed ? 90f : 0f);
        offlineArrow.localRotation = Quaternion.Euler(0f, 0f, offlineCollapsed ? 90f : 0f);
        onlineArrow.GetComponent<Image>().enabled = onlineCount > 0;
        offlineArrow.GetComponent<Image>().enabled = offlineCount > 0;
    }

    // Each section asks for its content height; when both don't fit, the layout shrinks them toward minScrollHeight and they scroll on their own.
    private void FitSection(LayoutElement scroll, RectTransform content, bool collapsed, int count)
    {
        bool visible = !collapsed && count > 0;
        scroll.gameObject.SetActive(visible);
        if (!visible)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        float height = LayoutUtility.GetPreferredHeight(content);
        scroll.preferredHeight = height;
        scroll.minHeight = Mathf.Min(height, minScrollHeight);
        scroll.flexibleHeight = 0f;
    }

    private void AddRow(Relationship friend, RectTransform parent)
    {
        string memberId = friend.Member.Id;
        bool reachable = IsReachable(friend);
        bool inGame = reachable && FriendsManager.IsMemberOfCurrentSession(memberId);

        string status;
        Color color;
        Describe(friend, inGame, out status, out color);

        PauseFriendRowUI row = Instantiate(rowTemplate, parent);
        row.gameObject.SetActive(true);
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
