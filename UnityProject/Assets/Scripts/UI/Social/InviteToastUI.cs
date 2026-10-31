using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Shows a friend's invite with Join/Decline on its own top canvas (clickable over the pause menu); stays on an always-active object.
public class InviteToastUI : MonoBehaviour
{
    [SerializeField] private ToastCard card;
    [SerializeField] private TMP_Text senderText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image avatar;
    [SerializeField] private TMP_Text avatarInitial;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text joinLabel;
    [SerializeField] private Button dismissButton;
    [SerializeField] private NetworkBootstrap bootstrap;
    [SerializeField] private Color messageColor = new Color(0.7f, 0.69f, 0.66f, 1f);
    [SerializeField] private Color warningColor = new Color(0.95f, 0.75f, 0.3f, 1f);

    private string pendingCode;
    private string pendingSenderId;
    private bool waitingForCode;

    private static bool IsInSession()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private void SetMessage(string text, Color color)
    {
        messageText.text = text;
        messageText.color = color;
    }

    private void SetJoinBusy(bool busy)
    {
        joinButton.interactable = !busy;
        if (busy)
            joinLabel.text = "...";
        else
            joinLabel.text = "Join";
    }

    // A follow-up only completes the open popup from the same friend with its code; it never reopens a dismissed or expired one.
    private void CompleteWithCode(FriendInvite invite)
    {
        if (!card.IsShown || invite.SenderId != pendingSenderId || string.IsNullOrEmpty(invite.SessionCode))
            return;
        pendingCode = invite.SessionCode;
        if (waitingForCode)
            Join();
    }

    // Joining from a game already running warns that it will be left.
    private void ShowInviteMessage()
    {
        if (IsInSession())
            SetMessage("Joining leaves your current game", warningColor);
        else
            SetMessage("invited you to play", messageColor);
    }

    private void Show(FriendInvite invite)
    {
        string sender;

        if (invite.FollowUp)
        {
            CompleteWithCode(invite);
            return;
        }
        sender = FriendsManager.ShortName(invite.SenderName);
        pendingCode = invite.SessionCode;
        pendingSenderId = invite.SenderId;
        waitingForCode = false;
        SetJoinBusy(false);
        senderText.text = sender;
        avatar.color = FriendAvatar.ColorFor(sender);
        avatarInitial.text = FriendAvatar.InitialFor(sender);
        ShowInviteMessage();
        card.Show();
    }

    // The invite arrived before the host's session existed: Join waits for the code's follow-up message.
    private void WaitForCode()
    {
        waitingForCode = true;
        SetJoinBusy(true);
        SetMessage("Waiting for the host...", messageColor);
    }

    // Leaves any session, waits for the code if needed, then joins (from a fresh Lobby when this scene has no NetworkBootstrap).
    private async void Join()
    {
        string code;
        (bool ok, string message) check;

        if (string.IsNullOrEmpty(pendingCode))
        {
            WaitForCode();
            return;
        }
        code = pendingCode;
        waitingForCode = false;
        SetJoinBusy(true);
        check = await SessionJoin.CheckAsync(code, bootstrap);
        if (this == null)
            return;
        if (!check.ok)
        {
            SetMessage(check.message, warningColor);
            SetJoinBusy(false);
            return;
        }
        card.Hide();
        await SessionJoin.JoinAsync(code, bootstrap, alreadyChecked: true);
    }

    private void Awake()
    {
        joinButton.onClick.AddListener(Join);
        dismissButton.onClick.AddListener(card.Hide);
    }

    private void OnEnable()
    {
        FriendsManager.OnInviteReceived += Show;
    }

    private void OnDisable()
    {
        FriendsManager.OnInviteReceived -= Show;
    }

    // Connects up front so invites arrive before the panel is ever opened.
    private async void Start()
    {
        try
        {
            await FriendsManager.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"InviteToastUI: friends service unavailable, invites disabled - {e.Message}");
        }
    }
}
