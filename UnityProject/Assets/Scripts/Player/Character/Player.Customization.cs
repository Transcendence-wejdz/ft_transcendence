using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Replicated look and identity: class, color, name (deduplicated), team, readiness, Friends tag and services id.
public partial class Player
{
    private static readonly List<Player> ActiveInstances = new List<Player>();

    private static readonly Player[] OfflinePlayer = new Player[1];

    public static IReadOnlyList<Player> AllActiveInstances => ActiveInstances;

    // Session players, or the offline solo player alone, since it never registers in AllActiveInstances; read it right away, the offline list is reused.
    public static IReadOnlyList<Player> ActivePlayers
    {
        get
        {
            if (ActiveInstances.Count > 0)
                return ActiveInstances;
            OfflinePlayer[0] = LocalPlayer.GetPlayer();
            if (OfflinePlayer[0] == null)
                return Array.Empty<Player>();
            return OfflinePlayer;
        }
    }

    public static event Action<Player> OnPlayerRegistered;
    public static event Action<Player> OnPlayerUnregistered;

    public event Action<int, int> OnClassOrColorChanged;
    public event Action<string> OnDisplayNameChanged;
    public event Action<int> OnTeamChanged;
    public event Action<bool> OnReadyChanged;

    public string CurrentDisplayName { get; private set; }

    private readonly NetworkVariable<int> classIndex = new OfflineSafeNetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<int> colorIndex = new OfflineSafeNetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<FixedString32Bytes> playerName = new OfflineSafeNetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<int> teamIndex = new OfflineSafeNetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Server-writable so readiness can be reset; SetReady goes through a ServerRpc.
    private readonly NetworkVariable<bool> isReady = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Unity Friends "Name#1234", shared so teammates can see it (and add each other) from the party strip.
    private readonly NetworkVariable<FixedString64Bytes> friendsTag = new OfflineSafeNetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Unity services id, so a friend entry or the host can find this player; claimed once via ClaimServicesIdServerRpc.
    private readonly NetworkVariable<FixedString64Bytes> servicesId = new OfflineSafeNetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int ClassIndex => classIndex.Value;
    public int ColorIndex => colorIndex.Value;
    public string PlayerName => playerName.Value.ToString();
    public int TeamIndex => teamIndex.Value;
    public bool IsReady => isReady.Value;
    public string FriendsTag => friendsTag.Value.ToString();
    public string ServicesId => servicesId.Value.ToString();

    [Header("Customization")]
    [SerializeField] private PlayerNameTag nameTag;
    [SerializeField] private Camera previewCamera;

    private int layerBeforePreview;

    private void SaveProfile()
    {
        PlayerProfileStore.Save(classIndex.Value, colorIndex.Value, PlayerName);
    }

    private void SpawnCustomization()
    {
        RaiseClassOrColorChanged();
        ActiveInstances.Add(this);
        RefreshAllDisplayNames();
        if (IsOwner)
        {
            StartCoroutine(PublishServicesId());
            PublishFriendsTag();
            FriendsManager.OnChanged += PublishFriendsTag;
        }
        OnPlayerRegistered?.Invoke(this);
    }

    private void DespawnCustomization()
    {
        FriendsManager.OnChanged -= PublishFriendsTag;
        ActiveInstances.Remove(this);
        OnPlayerUnregistered?.Invoke(this);
        RefreshAllDisplayNames();
    }

    [ServerRpc]
    private void SetReadyServerRpc(bool ready)
    {
        isReady.Value = ready;
    }

    // Always re-applies visuals: a saved value equal to the default fires no OnValueChanged, leaving the prefab's Warrior sprites.
    private void StartCustomization()
    {
        int savedClassIndex;
        int savedColorIndex;
        string savedName;

        if (!this.IsLocallyControlled())
            return;
        if (!PlayerProfileStore.TryLoad(out savedClassIndex, out savedColorIndex, out savedName))
        {
            ApplyVisuals();
            return;
        }
        classIndex.Value = savedClassIndex;
        colorIndex.Value = savedColorIndex;
        if (!string.IsNullOrEmpty(savedName))
            playerName.Value = savedName;
        ApplyVisuals();
        LocalCharacterLook.NotifyProfileLoaded(gameObject);
    }

    // Toggled while the customization panel is open; the CharacterPreview layer isolates this player for the preview camera, the main camera still sees it.
    public void SetPreviewCameraActive(bool active)
    {
        if (previewCamera != null)
            previewCamera.gameObject.SetActive(active);
        if (active)
        {
            layerBeforePreview = gameObject.layer;
            gameObject.layer = LayerMask.NameToLayer("CharacterPreview");
        }
        else
            gameObject.layer = layerBeforePreview;
    }

    public void SetSelection(int newClassIndex, int newColorIndex)
    {
        if (!this.IsLocallyControlled())
            return;
        classIndex.Value = newClassIndex;
        colorIndex.Value = newColorIndex;
        SaveProfile();
        WebProfileBridge.PushSelection(newClassIndex, newColorIndex);
    }

    public void SetTeam(int newTeamIndex)
    {
        if (this.IsLocallyControlled())
            teamIndex.Value = newTeamIndex;
    }

    public void SetName(string newName)
    {
        if (!this.IsLocallyControlled())
            return;
        playerName.Value = newName;
        SaveProfile();
        WebProfileBridge.PushName(newName);
    }

    public void SetReady(bool ready)
    {
        if (!this.IsLocallyControlled() || !NetworkObject.IsSpawned)
            return;
        SetReadyServerRpc(ready);
    }

    public void ServerResetReady()
    {
        if (IsServer)
            isReady.Value = false;
    }
}
