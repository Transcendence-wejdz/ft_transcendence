using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Services.Friends.Models;
using TMPro;
using Unity.Netcode;
using UnityEngine;

// Pause-menu party strip: the local player first, then the other session players in join order, empty slots after.
public class PausePartyUI : MonoBehaviour
{
    [SerializeField] private PartySlotUI[] slots;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private SocialContextMenuUI contextMenu;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Color errorColor = new Color(0.9f, 0.4f, 0.35f, 1f);
    [SerializeField] private Color infoColor = new Color(0.7f, 0.69f, 0.66f, 1f);

    private readonly List<Player> members = new List<Player>();
    private int signature;
    private Player lastLocal;

    private static bool IsOnline()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private static bool ContainsMember(IReadOnlyList<Relationship> relationships, string servicesId)
    {
        foreach (Relationship relationship in relationships)
        {
            if (relationship.Member.Id == servicesId)
                return true;
        }
        return false;
    }

    private static bool IsFriendOrAsked(string servicesId)
    {
        return ContainsMember(FriendsManager.Friends, servicesId) || ContainsMember(FriendsManager.Outgoing, servicesId);
    }

    private static int IdentityHash(Player player)
    {
        if (player == null)
            return 0;
        return RuntimeHelpers.GetHashCode(player);
    }

    // While hosting/joining swaps players, neither is findable for a moment: keep showing the last local one (only deactivated, not destroyed).
    private Player ResolveLocal()
    {
        Player local = LocalPlayer.GetPlayer();

        if (local == null)
            return lastLocal;
        lastLocal = local;
        return local;
    }

    // Hosting, a join, a leave or the session dying under us all change this; the local player also swaps from the offline to the spawned instance.
    private int CurrentSignature()
    {
        int hash = FriendsManager.SessionSignature() * 31 + IdentityHash(ResolveLocal());

        foreach (Player player in Player.AllActiveInstances)
        {
            if (player != null)
                hash = hash * 31 + RuntimeHelpers.GetHashCode(player);
        }
        return hash;
    }

    // The offline solo player never registers, so the local player is resolved separately; others only count while online.
    private void CollectMembers(Player local)
    {
        members.Clear();
        if (local != null)
            members.Add(local);
        if (!IsOnline())
            return;
        foreach (Player player in Player.AllActiveInstances)
        {
            if (player != null && player != local && members.Count < slots.Length)
                members.Add(player);
        }
    }

    private void Rebuild()
    {
        Player local;

        signature = CurrentSignature();
        local = ResolveLocal();
        CollectMembers(local);
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < members.Count)
                slots[i].Bind(members[i], members[i] == local);
            else
                slots[i].SetEmpty();
        }
        countText.text = $"{members.Count}/{slots.Length}";
    }

    private void SetStatus(string message, bool isError)
    {
        statusText.text = message;
        if (isError)
            statusText.color = errorColor;
        else
            statusText.color = infoColor;
    }

    private async void Kick(Player player)
    {
        bool kicked = await SessionKick.KickAsync(player);

        if (this == null)
            return;
        if (kicked)
            SetStatus("Player kicked.", isError: false);
        else
            SetStatus("Only the host can kick.", isError: true);
    }

    private async void AddFriend(string tag)
    {
        try
        {
            await FriendsManager.AddByNameAsync(tag);
            if (this != null)
                SetStatus("Request sent.", isError: false);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PausePartyUI: add friend failed - {e.Message}");
            if (this != null)
                SetStatus("Couldn't send the request.", isError: true);
        }
    }

    // Kick for the host, Add friend when that teammate isn't a friend (or already asked) yet.
    private void OpenMenu(PartySlotUI slot, Player player)
    {
        List<SocialContextMenuUI.Item> items = new List<SocialContextMenuUI.Item>();
        string tag = player.FriendsTag;

        if (SessionKick.CanKick(player))
            items.Add(new SocialContextMenuUI.Item("Kick", () => Kick(player), destructive: true));
        if (!string.IsNullOrEmpty(tag) && !IsFriendOrAsked(player.ServicesId))
            items.Add(new SocialContextMenuUI.Item("Add friend", () => AddFriend(tag)));
        contextMenu.Open((RectTransform)slot.transform, items);
    }

    private void OnEnable()
    {
        foreach (PartySlotUI slot in slots)
            slot.Clicked += OpenMenu;
        Rebuild();
    }

    private void OnDisable()
    {
        foreach (PartySlotUI slot in slots)
        {
            slot.Clicked -= OpenMenu;
            slot.SetEmpty();
        }
    }

    private void LateUpdate()
    {
        if (CurrentSignature() != signature)
            Rebuild();
    }
}
