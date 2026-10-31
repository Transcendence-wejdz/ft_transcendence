using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pause-menu friends list: collapsible Requests, Online and Offline sections, each scrolling on its own; rows and their actions live in PauseFriendsListUI.Rows.cs.
public partial class PauseFriendsListUI : MonoBehaviour
{
    private const float CollapsedArrowDegrees = 90f;

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

    private int sessionSignature;

    // Static so a collapsed section stays collapsed across pause openings for the rest of the run.
    private static bool onlineCollapsed;
    private static bool offlineCollapsed;
    private static bool requestsCollapsed;
    private int onlineCount;
    private int offlineCount;
    private int requestCount;

    private static float ArrowAngle(bool collapsed)
    {
        if (collapsed)
            return CollapsedArrowDegrees;
        return 0f;
    }

    private void SetStatus(string message, bool isError)
    {
        statusText.text = message;
        if (isError)
            statusText.color = errorColor;
        else
            statusText.color = infoColor;
    }

    // Arrows follow the collapse flags; an empty section hides its arrow image but keeps the title aligned.
    private void RefreshArrows()
    {
        onlineArrow.localRotation = Quaternion.Euler(0f, 0f, ArrowAngle(onlineCollapsed));
        offlineArrow.localRotation = Quaternion.Euler(0f, 0f, ArrowAngle(offlineCollapsed));
        requestArrow.localRotation = Quaternion.Euler(0f, 0f, ArrowAngle(requestsCollapsed));
        onlineArrow.GetComponent<Image>().enabled = onlineCount > 0;
        offlineArrow.GetComponent<Image>().enabled = offlineCount > 0;
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

    private void ToggleRequests()
    {
        requestsCollapsed = !requestsCollapsed;
        Rebuild();
    }

    private void ToggleOnline()
    {
        onlineCollapsed = !onlineCollapsed;
        Rebuild();
    }

    private void ToggleOffline()
    {
        offlineCollapsed = !offlineCollapsed;
        Rebuild();
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

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        requestRowTemplate.gameObject.SetActive(false);
        requestToggle.onClick.AddListener(ToggleRequests);
        onlineToggle.onClick.AddListener(ToggleOnline);
        offlineToggle.onClick.AddListener(ToggleOffline);
    }

    private void OnEnable()
    {
        FriendsManager.OnChanged += Rebuild;
        FriendInviteFlow.OnBusyChanged += SetRowsBusy;
        sessionSignature = FriendsManager.SessionSignature();
        RefreshArrows();
        Connect();
    }

    private void OnDisable()
    {
        FriendsManager.OnChanged -= Rebuild;
        FriendInviteFlow.OnBusyChanged -= SetRowsBusy;
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
}
