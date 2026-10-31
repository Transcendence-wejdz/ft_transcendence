using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;

// The offline player around a Host/Join: captured and hidden before, its look handed to the networked player after, restored on failure.
public partial class NetworkBootstrap
{
    private static string NameOf(Player customization)
    {
        if (customization == null)
            return string.Empty;
        return customization.PlayerName;
    }

    // The saved profile is the source of truth (every class/color/name change and the web profile write it); the live player may not have loaded it yet.
    private static (int classIndex, int colorIndex, string playerName) ReadLook(Player customization)
    {
        int savedClass;
        int savedColor;
        string savedName;

        if (PlayerProfileStore.TryLoad(out savedClass, out savedColor, out savedName))
        {
            if (string.IsNullOrEmpty(savedName))
                savedName = NameOf(customization);
            return (savedClass, savedColor, savedName);
        }
        if (customization == null)
            return (0, 0, string.Empty);
        return (customization.ClassIndex, customization.ColorIndex, customization.PlayerName);
    }

    private void CapturePose(Player customization)
    {
        capturedFacing = Vector2.down;
        capturedAim = DefaultAimDegrees;
        if (customization == null)
            return;
        capturedFacing = customization.Facing;
        capturedAim = customization.AimAngleDegrees;
    }

    private void DestroyStandIn()
    {
        if (standIn != null)
        {
            standIn.SetActive(false);
            Destroy(standIn);
        }
        standIn = null;
    }

    // Runs in the networked player's OnNetworkSpawn (mid-CreateSessionAsync, before its first frame): it takes over the offline look and pose, then the stand-in goes.
    private void ApplyCapturedStateOnLocalSpawn(Player player)
    {
        if (!player.IsOwner)
            return;
        Player.OnPlayerRegistered -= ApplyCapturedStateOnLocalSpawn;
        player.SetSelection(capturedLook.classIndex, capturedLook.colorIndex);
        if (!string.IsNullOrEmpty(capturedLook.playerName))
            player.SetName(capturedLook.playerName);
        player.AdoptPose(capturedFacing, capturedAim);
        DestroyStandIn();
    }

    // Deactivates the offline player before Host/Join, keeping its camera and look; must run before the session exists, or Netcode spawns it as a ghost.
    private OfflineCapture CaptureOfflinePlayerState(bool leaveStandIn = false)
    {
        GameObject offlinePlayer = LocalPlayer.Get();
        OfflineCapture capture = new OfflineCapture { playerName = string.Empty, rotation = Quaternion.identity };
        Player customization;

        if (offlinePlayer == null)
            return capture;
        customization = offlinePlayer.GetComponent<Player>();
        capture.position = offlinePlayer.transform.position;
        capture.rotation = offlinePlayer.transform.rotation;
        capturedLook = ReadLook(customization);
        CapturePose(customization);
        Player.OnPlayerRegistered -= ApplyCapturedStateOnLocalSpawn;
        Player.OnPlayerRegistered += ApplyCapturedStateOnLocalSpawn;
        detachedCameraHolder = PlayerCameraRig.Detach(offlinePlayer.transform);
        if (leaveStandIn)
            standIn = OfflinePlayerStandIn.Create(offlinePlayer);
        capturedOfflinePlayer = offlinePlayer;
        offlinePlayer.SetActive(false);
        capture.classIndex = capturedLook.classIndex;
        capture.colorIndex = capturedLook.colorIndex;
        capture.playerName = capturedLook.playerName;
        return capture;
    }

    private static bool HasLocalPlayerObject()
    {
        NetworkClient localClient = NetworkManager.Singleton.LocalClient;

        return localClient != null && localClient.PlayerObject != null;
    }

    private static Player GetLocalNetworkedPlayer()
    {
        NetworkObject playerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;

        if (playerObject == null)
            return null;
        return playerObject.GetComponent<Player>();
    }

    private void DestroyDetachedCameras()
    {
        if (detachedCameraHolder == null)
            return;
        Destroy(detachedCameraHolder);
        detachedCameraHolder = null;
    }

    // Reapplies the offline player's customization once spawned and hides the loading screen; timed out so a dropped connection doesn't strand it.
    private IEnumerator RestoreLocalCustomizationWhenSpawned(OfflineCapture capture)
    {
        float deadline = Time.unscaledTime + SpawnTimeout;
        Player customization;

        while (!HasLocalPlayerObject() && Time.unscaledTime < deadline)
            yield return null;
        DestroyDetachedCameras();
        DestroyStandIn();
        capturedOfflinePlayer = null;
        customization = GetLocalNetworkedPlayer();
        if (customization != null)
        {
            customization.SetSelection(capture.classIndex, capture.colorIndex);
            if (!string.IsNullOrEmpty(capture.playerName))
                customization.SetName(capture.playerName);
        }
        LoadingScreenManager.Hide();
    }

    private void ReattachCameras()
    {
        List<Transform> cameras = new List<Transform>();

        if (detachedCameraHolder == null)
            return;
        foreach (Transform child in detachedCameraHolder.transform)
            cameras.Add(child);
        foreach (Transform camera in cameras)
            camera.SetParent(capturedOfflinePlayer.transform, worldPositionStays: true);
        Destroy(detachedCameraHolder);
        detachedCameraHolder = null;
    }

    // A failed Host/Join hands the scene back to the offline player: cameras re-attached, player re-enabled.
    private void RestoreOfflinePlayer()
    {
        Player.OnPlayerRegistered -= ApplyCapturedStateOnLocalSpawn;
        DestroyStandIn();
        if (capturedOfflinePlayer == null || NetworkManager.Singleton.IsListening)
            return;
        ReattachCameras();
        capturedOfflinePlayer.SetActive(true);
        capturedOfflinePlayer = null;
    }
}
