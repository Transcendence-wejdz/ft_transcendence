using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shared "join this code" entry point for invites and typed codes: checks the code exists first, leaves any current session, and joins from a fresh Lobby when this scene has no NetworkBootstrap.
public static class SessionJoin
{
    // Public, indexed session property the host publishes its code into, so a code can be looked up before joining.
    public const string CodeProperty = "code";

    private const string LobbySceneName = "Lobby";

    // Looked up before anything irreversible (leaving our session, reloading, swapping the player out).
    public static async Task<(bool ok, string message)> CheckAsync(string code)
    {
        try
        {
            await ServicesAuth.EnsureSignedInAsync();
            var options = new QuerySessionsOptions
            {
                Count = 1,
                FilterOptions = new List<FilterOption> { new FilterOption(FilterField.StringIndex1, code, FilterOperation.Equal) },
            };
            QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(options);
            if (results.Sessions == null || results.Sessions.Count == 0)
                return (false, "No game found with that code.");
            if (results.Sessions[0].AvailableSlots <= 0)
                return (false, "That game is full.");
            return (true, string.Empty);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SessionJoin: couldn't look up code {code} - {e.Message}");
            return (false, "Couldn't check that code, try again.");
        }
    }

    public static async Task<(bool ok, string message)> JoinAsync(string code, NetworkBootstrap preferred = null, bool alreadyChecked = false)
    {
        if (!alreadyChecked)
        {
            var check = await CheckAsync(code);
            if (!check.ok)
                return check;
        }

        NetworkBootstrap target = preferred != null ? preferred : UnityEngine.Object.FindAnyObjectByType<NetworkBootstrap>(FindObjectsInactive.Include);
        bool online = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (online || target == null)
        {
            if (online)
                await SessionDisconnectHandler.LeaveSessionsAsync();
            NetworkBootstrap.PendingJoinCode = code;

            LoadingScreenManager.Show("Joining session...");
            _ = SceneManager.LoadSceneAsync(LobbySceneName);
            return (true, string.Empty);
        }

        bool joined = await target.JoinWithCodeAsync(code, alreadyChecked: true);
        return joined ? (true, string.Empty) : (false, "Couldn't join that game.");
    }
}
