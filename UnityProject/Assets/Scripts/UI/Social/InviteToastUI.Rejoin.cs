using System.Threading.Tasks;
using UnityEngine;

// Offer to get back into a game we dropped from, on the same card as an invite.
public partial class InviteToastUI
{
    private void ShowRejoinOffer(string sessionId)
    {
        pendingRejoinId = sessionId;
        pendingCode = null;
        pendingSenderId = null;
        waitingForCode = false;
        SetJoinBusy(false);
        senderText.text = "Your game";
        avatar.color = FriendAvatar.ColorFor(senderText.text);
        avatarInitial.text = FriendAvatar.InitialFor(senderText.text);
        sitePhoto.Load(null);
        SetMessage("is still running", messageColor);
        card.Show();
    }

    // Only says the game is over once the session no longer lists us; anything else can be retried.
    private async Task ShowRejoinFailureAsync(string sessionId)
    {
        bool stillMember = await SessionJoin.IsStillMemberAsync(sessionId);

        if (this == null)
            return;
        SetJoinBusy(false);
        if (stillMember)
        {
            pendingRejoinId = sessionId;
            SetMessage("Couldn't rejoin, try again", warningColor);
        }
        else
        {
            joinButton.interactable = false;
            SetMessage("That game is over", warningColor);
        }
        card.Show();
    }

    private async void Rejoin()
    {
        string sessionId = pendingRejoinId;

        pendingRejoinId = null;
        SetJoinBusy(true);
        card.Hide();
        if (await SessionJoin.RejoinAsync(sessionId, bootstrap) || this == null)
            return;
        await ShowRejoinFailureAsync(sessionId);
    }

    // Only from a Lobby with nothing else going on, and never over an invite already showing.
    private async Task OfferRejoinAsync()
    {
        string sessionId;

        if (!ActiveSession.InLobby || IsInSession() || !string.IsNullOrEmpty(NetworkBootstrap.PendingJoinCode))
            return;
        sessionId = await SessionJoin.FindRejoinableAsync();
        if (this != null && sessionId != null && !IsInSession() && !card.IsShown)
            ShowRejoinOffer(sessionId);
    }
}
