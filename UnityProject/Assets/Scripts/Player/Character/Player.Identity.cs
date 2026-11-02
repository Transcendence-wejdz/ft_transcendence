using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Player identity shared with others: deduplicated display names, Friends tag and services id.
public partial class Player
{
    private const float ServicesIdRetrySeconds = 1f;

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

    private static bool IsServicesIdHeld(Player claimer, string claimed)
    {
        foreach (Player other in ActiveInstances)
        {
            if (other != claimer && other.ServicesId == claimed)
                return true;
        }
        return false;
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

    // Comes from the sign-in rather than the friends service, which may be down; retried since our old body can still hold the id right after a drop.
    private IEnumerator PublishServicesId()
    {
        WaitForSecondsRealtime retryDelay = new WaitForSecondsRealtime(ServicesIdRetrySeconds);
        string id = ServicesAuth.SignedInPlayerId;

        while (IsOwner && IsSpawned && !string.IsNullOrEmpty(id) && string.IsNullOrEmpty(ServicesId))
        {
            ClaimServicesIdServerRpc(id);
            yield return retryDelay;
        }
    }

    // Write-once and refused while another player holds that id, so a client can never push a connected player out.
    [ServerRpc]
    private void ClaimServicesIdServerRpc(FixedString64Bytes id)
    {
        string claimed = id.ToString();

        if (!string.IsNullOrEmpty(ServicesId) || string.IsNullOrEmpty(claimed))
            return;
        if (IsServicesIdHeld(this, claimed))
            return;
        servicesId.Value = id;
        RestoreRejoinSnapshot(claimed);
        OnServicesIdClaimed?.Invoke(claimed);
    }
}
