using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;

// Hosts and joins Netcode sessions over Unity Relay (driven by the social panel and invites).
public partial class NetworkBootstrap : MonoBehaviour
{
    // Look and pose of the offline player, taken over by its networked copy.
    private struct OfflineCapture
    {
        public int classIndex;
        public int colorIndex;
        public string playerName;
        public Vector3 position;
        public Quaternion rotation;
    }

    private const int PublishAttempts = 3;
    private const int PublishRetryDelayMs = 1000;
    private const float SpawnTimeout = 15f;
    private const float DefaultAimDegrees = -90f;

    [SerializeField] private GameObject gameModeManagerPrefab;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private TMP_Text sessionCodeText;

    private Vector3? hostSpawnPosition;
    private Quaternion hostSpawnRotation;
    private GameObject detachedCameraHolder;
    private GameObject standIn;
    private GameObject capturedOfflinePlayer;
    private (int classIndex, int colorIndex, string playerName) capturedLook;
    private Vector2 capturedFacing;
    private float capturedAim;

    // Set by InviteToastUI before reloading Lobby, so the fresh scene picks up the join automatically.
    public static string PendingJoinCode;

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        Vector3 position;

        response.Approved = true;
        response.CreatePlayerObject = true;
        if (request.ClientNetworkId == NetworkManager.ServerClientId && hostSpawnPosition.HasValue)
        {
            response.Position = hostSpawnPosition.Value;
            response.Rotation = hostSpawnRotation;
        }
        else if (PlayerSpawner.TryGetSpawnPosition(request.ClientNetworkId, out position))
        {
            response.Position = position;
            response.Rotation = Quaternion.identity;
        }
    }

    // Start order between objects is undefined: waiting a frame lets the offline player load its saved class/color first, so the join doesn't capture (and save) the defaults.
    private IEnumerator JoinNextFrame(string code)
    {
        yield return null;
        _ = JoinWithCodeAsync(code, alreadyChecked: true);
    }

    // Shared by the social panel and invites; false on failure or if already hosting. Invites skip the loading screen and leave a frozen stand-in where the player was.
    public async Task<bool> HostGameAsync(bool showLoadingScreen = true)
    {
        if (NetworkManager.Singleton.IsListening)
            return false;
        SetSessionCodeText(string.Empty);
        if (showLoadingScreen)
            LoadingScreenManager.Show("Creating session...");
        try
        {
            await CreateHostSessionAsync(!showLoadingScreen);
            return true;
        }
        catch (System.Exception e)
        {
            FailSession("host", e);
            return false;
        }
    }

    // Shared by typed codes and invites; pre-checks the code before the loading screen and player swap unless already checked.
    public async Task<bool> JoinWithCodeAsync(string code, bool alreadyChecked = false)
    {
        if (string.IsNullOrEmpty(code) || NetworkManager.Singleton.IsListening)
            return false;
        if (!alreadyChecked && !await PassesCheckAsync(code, this))
            return false;
        LoadingScreenManager.Show("Joining session...");
        try
        {
            await JoinSessionAsync(code);
            return true;
        }
        catch (System.Exception e)
        {
            FailSession("join", e);
            return false;
        }
    }

    // Same player swap as a join, for a session we never left; a failure keeps our place, so the player can try again.
    public async Task<bool> RejoinSessionAsync(string sessionId)
    {
        LoadingScreenManager.Show("Rejoining session...");
        try
        {
            await ReconnectSessionAsync(sessionId);
            return true;
        }
        catch (System.Exception e)
        {
            SessionDisconnectHandler.StopWithoutLeaving();
            FailSession("rejoin", e);
            return false;
        }
    }

    // Start runs after every Awake, so NetworkManager.Singleton is set.
    private void Start()
    {
        string code;

        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        if (string.IsNullOrEmpty(PendingJoinCode))
            return;
        code = PendingJoinCode;
        PendingJoinCode = null;
        StartCoroutine(JoinNextFrame(code));
    }

    private void OnDestroy()
    {
        Player.OnPlayerRegistered -= ApplyCapturedStateOnLocalSpawn;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
    }
}
