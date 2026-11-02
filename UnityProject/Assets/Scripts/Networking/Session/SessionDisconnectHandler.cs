using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

// Persistent watcher: when a session ends under us (host left or dropped), falls back to an offline Lobby.
public class SessionDisconnectHandler : MonoBehaviour
{
    private const string LobbySceneName = "Lobby";
    private const float ExpectedStopWindow = 5f;

    private static float expectedStopUntil;
    private static bool quitting;

    private NetworkManager subscribedManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameObject go = new GameObject("SessionDisconnectHandler");

        DontDestroyOnLoad(go);
        go.AddComponent<SessionDisconnectHandler>();
    }

    private static List<ISession> ListedSessions()
    {
        return new List<ISession>(MultiplayerService.Instance.Sessions.Values);
    }

    private static async Task LeaveOneAsync(ISession session)
    {
        try
        {
            if (session.IsHost)
                await session.AsHost().DeleteAsync();
            else
                await session.LeaveAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"SessionDisconnectHandler: couldn't leave session cleanly - {e.Message}");
        }
    }

    // Best-effort cleanup of ended sessions the SDK still lists, keeping any we still belong to so it can be rejoined; never touches the NetworkManager, so a new session started meanwhile is safe.
    private static async Task ForgetSessionsAsync(List<ISession> sessions)
    {
        List<string> joined;

        try
        {
            joined = await MultiplayerService.Instance.GetJoinedSessionIdsAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"SessionDisconnectHandler: couldn't list joined sessions - {e.Message}");
            return;
        }
        foreach (ISession session in sessions)
        {
            if (joined.Contains(session.Id) && !session.IsHost)
                continue;
            try
            {
                await session.LeaveAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SessionDisconnectHandler: stale session cleanup failed - {e.Message}");
            }
        }
    }

    // Deferred a frame so the scene load doesn't run inside Netcode's own shutdown callback; never waits on the network.
    private IEnumerator ReturnToOfflineLobbyNextFrame()
    {
        List<ISession> stale;

        yield return null;
        Debug.Log("SessionDisconnectHandler: session ended under us, returning to the offline Lobby.");
        ActiveSession.Clear();
        stale = ListedSessions();
        LoadingScreenManager.LoadScene(LobbySceneName);
        _ = ForgetSessionsAsync(stale);
    }

    private void HandleClientStopped(bool wasHost)
    {
        if (quitting || Time.unscaledTime < expectedStopUntil)
            return;
        StartCoroutine(ReturnToOfflineLobbyNextFrame());
    }

    // Hosts delete the session; a failed call never blocks leaving. Stops are flagged so they aren't treated as drops.
    public static async Task LeaveSessionsAsync()
    {
        expectedStopUntil = Time.unscaledTime + ExpectedStopWindow;
        LocalPlayer.GetPlayer()?.AnnounceLeaving();
        ActiveSession.Clear();
        foreach (ISession session in ListedSessions())
            await LeaveOneAsync(session);
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    // After a failed rejoin: drops the half-open connection but keeps our place in the session.
    public static void StopWithoutLeaving()
    {
        expectedStopUntil = Time.unscaledTime + ExpectedStopWindow;
        ActiveSession.Clear();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    // Follows NetworkManager.Singleton, which can be replaced across scene loads.
    void Update()
    {
        if (subscribedManager == NetworkManager.Singleton)
            return;
        if (subscribedManager != null)
            subscribedManager.OnClientStopped -= HandleClientStopped;
        subscribedManager = NetworkManager.Singleton;
        if (subscribedManager != null)
            subscribedManager.OnClientStopped += HandleClientStopped;
    }

    void OnApplicationQuit()
    {
        quitting = true;
    }

    void OnDestroy()
    {
        if (subscribedManager != null)
            subscribedManager.OnClientStopped -= HandleClientStopped;
    }
}
