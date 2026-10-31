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
    private enum Lookup { Found, NotFound, Full, Failed }

    // Public, indexed session property the host publishes its code into, so a code can be looked up before joining.
    public const string CodeProperty = "code";

    private const string LobbySceneName = "Lobby";

    private static bool IsOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    private static NetworkBootstrap ResolveBootstrap(NetworkBootstrap preferred)
    {
        if (preferred != null)
            return preferred;
        return UnityEngine.Object.FindAnyObjectByType<NetworkBootstrap>(FindObjectsInactive.Include);
    }

    // Leaving our session or reloading into the Lobby can't be undone, unlike a direct join from solo (which restores the player on failure).
    private static bool WouldLeaveSomething(NetworkBootstrap preferred)
    {
        return IsOnline || ResolveBootstrap(preferred) == null;
    }

    private static QuerySessionsOptions CodeQuery(string code)
    {
        return new QuerySessionsOptions
        {
            Count = 1,
            FilterOptions = new List<FilterOption> { new FilterOption(FilterField.StringIndex1, code, FilterOperation.Equal) },
        };
    }

    private static Lookup ReadLookup(QuerySessionsResults results)
    {
        if (results.Sessions == null || results.Sessions.Count == 0)
            return Lookup.NotFound;
        if (results.Sessions[0].AvailableSlots <= 0)
            return Lookup.Full;
        return Lookup.Found;
    }

    private static async Task<Lookup> LookUpAsync(string code)
    {
        QuerySessionsResults results;

        try
        {
            await ServicesAuth.EnsureSignedInAsync();
            results = await MultiplayerService.Instance.QuerySessionsAsync(CodeQuery(code));
            return ReadLookup(results);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SessionJoin: couldn't look up code {code} - {e.Message}");
            return Lookup.Failed;
        }
    }

    // Online, or with no bootstrap in this scene, the join goes through a Lobby reload that picks the code up.
    private static async Task JoinThroughLobbyAsync(string code)
    {
        if (IsOnline)
            await SessionDisconnectHandler.LeaveSessionsAsync();
        NetworkBootstrap.PendingJoinCode = code;
        LoadingScreenManager.Show("Joining session...");
        _ = SceneManager.LoadSceneAsync(LobbySceneName);
    }

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
        if (lookup == Lookup.NotFound)
            return (false, "No game found with that code.");
        return (false, "Couldn't check that code, try again.");
    }

    public static async Task<(bool ok, string message)> JoinAsync(string code, NetworkBootstrap preferred = null, bool alreadyChecked = false)
    {
        (bool ok, string message) check;
        NetworkBootstrap target;

        if (!alreadyChecked)
        {
            check = await CheckAsync(code, preferred);
            if (!check.ok)
                return check;
        }
        target = ResolveBootstrap(preferred);
        if (IsOnline || target == null)
        {
            await JoinThroughLobbyAsync(code);
            return (true, string.Empty);
        }
        if (await target.JoinWithCodeAsync(code, alreadyChecked: true))
            return (true, string.Empty);
        return (false, "Couldn't join that game.");
    }
}
