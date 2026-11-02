using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

// Host-only kick: removes the player from the session and drops their connection; their client then falls back to the offline Lobby.
public static class SessionKick
{
    public const string Reason = "Kicked by the host";

    public static bool CanKick(Player player)
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (player == null || manager == null)
            return false;
        if (!manager.IsListening || !manager.IsServer)
            return false;
        return player.IsSpawned && !player.IsOwner;
    }

    // Finds the spawned player behind a Friends/services id, so a friend entry can be kicked too.
    public static Player FindByServicesId(string servicesId)
    {
        if (string.IsNullOrEmpty(servicesId))
            return null;
        foreach (Player player in Player.AllActiveInstances)
        {
            if (player != null && player.ServicesId == servicesId)
                return player;
        }
        return null;
    }

    // Best effort: the connection is dropped below even if the session refuses the removal.
    private static async Task RemoveFromSessionAsync(string id)
    {
        ISession session = ActiveSession.Current;

        if (session == null || !session.IsHost || string.IsNullOrEmpty(id))
            return;
        try
        {
            await session.AsHost().RemovePlayerAsync(id);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SessionKick: couldn't remove {id} from the session - {e.Message}");
        }
    }

    public static async Task<bool> KickAsync(Player player)
    {
        ulong clientId;
        NetworkManager manager;

        if (!CanKick(player))
            return false;
        clientId = player.OwnerClientId;
        player.MarkLeftOnPurpose();
        await RemoveFromSessionAsync(player.ServicesId);
        manager = NetworkManager.Singleton;
        if (manager != null && manager.IsServer && manager.ConnectedClients.ContainsKey(clientId))
            manager.DisconnectClient(clientId, Reason);
        return true;
    }
}
