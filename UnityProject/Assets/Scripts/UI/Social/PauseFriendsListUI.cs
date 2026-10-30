using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.UI;

// Pause-menu friends list: collapsible Requests, Online and Offline sections, each scrolling on its own, with presence, invites and request actions.
public class PauseFriendsListUI : MonoBehaviour
{
    [SerializeField] private PauseFriendRowUI rowTemplate;
    [SerializeField] private TMP_Text onlineCountText;
    [SerializeField] private TMP_Text offlineCountText;
    [SerializeField] private TMP_Text requestCountText;
    [SerializeField] private Button onlineToggle;
    [SerializeField] private Button offlineToggle;
    [SerializeField] private RectTransform onlineArrow;
    [SerializeField] private RectTransform offlineArrow;
    [SerializeField] private RectTransform onlineContent;
    [SerializeField] private RectTransform offlineContent;
    [SerializeField] private LayoutElement onlineScroll;
    [SerializeField] private LayoutElement offlineScroll;
    [SerializeField] private float minScrollHeight = 72f;
    [SerializeField] private PauseRequestRowUI requestRowTemplate;
    [SerializeField] private Button requestToggle;
    [SerializeField] private RectTransform requestArrow;
    [SerializeField] private RectTransform requestContent;
    [SerializeField] private LayoutElement requestScroll;
    [SerializeField] private Color requestColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private SocialContextMenuUI contextMenu;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Color onlineColor = new Color(0.45f, 0.8f, 0.45f, 1f);
    [SerializeField] private Color awayColor = new Color(0.95f, 0.75f, 0.3f, 1f);
    [SerializeField] private Color busyColor = new Color(0.9f, 0.4f, 0.35f, 1f);
    [SerializeField] private Color inGameColor = new Color(0.45f, 0.65f, 1f, 1f);
    [SerializeField] private Color offlineColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color errorColor = new Color(0.9f, 0.4f, 0.35f, 1f);
    [SerializeField] private Color infoColor = new Color(0.7f, 0.69f, 0.66f, 1f);

    private readonly List<PauseFriendRowUI> rows = new List<PauseFriendRowUI>();
    private readonly List<PauseRequestRowUI> requestRows = new List<PauseRequestRowUI>();
    private int sessionSignature;

