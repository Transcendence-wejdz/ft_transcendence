using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.UI;

// Friends menu: own name, add-by-name, friends and requests.
public class FriendsPanelUI : MonoBehaviour
{
    [SerializeField] private FriendRowUI rowTemplate;
    [SerializeField] private TMP_Text ownNameText;
    [SerializeField] private TMP_InputField addInput;
    [SerializeField] private Button addButton;
    [SerializeField] private Button copyNameButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;

    private static readonly Color ErrorColor = new Color(0.75f, 0.15f, 0.15f);
    private static readonly Color InfoColor = new Color(0.2f, 0.12f, 0.08f);
    private readonly List<GameObject> rows = new List<GameObject>();

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        addButton.onClick.AddListener(AddFriend);
        if (copyNameButton != null)
            copyNameButton.onClick.AddListener(CopyOwnName);
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void OnEnable()
    {
        FriendsManager.OnChanged += Rebuild;
        FriendInviteFlow.OnBusyChanged += SetRowsInteractable;
        Connect();
    }

    // Also runs when the parent panel closes, so the overlay never reopens stale.
    private void OnDisable()
    {
        FriendsManager.OnChanged -= Rebuild;
        FriendInviteFlow.OnBusyChanged -= SetRowsInteractable;
        gameObject.SetActive(false);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private async void Connect()
    {
        SetStatus("Connecting...", isError: false);
        try
        {
            await FriendsManager.InitializeAsync();
            SetStatus(string.Empty, isError: false);
        }
        catch (Exception e)
        {
            Debug.LogError($"FriendsPanelUI: failed to connect - {e.Message}");
            SetStatus("Couldn't reach the friends service.", isError: true);
        }
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (GameObject row in rows)
            Destroy(row);
        rows.Clear();

        ownNameText.text = FriendsManager.IsReady ? FriendsManager.OwnName : string.Empty;
        addButton.interactable = FriendsManager.IsReady;

        foreach (Relationship request in FriendsManager.Incoming)
            AddRow(request, "Request", "Accept", () => _ = Run(FriendsManager.AcceptAsync(request.Member.Id)),
                "Decline", () => _ = Run(FriendsManager.DeclineAsync(request.Member.Id)));

        foreach (Relationship friend in FriendsManager.Friends)
        {
            bool inSession = FriendsManager.IsMemberOfCurrentSession(friend.Member.Id);
            string tag = inSession ? "In session" : PresenceTag(friend);
            Action invite = inSession ? null : () => Invite(friend.Member.Id);
            AddRow(friend, tag, "Invite", invite, "Remove", () => _ = Run(FriendsManager.RemoveAsync(friend.Member.Id)));
        }

        foreach (Relationship request in FriendsManager.Outgoing)
            AddRow(request, "Request", "Cancel", () => _ = Run(FriendsManager.CancelRequestAsync(request.Member.Id)));

        SetRowsInteractable(FriendInviteFlow.IsBusy);
    }

    // Online for any reachable availability, otherwise Offline.
    private static string PresenceTag(Relationship friend)
    {
        Availability availability = friend.Member.Presence?.Availability ?? Availability.Unknown;
        bool online = availability == Availability.Online || availability == Availability.Busy || availability == Availability.Away;
        return online ? "Online" : "Offline";
    }

    private void AddRow(Relationship relationship, string tag, string primaryText, Action onPrimary, string secondaryText = null, Action onSecondary = null)
    {
        FriendRowUI row = Instantiate(rowTemplate, rowTemplate.transform.parent);
        row.gameObject.SetActive(true);
        row.Setup(relationship.Member.Profile.Name, tag, primaryText, onPrimary, secondaryText, onSecondary);
        rows.Add(row.gameObject);
    }

    private async void AddFriend()
    {
        string friendName = addInput.text.Trim();
        if (string.IsNullOrEmpty(friendName))
        {
            SetStatus("Enter a player name (Name#1234).", isError: true);
            return;
        }

        addButton.interactable = false;
        try
        {
            await FriendsManager.AddByNameAsync(friendName);
            addInput.text = string.Empty;
            SetStatus("Request sent.", isError: false);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"FriendsPanelUI: add friend failed - {e.Message}");
            SetStatus("Couldn't add that player - check the name.", isError: true);
        }
        addButton.interactable = FriendsManager.IsReady;
    }

    // Rows stay locked while any invite (from this or another friends list) is in flight.
    private async void Invite(string memberId)
    {
        if (FriendInviteFlow.IsBusy)
            return;

        var result = await FriendInviteFlow.InviteAsync(memberId, message => SetStatus(message, isError: false));
        if (this != null)
            SetStatus(result.message, isError: !result.ok);
    }

    private void SetRowsInteractable(bool busy)
    {
        foreach (GameObject row in rows)
            row.GetComponent<FriendRowUI>().SetInteractable(!busy);
    }

    // Runs a row action and shows failures in the status line.
    private async Task Run(Task action, string successMessage = "")
    {
        try
        {
            await action;
            SetStatus(successMessage, isError: false);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"FriendsPanelUI: action failed - {e.Message}");
            SetStatus("That didn't work, try again.", isError: true);
        }
    }

    private void CopyOwnName()
    {
        GUIUtility.systemCopyBuffer = FriendsManager.OwnName;
        SetStatus("Name copied.", isError: false);
    }

    private void SetStatus(string message, bool isError)
    {
        statusText.text = message;
        statusText.color = isError ? ErrorColor : InfoColor;
    }
}
