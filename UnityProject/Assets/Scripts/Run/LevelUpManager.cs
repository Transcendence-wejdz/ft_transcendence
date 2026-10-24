using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Drives the level-up choice sequence: solo waits indefinitely, multiplayer (2+ players) adds a 20s timer.
public class LevelUpManager : NetworkBehaviour
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
    private readonly NetworkVariable<OfferedBonusSet> offeredBonuses = new OfflineSafeNetworkVariable<OfferedBonusSet>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool offlineSequenceActive;
    private int offlineSequenceLevel;
    private OfferedBonusSet offlineOfferedBonuses;

    private int processedLevel = 1;
    private bool frozeTime;
    private double remainingUnscaled;
    private XPManager xpManager;

    public bool SequenceActive => IsSpawned ? sequenceActive.Value : offlineSequenceActive;
    public int ProcessedLevel => processedLevel;
    public int SequenceLevel => IsSpawned ? sequenceLevel.Value : offlineSequenceLevel;
    public bool IsTimedSequence => IsSpawned && timedSequence.Value;
    public float SecondsRemaining => IsSpawned ? secondsRemaining.Value : 0f;

    public LevelUpBonusType GetOfferedBonusType(int slot) =>
        (IsSpawned ? offeredBonuses.Value : offlineOfferedBonuses).Get(slot);

    // Levels already rewarded before a scene change must not offer their bonuses again.
    void Start()
    {
        if (RunCarryOver.IsForActiveScene)
            processedLevel = RunCarryOver.ProcessedLevel;
    }

    public override void OnNetworkSpawn()
    {
        sequenceActive.OnValueChanged += HandleSequenceActiveChanged;
    }

    private void SetFrozen(bool frozen)
    {
        frozeTime = frozen;
        Time.timeScale = frozen ? 0f : 1f;
    }

    // A scene change mid-choice (e.g. a wipe, then back to the Lobby) never reaches EndSequence and would leave the game frozen.
    public override void OnDestroy()
    {
        if (frozeTime)
            SetFrozen(false);
        base.OnDestroy();
    }

    public override void OnNetworkDespawn()
    {
        sequenceActive.OnValueChanged -= HandleSequenceActiveChanged;
    }

    // Every instance (host and clients) reacts identically, so all players freeze/unfreeze together.
    private void HandleSequenceActiveChanged(bool previousValue, bool newValue)
    {
        SetFrozen(newValue);
    }

    void Update()
    {
        if (!this.HasServerAuthority())
            return;

        if (xpManager == null)
        {
            xpManager = FindAnyObjectByType<XPManager>();
            if (xpManager == null)
                return;
        }

        if (!SequenceActive)
        {
            xpManager.GetProgress(out int level, out _, out _);
            if (level > processedLevel)
                StartSequence(level);
            return;
        }

        TickActiveSequence();
    }

    // Offline solo play never registers in AllActiveInstances, same gap XPManager/GameOverCheck work around.
    private static IEnumerable<Player> ActivePlayers()
    {
        if (Player.AllActiveInstances.Count > 0)
            return Player.AllActiveInstances;

        Player local = LocalPlayer.GetPlayer();
        return local != null ? new[] { local } : System.Array.Empty<Player>();
    }

    private void TickActiveSequence()
    {
        if (IsSpawned ? timedSequence.Value : false)
        {
            remainingUnscaled -= Time.unscaledDeltaTime;
            int displaySeconds = Mathf.CeilToInt(Mathf.Max(0f, (float)remainingUnscaled));
            if (displaySeconds != Mathf.CeilToInt(secondsRemaining.Value))
                secondsRemaining.Value = displaySeconds;

            if (remainingUnscaled <= 0d)
            {
                foreach (Player player in ActivePlayers())
                    if (!player.HasChosenLevelUpBonus)
                        player.ForceLevelUpBonus(GetOfferedBonusType(Random.Range(0, BonusOptionCount)));
                EndSequence();
                return;
            }
        }

        foreach (Player player in ActivePlayers())
            if (!player.HasChosenLevelUpBonus)
                return;

        EndSequence();
    }

    private void StartSequence(int level)
    {
        processedLevel = level;
        bool multi = Player.AllActiveInstances.Count > 1;
        remainingUnscaled = multiplayerChoiceSeconds;
        OfferedBonusSet offers = PickRandomOffers();

        foreach (Player player in ActivePlayers())
            player.ResetLevelUpChoice();

        if (IsSpawned)
        {
            offeredBonuses.Value = offers;
            timedSequence.Value = multi;
            secondsRemaining.Value = multi ? multiplayerChoiceSeconds : 0f;
            sequenceLevel.Value = level;
            sequenceActive.Value = true;
        }
        else
        {
            offlineOfferedBonuses = offers;
            offlineSequenceLevel = level;
            offlineSequenceActive = true;
            SetFrozen(true);
        }
    }

    // Fisher-Yates shuffles every bonus type and offers the first 3.
    private static OfferedBonusSet PickRandomOffers()
    {
        LevelUpBonusType[] types = (LevelUpBonusType[])System.Enum.GetValues(typeof(LevelUpBonusType));
        for (int i = types.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (types[i], types[j]) = (types[j], types[i]);
        }
        return new OfferedBonusSet { first = types[0], second = types[1], third = types[2] };
    }

    private void EndSequence()
    {
        if (IsSpawned)
        {
            sequenceActive.Value = false;
        }
        else
        {
            offlineSequenceActive = false;
            SetFrozen(false);
        }
    }
}
