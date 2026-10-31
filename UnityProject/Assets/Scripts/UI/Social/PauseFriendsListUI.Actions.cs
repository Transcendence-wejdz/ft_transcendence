using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Friends.Models;
using UnityEngine;

// Friend and request actions: invite, kick, remove, accept, decline, cancel, with their outcome shown.
public partial class PauseFriendsListUI
{
    private void SetRowsBusy(bool busy)
    {
        foreach (PauseFriendRowUI row in rows)
            row.SetInteractable(!busy);
    }

    private void SetRequestRowsInteractable(bool interactable)
    {
        foreach (PauseRequestRowUI row in requestRows)
            row.SetInteractable(interactable);
    }

    // The service raises OnChanged after each action, which rebuilds the list; this only reports the outcome.
    private async void RunAction(Task action, string successMessage)
    {
        SetRequestRowsInteractable(false);
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
            SetRequestRowsInteractable(true);
    }

    private async void Invite(string memberId)
    {
        (bool ok, string message) result;

        if (FriendInviteFlow.IsBusy)
            return;
        result = await FriendInviteFlow.InviteAsync(memberId, message => SetStatus(message, isError: false));
        if (this != null)
            SetStatus(result.message, isError: !result.ok);
    }

    private async void KickFriend(Player player)
    {
        bool kicked = await SessionKick.KickAsync(player);

        if (this == null)
            return;
        if (kicked)
            SetStatus("Player kicked.", isError: false);
        else
            SetStatus("Only the host can kick.", isError: true);
    }

    private static Player FindInGame(string memberId, bool inGame)
    {
        if (!inGame)
            return null;
        return SessionKick.FindByServicesId(memberId);
    }

    // Invite when they can join us; Kick when that friend is in our game and we host; Remove friend always (Kick/Remove confirmed by a second click).
    private void OpenFriendMenu(RectTransform row, string memberId, bool inGame, Action invite)
    {
        List<SocialContextMenuUI.Item> items = new List<SocialContextMenuUI.Item>();
        Player player = FindInGame(memberId, inGame);

        if (invite != null && !FriendInviteFlow.IsBusy)
            items.Add(new SocialContextMenuUI.Item("Invite", invite));
        if (SessionKick.CanKick(player))
            items.Add(new SocialContextMenuUI.Item("Kick", () => KickFriend(player), destructive: true));
        items.Add(new SocialContextMenuUI.Item("Remove friend", () => RunAction(FriendsManager.RemoveAsync(memberId), "Friend removed."), destructive: true));
        contextMenu.Open(row, items);
    }
}
