using System;
using TMPro;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.UI;

// Popup for a live incoming friend request: Accept adds the friend, the close button only dismisses (the request stays in the social panel).
public class FriendRequestToastUI : MonoBehaviour
{
    [SerializeField] private ToastCard card;
    [SerializeField] private TMP_Text senderText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image avatar;
    [SerializeField] private TMP_Text avatarInitial;
    [SerializeField] private Button acceptButton;
    [SerializeField] private TMP_Text acceptLabel;
    [SerializeField] private Button dismissButton;
    [SerializeField] private Color messageColor = new Color(0.7f, 0.69f, 0.66f, 1f);
    [SerializeField] private Color warningColor = new Color(0.95f, 0.75f, 0.3f, 1f);

    private string pendingMemberId;

    private void Awake()
    {
        acceptButton.onClick.AddListener(Accept);
        dismissButton.onClick.AddListener(card.Hide);
    }

    private void OnEnable()
    {
        FriendsManager.OnFriendRequestReceived += Show;
        FriendsManager.OnChanged += HideIfHandled;
    }

    private void OnDisable()
    {
        FriendsManager.OnFriendRequestReceived -= Show;
        FriendsManager.OnChanged -= HideIfHandled;
    }

    // Accepted or declined elsewhere (pause menu Requests section), or cancelled by the sender: the popup has nothing left to ask.
    private void HideIfHandled()
    {
        if (!card.IsShown || string.IsNullOrEmpty(pendingMemberId))
            return;

        foreach (Relationship request in FriendsManager.Incoming)
            if (request.Member.Id == pendingMemberId)
                return;
        card.Hide();
    }

    private void Show(string memberId, string senderName)
    {
        string sender = string.IsNullOrEmpty(senderName) ? "Someone" : senderName;
        pendingMemberId = memberId;
        senderText.text = sender;
        avatar.color = FriendAvatar.ColorFor(sender);
        avatarInitial.text = FriendAvatar.InitialFor(sender);
        messageText.text = "wants to be friends";
        messageText.color = messageColor;
        acceptLabel.text = "Accept";
        acceptButton.interactable = true;
        card.Show();
    }

    private async void Accept()
    {
        if (string.IsNullOrEmpty(pendingMemberId))
            return;

        acceptButton.interactable = false;
        acceptLabel.text = "...";
        try
        {
            await FriendsManager.AcceptAsync(pendingMemberId);
            if (this != null)
                card.Hide();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"FriendRequestToastUI: accept failed - {e.Message}");
            if (this == null)
                return;
            messageText.text = "Couldn't accept, try again.";
            messageText.color = warningColor;
            acceptLabel.text = "Accept";
            acceptButton.interactable = true;
        }
    }
}
