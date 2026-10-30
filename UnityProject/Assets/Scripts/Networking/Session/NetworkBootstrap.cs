using System.Reflection;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;

// Hosts and joins Netcode sessions over Unity Relay (driven by the social panel and invites).
public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject gameModeManagerPrefab;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private TMP_Text sessionCodeText;
    private Vector3? hostSpawnPosition;
    private Quaternion hostSpawnRotation;
    private GameObject detachedCameraHolder;
    private const int PublishAttempts = 3;
    private GameObject standIn;
    private GameObject capturedOfflinePlayer;
    private (int classIndex, int colorIndex, string playerName) capturedLook;
    private Vector2 capturedFacing;
    private float capturedAim;

    // Set by InviteToastUI before reloading Lobby, so the fresh scene picks up the join automatically.
    public static string PendingJoinCode;

    // Start runs after every Awake, so NetworkManager.Singleton is set.
    private void Start()
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        if (!string.IsNullOrEmpty(PendingJoinCode))
        {
            string code = PendingJoinCode;
            PendingJoinCode = null;
            StartCoroutine(JoinNextFrame(code));
        }
    }

    // Start order between objects is undefined: waiting a frame lets the offline player load its saved class/color first, so the join doesn't capture (and save) the defaults.
    private System.Collections.IEnumerator JoinNextFrame(string code)
    {
        yield return null;
        _ = JoinWithCodeAsync(code, alreadyChecked: true);
    }

    private void OnDestroy()
    {
        Player.OnPlayerRegistered -= ApplyCapturedStateOnLocalSpawn;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;

        if (request.ClientNetworkId == NetworkManager.ServerClientId && hostSpawnPosition.HasValue)
        {
            response.Position = hostSpawnPosition.Value;
            response.Rotation = hostSpawnRotation;
        }
        else if (PlayerSpawner.TryGetSpawnPosition(request.ClientNetworkId, out Vector3 position))
        {
            response.Position = position;
            response.Rotation = Quaternion.identity;
        }
    }

    // Shared by the social panel and invites; false on failure or if already hosting. Invites skip the loading screen and leave a frozen stand-in where the player was.
    public async System.Threading.Tasks.Task<bool> HostGameAsync(bool showLoadingScreen = true)
    {
        if (NetworkManager.Singleton.IsListening)
            return false;

        if (sessionCodeText != null)
            sessionCodeText.text = string.Empty;

        if (showLoadingScreen)
            LoadingScreenManager.Show("Creating session...");

        try
        {
            SyncTransportWebSockets();
            await ServicesAuth.EnsureSignedInAsync();

            (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) customization = CaptureOfflinePlayerState(leaveStandIn: !showLoadingScreen);
            hostSpawnPosition = customization.position;
            hostSpawnRotation = customization.rotation;

            SessionOptions options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork(RelayRegions.Selected);
            IHostSession session = await MultiplayerService.Instance.CreateSessionAsync(options);
            await PublishCodeAsync(session);
            ActiveSession.Set(session);

            if (gameModeManagerPrefab != null)
            {
                GameObject gameModeManager = Instantiate(gameModeManagerPrefab);
                gameModeManager.GetComponent<NetworkObject>().Spawn();
            }

            if (sessionCodeText != null)
                sessionCodeText.text = $"Code {session.Code}";

            StartCoroutine(RestoreLocalCustomizationWhenSpawned(customization.classIndex, customization.colorIndex, customization.playerName));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NetworkBootstrap: failed to host - {e.Message}");
            RestoreOfflinePlayer();
            LoadingScreenManager.Hide();
            return false;
        }
    }

    // Shared by typed codes and invites; pre-checks the code before the loading screen and player swap unless already checked.
    public async System.Threading.Tasks.Task<bool> JoinWithCodeAsync(string code, bool alreadyChecked = false)
    {
        if (string.IsNullOrEmpty(code))
            return false;

        if (NetworkManager.Singleton.IsListening)
            return false;

        if (!alreadyChecked)
        {
            var check = await SessionJoin.CheckAsync(code, this);
            if (!check.ok)
            {
                Debug.LogWarning($"NetworkBootstrap: can't join {code} - {check.message}");
                return false;
            }
        }

        LoadingScreenManager.Show("Joining session...");

        try
        {
            SyncTransportWebSockets();
            await ServicesAuth.EnsureSignedInAsync();

            (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) customization = CaptureOfflinePlayerState();

            ISession joined = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
            ActiveSession.Set(joined);

            StartCoroutine(RestoreLocalCustomizationWhenSpawned(customization.classIndex, customization.colorIndex, customization.playerName));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NetworkBootstrap: failed to join - {e.Message}");
            RestoreOfflinePlayer();
            LoadingScreenManager.Hide();
            return false;
        }
    }

    // Matches UnityTransport's WebSocket flag to the protocol Relay picks (WSS on WebGL), or StartHost/StartClient throws.
    private static void SyncTransportWebSockets()
    {
        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is not UnityTransport transport)
            return;

        transport.UseWebSockets = RelayProtocol.Default == RelayProtocol.WSS;
    }


    // Deactivates the offline player before Host/Join, keeping its camera and look; must run before the session exists, or Netcode spawns it as a ghost.
    private (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) CaptureOfflinePlayerState(bool leaveStandIn = false)
    {
        GameObject offlinePlayer = LocalPlayer.Get();
        if (offlinePlayer == null)
            return (0, 0, string.Empty, Vector3.zero, Quaternion.identity);

        // The saved profile is the source of truth (every class/color/name change and the web profile write it); the live player may not have loaded it yet.
        Player customization = offlinePlayer.GetComponent<Player>();
        (int classIndex, int colorIndex, string playerName) current;
        if (PlayerProfileStore.TryLoad(out int savedClass, out int savedColor, out string savedName))
            current = (savedClass, savedColor, !string.IsNullOrEmpty(savedName) ? savedName : (customization != null ? customization.PlayerName : string.Empty));
        else
            current = customization != null ? (customization.ClassIndex, customization.ColorIndex, customization.PlayerName) : (0, 0, string.Empty);
        Vector3 position = offlinePlayer.transform.position;
        Quaternion rotation = offlinePlayer.transform.rotation;

        capturedLook = current;
        capturedFacing = customization != null ? customization.Facing : Vector2.down;
        capturedAim = customization != null ? customization.AimAngleDegrees : -90f;
        Player.OnPlayerRegistered -= ApplyCapturedStateOnLocalSpawn;
        Player.OnPlayerRegistered += ApplyCapturedStateOnLocalSpawn;

        detachedCameraHolder = PlayerCameraRig.Detach(offlinePlayer.transform);
        if (leaveStandIn)
            standIn = OfflinePlayerStandIn.Create(offlinePlayer);

        capturedOfflinePlayer = offlinePlayer;
        offlinePlayer.SetActive(false);
        return (current.classIndex, current.colorIndex, current.playerName, position, rotation);
    }

    // Reapplies the offline player's customization once spawned and hides the loading screen; timed out so a dropped connection doesn't strand it.
    private System.Collections.IEnumerator RestoreLocalCustomizationWhenSpawned(int classIndex, int colorIndex, string playerName)
    {
        float deadline = Time.unscaledTime + 15f;
        while ((NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
            && Time.unscaledTime < deadline)
            yield return null;

        if (detachedCameraHolder != null)
        {
            Destroy(detachedCameraHolder);
            detachedCameraHolder = null;
        }
        DestroyStandIn();
        capturedOfflinePlayer = null;

        NetworkObject playerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;
        Player customization = playerObject != null ? playerObject.GetComponent<Player>() : null;
        if (customization != null)
        {
            customization.SetSelection(classIndex, colorIndex);
            if (!string.IsNullOrEmpty(playerName))
                customization.SetName(playerName);
        }

        LoadingScreenManager.Hide();
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

    private void DestroyStandIn()
    {
        if (standIn != null)
        {
            standIn.SetActive(false);
            Destroy(standIn);
        }
        standIn = null;
    }

    // Retried and run before ActiveSession.Set so the shown/invited code is findable by SessionJoin.CheckAsync; a final failure keeps hosting.
    private static async System.Threading.Tasks.Task PublishCodeAsync(IHostSession session)
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
                    await System.Threading.Tasks.Task.Delay(attempt * 1000);
            }
        }
    }

    // A failed Host/Join hands the scene back to the offline player: cameras re-attached, player re-enabled.
    private void RestoreOfflinePlayer()
    {
        Player.OnPlayerRegistered -= ApplyCapturedStateOnLocalSpawn;
        DestroyStandIn();
        if (capturedOfflinePlayer == null || NetworkManager.Singleton.IsListening)
            return;

        if (detachedCameraHolder != null)
        {
            var cameras = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in detachedCameraHolder.transform)
                cameras.Add(child);
            foreach (Transform camera in cameras)
                camera.SetParent(capturedOfflinePlayer.transform, worldPositionStays: true);
            Destroy(detachedCameraHolder);
            detachedCameraHolder = null;
        }

        capturedOfflinePlayer.SetActive(true);
        capturedOfflinePlayer = null;
    }
}
