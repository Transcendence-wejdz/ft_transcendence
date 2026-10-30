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
        return player != null && manager != null && manager.IsListening && manager.IsServer && player.IsSpawned && !player.IsOwner;
    }

    // Finds the spawned player behind a Friends/services id, so a friend entry can be kicked too.
    public static Player FindByServicesId(string servicesId)
    {
        if (string.IsNullOrEmpty(servicesId))
            return null;
        foreach (Player player in Player.AllActiveInstances)
            if (player != null && player.ServicesId == servicesId)
                return player;
        return null;
    }

    public static async Task<bool> KickAsync(Player player)
    {
        if (!CanKick(player))
            return false;

        string id = player.ServicesId;
        ulong clientId = player.OwnerClientId;
        ISession session = ActiveSession.Current;
        if (session != null && session.IsHost && !string.IsNullOrEmpty(id))
        {
            try
            {
                await session.AsHost().RemovePlayerAsync(id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SessionKick: couldn't remove {id} from the session - {e.Message}");
            }
        }

        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null && manager.IsServer && manager.ConnectedClients.ContainsKey(clientId))
            manager.DisconnectClient(clientId, Reason);
        return true;
    }
}
