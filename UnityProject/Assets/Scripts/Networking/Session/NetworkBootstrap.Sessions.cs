using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;

// Session creation and joining over Relay, with the code published so invites can find it.
public partial class NetworkBootstrap
{
    // Matches UnityTransport's WebSocket flag to the protocol Relay picks (WSS on WebGL), or StartHost/StartClient throws.
    private static void SyncTransportWebSockets()
    {
        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is not UnityTransport transport)
            return;
        transport.UseWebSockets = RelayProtocol.Default == RelayProtocol.WSS;
    }

    // Retried and run before ActiveSession.Set so the shown/invited code is findable by SessionJoin.CheckAsync; a final failure keeps hosting.
    private static async Task PublishCodeAsync(IHostSession session)
    {
        session.SetProperty(SessionJoin.CodeProperty, new SessionProperty(session.Code, VisibilityPropertyOptions.Public, PropertyIndex.String1));
        for (int attempt = 1; attempt <= PublishAttempts; attempt++)
        {
            try
            {
                await session.SavePropertiesAsync();
                return;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"NetworkBootstrap: couldn't publish the session code (attempt {attempt}/{PublishAttempts}) - {e.Message}");
                if (attempt < PublishAttempts)
                    await Task.Delay(attempt * PublishRetryDelayMs);
            }
        }
    }

    private void FailSession(string action, System.Exception e)
    {
        Debug.LogError($"NetworkBootstrap: failed to {action} - {e.Message}");
        RestoreOfflinePlayer();
        LoadingScreenManager.Hide();
    }

    private void SpawnGameModeManager()
    {
        GameObject gameModeManager;

        if (gameModeManagerPrefab == null)
            return;
        gameModeManager = Instantiate(gameModeManagerPrefab);
        gameModeManager.GetComponent<NetworkObject>().Spawn();
    }

    private void SetSessionCodeText(string text)
    {
        if (sessionCodeText != null)
            sessionCodeText.text = text;
    }

    private async Task CreateHostSessionAsync(bool leaveStandIn)
    {
        OfflineCapture capture;
        SessionOptions options;
        IHostSession session;

        SyncTransportWebSockets();
        await ServicesAuth.EnsureSignedInAsync();
        capture = CaptureOfflinePlayerState(leaveStandIn);
        hostSpawnPosition = capture.position;
        hostSpawnRotation = capture.rotation;
        options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork(RelayRegions.Selected);
        session = await MultiplayerService.Instance.CreateSessionAsync(options);
        await PublishCodeAsync(session);
        ActiveSession.Set(session);
        SpawnGameModeManager();
        SetSessionCodeText($"Code {session.Code}");
        StartCoroutine(RestoreLocalCustomizationWhenSpawned(capture));
    }

    private async Task JoinSessionAsync(string code)
    {
        OfflineCapture capture;
        ISession joined;

        SyncTransportWebSockets();
        await ServicesAuth.EnsureSignedInAsync();
        capture = CaptureOfflinePlayerState();
        joined = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
        ActiveSession.Set(joined);
        StartCoroutine(RestoreLocalCustomizationWhenSpawned(capture));
    }

    private static async Task<bool> PassesCheckAsync(string code, NetworkBootstrap bootstrap)
    {
        (bool ok, string message) check = await SessionJoin.CheckAsync(code, bootstrap);

        if (!check.ok)
            Debug.LogWarning($"NetworkBootstrap: can't join {code} - {check.message}");
        return check.ok;
    }
}
