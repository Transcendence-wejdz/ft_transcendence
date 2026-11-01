using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Player identity shared with others: deduplicated display names, Friends tag and services id.
public partial class Player
{
    // Players sharing a base name, grouped so duplicates can be numbered.
    private static Dictionary<string, List<Player>> GroupByBaseName()
    {
        Dictionary<string, List<Player>> groups = new Dictionary<string, List<Player>>();
        List<Player> group;
        string baseName;

        foreach (Player instance in ActiveInstances)
        {
            baseName = instance.PlayerName;
            if (!groups.TryGetValue(baseName, out group))
            {
                group = new List<Player>();
                groups[baseName] = group;
            }
            group.Add(instance);
        }
        return groups;
    }

    // Duplicates get " (1)", " (2)"... in spawn order; a unique name stays as is.
    private static void NumberGroup(List<Player> group)
    {
        string baseName;

        group.Sort((a, b) => a.NetworkObjectId.CompareTo(b.NetworkObjectId));
        for (int i = 0; i < group.Count; i++)
        {
            baseName = group[i].PlayerName;
            if (group.Count > 1)
                group[i].ApplyName($"{baseName} ({i + 1})");
            else
                group[i].ApplyName(baseName);
        }
    }

    private static void RefreshAllDisplayNames()
    {
        foreach (List<Player> group in GroupByBaseName().Values)
            NumberGroup(group);
    }

    private static Player FindServicesIdHolder(Player claimer, string claimed)
    {
        foreach (Player other in ActiveInstances)
        {
            if (other != claimer && other.ServicesId == claimed)
                return other;
        }
        return null;
    }

    private void ApplyName(string value)
    {
        CurrentDisplayName = value;
        if (nameTag != null)
            nameTag.SetName(value);
        OnDisplayNameChanged?.Invoke(value);
    }

    // Only once spawned (never from Awake), and again when the friends name arrives or changes.
    private void PublishFriendsTag()
    {
        if (this == null || !IsOwner || !IsSpawned || !FriendsManager.IsReady)
            return;
        if (FriendsManager.OwnName != FriendsTag)
            friendsTag.Value = FriendsManager.OwnName;
    }

    // Comes from the sign-in rather than the friends service, which may be down: a session can't be joined without signing in.
    private void PublishServicesId()
    {
        string id = ServicesAuth.SignedInPlayerId;

        if (IsOwner && IsSpawned && !string.IsNullOrEmpty(id))
            ClaimServicesIdServerRpc(id);
    }

    // Write-once; another player holding that id is our own body from before a drop, unless it's the host's.
    [ServerRpc]
    private void ClaimServicesIdServerRpc(FixedString64Bytes id)
    {
        string claimed = id.ToString();
        Player holder;

        if (!string.IsNullOrEmpty(ServicesId) || string.IsNullOrEmpty(claimed))
            return;
        holder = FindServicesIdHolder(this, claimed);
        if (holder != null && holder.IsOwner)
        {
            Debug.LogWarning($"Player: client {OwnerClientId} tried to claim the host's services id {claimed}");
            return;
        }
        if (holder != null)
            holder.ReleaseStaleBody();
        servicesId.Value = id;
        RestoreRejoinSnapshot(claimed);
        OnServicesIdClaimed?.Invoke(claimed);
    }
}
