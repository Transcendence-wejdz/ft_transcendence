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

    // A follow-up only completes the open popup from the same friend with its code; it never reopens a dismissed or expired one.
    private void Show(FriendInvite invite)
    {
        if (invite.FollowUp)
        {
            if (card.IsShown && invite.SenderId == pendingSenderId && !string.IsNullOrEmpty(invite.SessionCode))
            {
                pendingCode = invite.SessionCode;
                if (waitingForCode)
                    Join();
            }
            return;
        }

        bool alreadyInSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        string sender = FriendsManager.ShortName(invite.SenderName);

        pendingCode = invite.SessionCode;
        pendingSenderId = invite.SenderId;
        waitingForCode = false;
        joinLabel.text = "Join";
        joinButton.interactable = true;
        senderText.text = sender;
        avatar.color = FriendAvatar.ColorFor(sender);
        avatarInitial.text = FriendAvatar.InitialFor(sender);
        messageText.text = alreadyInSession ? "Joining leaves your current game" : "invited you to play";
        messageText.color = alreadyInSession ? warningColor : messageColor;

        card.Show();
    }

    // Leaves any session, waits for the code if needed, then joins (from a fresh Lobby when this scene has no NetworkBootstrap).
    private async void Join()
    {
        if (string.IsNullOrEmpty(pendingCode))
        {
            waitingForCode = true;
            joinButton.interactable = false;
            joinLabel.text = "...";
            messageText.text = "Waiting for the host...";
            messageText.color = messageColor;
            return;
        }

        string code = pendingCode;
        waitingForCode = false;
        joinButton.interactable = false;
        joinLabel.text = "...";

        var check = await SessionJoin.CheckAsync(code, bootstrap);
        if (this == null)
            return;
        if (!check.ok)
        {
            messageText.text = check.message;
            messageText.color = warningColor;
            joinLabel.text = "Join";
            joinButton.interactable = true;
            return;
        }

        card.Hide();
        await SessionJoin.JoinAsync(code, bootstrap, alreadyChecked: true);
    }
}
