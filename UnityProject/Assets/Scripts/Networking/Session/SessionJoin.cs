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
    private const string HostedSessionKey = "session.hostedId";

    private static string offeredRejoinId;

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

    // Our own hosted game dies with us, so it is never offered back.
    public static void MarkHosted(string sessionId)
    {
        PlayerPrefs.SetString(HostedSessionKey, sessionId);
        PlayerPrefs.Save();
    }

    private static string FirstRejoinable(List<string> joined)
    {
        string hosted = PlayerPrefs.GetString(HostedSessionKey, string.Empty);

        foreach (string id in joined)
        {
            if (id != hosted && id != offeredRejoinId)
                return id;
        }
        return null;
    }

    // A session we still belong to after a drop or a crash, offered once per app run; null when there is none.
    public static async Task<string> FindRejoinableAsync()
    {
        string id;

        try
        {
            await ServicesAuth.EnsureSignedInAsync();
            id = FirstRejoinable(await MultiplayerService.Instance.GetJoinedSessionIdsAsync());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SessionJoin: couldn't look for a game to rejoin - {e.Message}");
            return null;
        }
        if (id != null)
            offeredRejoinId = id;
        return id;
    }

    // Tells a failed rejoin worth retrying from a game that is really gone.
    public static async Task<bool> IsStillMemberAsync(string sessionId)
    {
        try
        {
            return (await MultiplayerService.Instance.GetJoinedSessionIdsAsync()).Contains(sessionId);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SessionJoin: couldn't check session membership - {e.Message}");
            return false;
        }
    }

    public static async Task<bool> RejoinAsync(string sessionId, NetworkBootstrap preferred = null)
    {
        NetworkBootstrap target = ResolveBootstrap(preferred);

        if (target == null || IsOnline)
            return false;
        offeredRejoinId = null;
        return await target.RejoinSessionAsync(sessionId);
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
