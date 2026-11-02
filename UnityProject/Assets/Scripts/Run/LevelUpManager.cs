using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Drives the level-up choice: solo waits indefinitely, multiplayer adds a timer (multiplayerChoiceSeconds).
public partial class LevelUpManager : NetworkBehaviour
{
    public const int BonusOptionCount = 3;

    [SerializeField] private float multiplayerChoiceSeconds = 20f;

    private readonly NetworkVariable<bool> sequenceActive = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> sequenceLevel = new OfflineSafeNetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> timedSequence = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> secondsRemaining = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> offerSeed = new OfflineSafeNetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool offlineSequenceActive;
    private int offlineSequenceLevel;
    private int offlineOfferSeed;

    private int processedLevel = 1;
    private bool frozeTime;
    private double remainingUnscaled;
    private XPManager xpManager;

    public bool SequenceActive
    {
        get
        {
            if (IsSpawned)
                return sequenceActive.Value;
            return offlineSequenceActive;
        }
    }

    public int ProcessedLevel => processedLevel;

    public int SequenceLevel
    {
        get
        {
            if (IsSpawned)
                return sequenceLevel.Value;
            return offlineSequenceLevel;
        }
    }

    public float SecondsRemaining
    {
        get
        {
            if (IsSpawned)
                return secondsRemaining.Value;
            return 0f;
        }
    }

    private int OfferSeed
    {
        get
        {
            if (IsSpawned)
                return offerSeed.Value;
            return offlineOfferSeed;
        }
    }

    private bool IsTimed => IsSpawned && timedSequence.Value;

    private void SetFrozen(bool frozen)
    {
        frozeTime = frozen;
        if (frozen)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }

    // Every instance (host and clients) reacts identically, so all players freeze/unfreeze together.
    private void HandleSequenceActiveChanged(bool previousValue, bool newValue)
    {
        SetFrozen(newValue);
    }

    private bool FindXPManager()
    {
        if (xpManager == null)
            xpManager = FindAnyObjectByType<XPManager>();
        return xpManager != null;
    }

    private void CheckForLevelUp()
    {
        int level;

        xpManager.GetProgress(out level, out _, out _);
        if (level > processedLevel)
            StartSequence(level);
    }

    // Each player gets their own offers: every peer derives the same ones from the round's seed and that player's id.
    public LevelUpBonusType GetOfferedBonusType(int slot, ulong clientId)
    {
        return PickOffers(OfferSeed, clientId).Get(slot);
    }

    public override void OnNetworkSpawn()
    {
        sequenceActive.OnValueChanged += HandleSequenceActiveChanged;
    }

    public override void OnNetworkDespawn()
    {
        sequenceActive.OnValueChanged -= HandleSequenceActiveChanged;
    }

    // A scene change mid-choice (e.g. a wipe, then back to the Lobby) never reaches EndSequence and would leave the game frozen.
    public override void OnDestroy()
    {
        if (frozeTime)
            SetFrozen(false);
        base.OnDestroy();
    }

    // Levels already rewarded before a scene change must not offer their bonuses again.
    void Start()
    {
        if (RunCarryOver.IsForActiveScene)
            processedLevel = RunCarryOver.ProcessedLevel;
    }

    void Update()
    {
        if (!this.HasServerAuthority() || !FindXPManager() || GameOverCheck.IsWaitingForPlayers)
            return;
        if (SequenceActive)
            TickActiveSequence();
        else
            CheckForLevelUp();
    }
}
