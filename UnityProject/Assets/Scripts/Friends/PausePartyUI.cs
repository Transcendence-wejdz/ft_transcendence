using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Netcode;
using UnityEngine;

// Pause-menu party strip: the local player first, then the other session players in join order, empty slots after.
public class PausePartyUI : MonoBehaviour
{
    [SerializeField] private PartySlotUI[] slots;
    [SerializeField] private TMP_Text header;

    private readonly List<Player> members = new List<Player>();
    private int signature;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnDisable()
    {
        foreach (PartySlotUI slot in slots)
            slot.SetEmpty();
    }

    // Hosting, a join, a leave or the session dying under us all change this; the local player also swaps from the offline to the spawned instance.
    private void LateUpdate()
    {
        if (CurrentSignature() != signature)
            Rebuild();
    }

    private static int CurrentSignature()
    {
        Player local = LocalPlayer.GetPlayer();
        int hash = FriendsManager.SessionSignature();
        hash = hash * 31 + (local != null ? RuntimeHelpers.GetHashCode(local) : 0);
        foreach (Player player in Player.AllActiveInstances)
            if (player != null)
                hash = hash * 31 + RuntimeHelpers.GetHashCode(player);
        return hash;
    }

    // The offline solo player never registers, so the local player is resolved separately; others only count while online.
    private void Rebuild()
    {
        signature = CurrentSignature();
        members.Clear();
        Player local = LocalPlayer.GetPlayer();
        if (local != null)
            members.Add(local);

        bool online = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (online)
            foreach (Player player in Player.AllActiveInstances)
                if (player != null && player != local && members.Count < slots.Length)
                    members.Add(player);

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < members.Count)
                slots[i].Bind(members[i], members[i] == local);
            else
                slots[i].SetEmpty();
        }
        header.text = $"Party · {members.Count}/{slots.Length}";
    }
}
