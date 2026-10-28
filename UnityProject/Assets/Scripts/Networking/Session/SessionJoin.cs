using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

// Single "join this code" path for invites and typed codes: checks the code, leaves any session, then joins.
public static class SessionJoin
{
    // Public, indexed session property the host publishes its code into, so a code can be looked up before joining.
    public const string CodeProperty = "code";

    private const string LobbySceneName = "Lobby";

    private enum Lookup { Found, NotFound, Full, Failed }

    // A miss isn't proof (the host may have failed to publish), so it only blocks when joining would leave our session or reload the scene.
    public static async Task<(bool ok, string message)> CheckAsync(string code, NetworkBootstrap preferred = null)
    {
        Lookup lookup = await LookUpAsync(code);
        if (lookup == Lookup.Found)
            return (true, string.Empty);
        if (lookup == Lookup.Full)
            return (false, "That game is full.");
        if (!WouldLeaveSomething(preferred))
            return (true, string.Empty);
        return lookup == Lookup.NotFound
            ? (false, "No game found with that code.")
            : (false, "Couldn't check that code, try again.");
    }

    public static async Task<(bool ok, string message)> JoinAsync(string code, NetworkBootstrap preferred = null, bool alreadyChecked = false)
    {
        if (!alreadyChecked)
        {
            var check = await CheckAsync(code, preferred);
            if (!check.ok)
                return check;
        }

        NetworkBootstrap target = ResolveBootstrap(preferred);
        bool online = IsOnline;
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

    private static bool IsOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    // Leaving our session or reloading into the Lobby can't be undone, unlike a direct join from solo (which restores the player on failure).
    private static bool WouldLeaveSomething(NetworkBootstrap preferred)
    {
        return IsOnline || ResolveBootstrap(preferred) == null;
    }

    private static NetworkBootstrap ResolveBootstrap(NetworkBootstrap preferred)
    {
        return preferred != null ? preferred : UnityEngine.Object.FindAnyObjectByType<NetworkBootstrap>(FindObjectsInactive.Include);
    }

    private static async Task<Lookup> LookUpAsync(string code)
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
                return Lookup.NotFound;
            return results.Sessions[0].AvailableSlots <= 0 ? Lookup.Full : Lookup.Found;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SessionJoin: couldn't look up code {code} - {e.Message}");
            return Lookup.Failed;
        }
    }
}
