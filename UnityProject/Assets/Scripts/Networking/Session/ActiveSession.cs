using System;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

// The session this client is really playing in; the SDK keeps dead sessions listed (still "Connected"), so its list can't be trusted to find it.
public static class ActiveSession
{
    private const string LobbySceneName = "Lobby";

    private static ISession current;

    public static bool InLobby => SceneManager.GetActiveScene().name == LobbySceneName;

    // Null when solo, even if a stale session object is still held.
    public static ISession Current
    {
        get
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return null;
            return current;
        }
    }

    public static void Set(ISession session)
    {
        current = session;
    }

    public static void Clear()
    {
        current = null;
    }

    // Host only: a locked session refuses invites and typed codes for as long as the run lasts.
    private static async void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ISession session = Current;
        bool locked = scene.name != LobbySceneName;
        IHostSession host;

        if (session == null || !session.IsHost || session.IsLocked == locked)
            return;
        try
        {
            host = session.AsHost();
            host.IsLocked = locked;
            await host.SavePropertiesAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ActiveSession: couldn't update the session lock - {e.Message}");
        }
    }

    [RuntimeInitializeOnLoadMethod]
    private static void ListenToSceneLoads()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }
}
