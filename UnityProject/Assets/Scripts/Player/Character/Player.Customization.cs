using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public partial class Player
{
    private static readonly List<Player> ActiveInstances = new List<Player>();

    public static IReadOnlyList<Player> AllActiveInstances => ActiveInstances;

    private static readonly Player[] OfflinePlayer = new Player[1];

    // Session players, or the offline solo player alone, since it never registers in AllActiveInstances; read it right away, the offline list is reused.
    public static IReadOnlyList<Player> ActivePlayers
    {
        get
        {
            if (ActiveInstances.Count > 0)
                return ActiveInstances;
            OfflinePlayer[0] = LocalPlayer.GetPlayer();
            return OfflinePlayer[0] != null ? OfflinePlayer : Array.Empty<Player>();
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
        {
            gameObject.layer = layerBeforePreview;
        }
    }

    private void AwakeCustomization()
    {
        classIndex.OnValueChanged += (_, _) => { ApplyVisuals(); OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value); };
        colorIndex.OnValueChanged += (_, _) => { ApplyVisuals(); OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value); };
        playerName.OnValueChanged += (_, _) => RefreshAllDisplayNames();
        teamIndex.OnValueChanged += (_, newValue) => OnTeamChanged?.Invoke(newValue);
        isReady.OnValueChanged += (_, newValue) => OnReadyChanged?.Invoke(newValue);
    }

    private void SpawnCustomization()
    {
        ApplyVisuals();
        OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value);
        ActiveInstances.Add(this);
        RefreshAllDisplayNames();
        if (IsOwner)
        {
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

    // Only once spawned (never from Awake), and again when the friends name arrives or changes; also shares our services id.
    private void PublishFriendsTag()
    {
        if (this == null || !IsOwner || !IsSpawned || !FriendsManager.IsReady)
            return;
        if (FriendsManager.OwnName != FriendsTag)
            friendsTag.Value = FriendsManager.OwnName;
        if (string.IsNullOrEmpty(ServicesId) && !string.IsNullOrEmpty(FriendsManager.OwnId))
            ClaimServicesIdServerRpc(FriendsManager.OwnId);
    }

    // Write-once, and refused if another spawned player already holds that id.
    [ServerRpc]
    private void ClaimServicesIdServerRpc(FixedString64Bytes id)
    {
        string claimed = id.ToString();
        if (!string.IsNullOrEmpty(ServicesId) || string.IsNullOrEmpty(claimed))
            return;
        foreach (Player other in ActiveInstances)
            if (other != this && other.ServicesId == claimed)
            {
                Debug.LogWarning($"Player: client {OwnerClientId} tried to claim services id {claimed}, already held by client {other.OwnerClientId}");
                return;
            }
        servicesId.Value = id;
    }

    private static void RefreshAllDisplayNames()
    {
        var groups = new Dictionary<string, List<Player>>();
        foreach (Player instance in ActiveInstances)
        {
            string baseName = instance.playerName.Value.ToString();
            if (!groups.TryGetValue(baseName, out List<Player> group))
            {
                group = new List<Player>();
                groups[baseName] = group;
            }
            group.Add(instance);
        }

        foreach (List<Player> group in groups.Values)
        {
            group.Sort((a, b) => a.NetworkObjectId.CompareTo(b.NetworkObjectId));
            for (int i = 0; i < group.Count; i++)
            {
                string baseName = group[i].playerName.Value.ToString();
                group[i].ApplyName(group.Count > 1 ? $"{baseName} ({i + 1})" : baseName);
            }
        }
    }

    // Always re-applies visuals: a saved value equal to the default fires no OnValueChanged, leaving the prefab's Warrior sprites.
    private void StartCustomization()
    {
        if (!this.IsLocallyControlled())
            return;

        if (!PlayerProfileStore.TryLoad(out int savedClassIndex, out int savedColorIndex, out string savedName))
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

    public void SetSelection(int newClassIndex, int newColorIndex)
    {
        if (!this.IsLocallyControlled())
            return;

        classIndex.Value = newClassIndex;
        colorIndex.Value = newColorIndex;
        PlayerProfileStore.Save(classIndex.Value, colorIndex.Value, playerName.Value.ToString());
        WebProfileBridge.PushSelection(newClassIndex, newColorIndex);
    }

    public void SetTeam(int newTeamIndex)
    {
        if (!this.IsLocallyControlled())
            return;

        teamIndex.Value = newTeamIndex;
    }

    public void SetName(string newName)
    {
        if (!this.IsLocallyControlled())
            return;

        playerName.Value = newName;
        PlayerProfileStore.Save(classIndex.Value, colorIndex.Value, playerName.Value.ToString());
        WebProfileBridge.PushName(newName);
    }

    public void SetReady(bool ready)
    {
        if (!this.IsLocallyControlled())
            return;

        if (!NetworkObject.IsSpawned)
            return;

        SetReadyServerRpc(ready);
    }

    [ServerRpc]
    private void SetReadyServerRpc(bool ready)
    {
        isReady.Value = ready;
    }

    public void ServerResetReady()
    {
        if (!IsServer)
            return;

        isReady.Value = false;
    }

    private void ApplyVisuals()
    {
        if (CharacterCatalog.Instance != null)
            CharacterCatalog.Instance.ApplyVisuals(gameObject, classIndex.Value, colorIndex.Value);
    }

    private void ApplyName(string value)
    {
        CurrentDisplayName = value;
        if (nameTag != null)
            nameTag.SetName(value);
        OnDisplayNameChanged?.Invoke(value);
    }
}