    // Static so a collapsed section stays collapsed across pause openings for the rest of the run.
    private static bool onlineCollapsed;
    private static bool offlineCollapsed;
    private static bool requestsCollapsed;
    private int onlineCount;
    private int offlineCount;
    private int requestCount;

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        requestRowTemplate.gameObject.SetActive(false);
        requestToggle.onClick.AddListener(() => { requestsCollapsed = !requestsCollapsed; Rebuild(); });
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
            FitSections();
            RefreshArrows();
        }
        SetRowsBusy(FriendInviteFlow.IsBusy);
        if (isActiveAndEnabled)
            StartCoroutine(RefitNextFrame());
    }

    private void FitSections()
    {
        FitSection(requestScroll, requestContent, requestsCollapsed, requestCount);
        FitSection(onlineScroll, onlineContent, onlineCollapsed, onlineCount);
        FitSection(offlineScroll, offlineContent, offlineCollapsed, offlineCount);
    }

    // Rows built in the frame the pause opens can be laid out against a 0-tall viewport; refit and snap each list back to its top once settled.
    private IEnumerator RefitNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        FitSections();
        foreach (LayoutElement scroll in new[] { requestScroll, onlineScroll, offlineScroll })
        {
            ScrollRect rect = scroll.GetComponent<ScrollRect>();
            if (rect != null && rect.isActiveAndEnabled)
            {
                rect.StopMovement();
                rect.verticalNormalizedPosition = 1f;
            }
        }
    }

    // Old rows are deactivated before Destroy, since Destroy is deferred and FitSection measures this frame.
    private void BuildRows()
    {
        foreach (PauseFriendRowUI row in rows)
        {
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }
        rows.Clear();
        foreach (PauseRequestRowUI row in requestRows)
        {
            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }
        requestRows.Clear();
        BuildRequestRows();

        var online = new List<Relationship>();
        var offline = new List<Relationship>();
        foreach (Relationship friend in FriendsManager.Friends)
            (IsReachable(friend) ? online : offline).Add(friend);
        online.Sort(CompareByName);
        offline.Sort(CompareByName);
        onlineCount = online.Count;
        offlineCount = offline.Count;

        onlineCountText.text = online.Count.ToString();
        offlineCountText.text = offline.Count.ToString();
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

    // Invite when they can join us; Kick when that friend is in our game and we host; Remove friend always (Kick/Remove confirmed by a second click).
    private void OpenFriendMenu(RectTransform row, string memberId, bool inGame, Action invite)
    {
        var items = new List<SocialContextMenuUI.Item>();
        if (invite != null && !FriendInviteFlow.IsBusy)
            items.Add(new SocialContextMenuUI.Item("Invite", invite));
        Player player = inGame ? SessionKick.FindByServicesId(memberId) : null;
        if (SessionKick.CanKick(player))
            items.Add(new SocialContextMenuUI.Item("Kick", () => KickFriend(player), destructive: true));
        items.Add(new SocialContextMenuUI.Item("Remove friend", () => RunAction(FriendsManager.RemoveAsync(memberId), "Friend removed."), destructive: true));
        contextMenu.Open(row, items);
    }

    private async void KickFriend(Player player)
    {
        bool kicked = await SessionKick.KickAsync(player);
        if (this != null)
            SetStatus(kicked ? "Player kicked." : "Only the host can kick.", isError: !kicked);
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
        {
            string memberId = request.Member.Id;
            AddRequestRow(request, "Wants to be your friend", requestColor,
                () => RunAction(FriendsManager.AcceptAsync(memberId), "Friend added."),
                () => RunAction(FriendsManager.DeclineAsync(memberId), "Request declined."));
        }
        foreach (Relationship request in FriendsManager.Outgoing)
        {
            string memberId = request.Member.Id;
            AddRequestRow(request, "Request sent", offlineColor, null,
                () => RunAction(FriendsManager.CancelRequestAsync(memberId), "Request cancelled."));
        }
    }

    private void AddRequestRow(Relationship request, string status, Color color, Action onAccept, Action onRemove)
    {
        PauseRequestRowUI row = Instantiate(requestRowTemplate, requestContent);
        row.gameObject.SetActive(true);
        row.Setup(FriendsManager.ShortName(request.Member.Profile?.Name), status, color, onAccept, onRemove);
        requestRows.Add(row);
    }

    // The service raises OnChanged after each action, which rebuilds the list; this only reports the outcome.
    private async void RunAction(Task action, string successMessage)
    {
        foreach (PauseRequestRowUI row in requestRows)
            row.SetInteractable(false);
        try
        {
            await action;
            if (this != null)
                SetStatus(successMessage, isError: false);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PauseFriendsListUI: request action failed - {e.Message}");
            if (this != null)
                SetStatus("That didn't work, try again.", isError: true);
        }
        if (this != null)
            foreach (PauseRequestRowUI row in requestRows)
                row.SetInteractable(true);
    }

    // Arrows follow the collapse flags; an empty section hides its arrow image but keeps the title aligned.
    private void RefreshArrows()
    {
        onlineArrow.localRotation = Quaternion.Euler(0f, 0f, onlineCollapsed ? 90f : 0f);
        offlineArrow.localRotation = Quaternion.Euler(0f, 0f, offlineCollapsed ? 90f : 0f);
        requestArrow.localRotation = Quaternion.Euler(0f, 0f, requestsCollapsed ? 90f : 0f);
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
        row.Setup(FriendsManager.ShortName(friend.Member.Profile.Name), status, color, reachable, invite, rect => OpenFriendMenu(rect, memberId, inGame, invite));
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

    // Online, Away and Busy friends can receive invites.
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
