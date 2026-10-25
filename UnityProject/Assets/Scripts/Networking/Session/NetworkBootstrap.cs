using System.Reflection;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

// Wires the Lobby's Host/Join buttons to Netcode over Unity Relay.
public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] private MenuPanel multiplayerPanel;
    [SerializeField] private GameObject gameModeManagerPrefab;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text joinStatusText;
    [SerializeField] private TMP_Text sessionCodeText;
    private Vector3? hostSpawnPosition;
    private Quaternion hostSpawnRotation;
    private GameObject detachedCameraHolder;
    private const int PublishAttempts = 3;
    private GameObject standIn;
    private GameObject capturedOfflinePlayer;

    // Set by InviteToastUI before reloading Lobby, so the fresh scene picks up the join automatically.
    public static string PendingJoinCode;

    // Start runs after every Awake, so NetworkManager.Singleton is set.
    private void Start()
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        if (ipInputField != null)
            ipInputField.onSubmit.AddListener(SubmitJoinCode);

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
        Player.OnPlayerRegistered -= HideStandInOnLocalSpawn;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
        if (ipInputField != null)
            ipInputField.onSubmit.RemoveListener(SubmitJoinCode);
    }

    // Enter in the join-code field acts like the Join button, unless a join is already running (button disabled).
    private void SubmitJoinCode(string _)
    {
        if (joinButton != null && !joinButton.interactable)
            return;

        JoinGame();
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

    public void HostGame()
    {
        _ = HostGameAsync();
    }

    // Shared by the Host button and invites; false on failure or if already hosting. Invites skip the loading screen and leave a frozen stand-in where the player was.
    public async System.Threading.Tasks.Task<bool> HostGameAsync(bool showLoadingScreen = true)
    {
        if (NetworkManager.Singleton.IsListening)
            return false;

        string originalPlaceholder = null;
        TMP_Text placeholderText = ipInputField != null ? ipInputField.placeholder as TMP_Text : null;
        if (placeholderText != null)
        {
            originalPlaceholder = placeholderText.text;
            placeholderText.text = "Generating code...";
        }
        if (sessionCodeText != null)
            sessionCodeText.text = string.Empty;

        if (hostButton != null)
            hostButton.interactable = false;
        SetJoinControlsInteractable(false);
        if (showLoadingScreen)
            LoadingScreenManager.Show("Creating session...");

        try
        {
            SyncTransportWebSockets();
            await ServicesAuth.EnsureSignedInAsync();

            // Must stay before any network call: the host's approval is processed late, after Netcode auto-spawns active scene-placed players (ghost).
            (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) customization = CaptureOfflinePlayerState(leaveStandIn: !showLoadingScreen);
            hostSpawnPosition = customization.position;
            hostSpawnRotation = customization.rotation;

            SessionOptions options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork();
            IHostSession session = await MultiplayerService.Instance.CreateSessionAsync(options);
            await PublishCodeAsync(session);
            ActiveSession.Set(session);

            if (gameModeManagerPrefab != null)
            {
                GameObject gameModeManager = Instantiate(gameModeManagerPrefab);
                gameModeManager.GetComponent<NetworkObject>().Spawn();
            }

            if (ipInputField != null)
            {
                ipInputField.text = session.Code;
                ipInputField.interactable = false;
            }
            if (sessionCodeText != null)
                sessionCodeText.text = $"Code: <font=\"LiberationSans SDF\">{session.Code}</font>";

            StartCoroutine(RestoreLocalCustomizationWhenSpawned(customization.classIndex, customization.colorIndex, customization.playerName));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NetworkBootstrap: failed to host - {e.Message}");
            RestoreOfflinePlayer();
            if (placeholderText != null)
                placeholderText.text = originalPlaceholder;
            if (hostButton != null)
                hostButton.interactable = true;
            SetJoinControlsInteractable(true);
            LoadingScreenManager.Hide();
            return false;
        }
    }

    public async void JoinGame()
    {
        string code = ipInputField != null ? ipInputField.text.Trim() : string.Empty;
        await JoinWithCodeAsync(code);
    }

    // Shared by the Join button and invites (own code); pre-checks the code before the loading screen and player swap unless already checked.
    public async System.Threading.Tasks.Task<bool> JoinWithCodeAsync(string code, bool alreadyChecked = false)
    {
        if (string.IsNullOrEmpty(code))
        {
            SetJoinStatus("Enter a join code before joining.", isError: true);
            return false;
        }

        if (NetworkManager.Singleton.IsListening)
            return false;

        SetJoinControlsInteractable(false);
        SetJoinStatus("Searching for the lobby...", isError: false);

        if (!alreadyChecked)
        {
            var check = await SessionJoin.CheckAsync(code, this);
            if (!check.ok)
            {
                SetJoinStatus(check.message, isError: true);
                SetJoinControlsInteractable(true);
                return false;
            }
        }

        LoadingScreenManager.Show("Joining session...");

        if (hostButton != null)
            hostButton.interactable = false;

        try
        {
            SyncTransportWebSockets();
            await ServicesAuth.EnsureSignedInAsync();

            (int classIndex, int colorIndex, string playerName, Vector3 position, Quaternion rotation) customization = CaptureOfflinePlayerState();

            ISession joined = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
            ActiveSession.Set(joined);

            SetJoinStatus(string.Empty, isError: false);
            if (multiplayerPanel.gameObject.activeInHierarchy)
                multiplayerPanel.Close();

            StartCoroutine(RestoreLocalCustomizationWhenSpawned(customization.classIndex, customization.colorIndex, customization.playerName));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NetworkBootstrap: failed to join - {e.Message}");
            RestoreOfflinePlayer();
            SetJoinStatus("Couldn't join that lobby, try again.", isError: true);
            if (hostButton != null)
                hostButton.interactable = true;
            LoadingScreenManager.Hide();
            return false;
        }
        finally
        {
            SetJoinControlsInteractable(true);
        }
    }

    // Relay allocates a WebSocket or raw-UDP endpoint based on RelayProtocol.Default (WSS for WebGL, DTLS otherwise - see Unity.Services.Multiplayer); UnityTransport's checkbox must match whatever the SDK actually requested, or StartHost/StartClient throws. Deriving it from that same enum instead of a second, independent platform check keeps the two from ever disagreeing.
    private static void SyncTransportWebSockets()
    {
        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is not UnityTransport transport)
            return;

        transport.UseWebSockets = RelayProtocol.Default == RelayProtocol.WSS;
    }

    private void SetJoinControlsInteractable(bool interactable)
    {
        if (joinButton != null)
            joinButton.interactable = interactable;
        if (ipInputField != null)
            ipInputField.interactable = interactable;
    }

    private static readonly Color JoinErrorColor = new Color(0.85f, 0.2f, 0.2f);

    private void SetJoinStatus(string message, bool isError)
    {
        if (joinStatusText == null)
            return;

        joinStatusText.text = message;
        joinStatusText.color = isError ? JoinErrorColor : Color.white;
    }

    // Deactivates the offline player before Host/Join, keeping its camera and customization.
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

        detachedCameraHolder = PlayerCameraRig.Detach(offlinePlayer.transform);
        if (leaveStandIn)
        {
            standIn = OfflinePlayerStandIn.Create(offlinePlayer);
            Player.OnPlayerRegistered += HideStandInOnLocalSpawn;
        }

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

    // The networked player appears mid-CreateSessionAsync, well before hosting returns, so the frozen stand-in goes the moment it registers.
    private void HideStandInOnLocalSpawn(Player player)
    {
        if (player.IsOwner)
            DestroyStandIn();
    }

    private void DestroyStandIn()
    {
        Player.OnPlayerRegistered -= HideStandInOnLocalSpawn;
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
