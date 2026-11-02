using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// One level-up round: offers picked, published, timed in multiplayer, and closed once everyone chose.
public partial class LevelUpManager
{
    private const int OfferSeedPlayerStep = 7919;

    private static bool AllPlayersChose()
    {
        foreach (Player player in Player.ActivePlayers)
        {
            if (!player.HasChosenLevelUpBonus)
                return false;
        }
        return true;
    }

    private void ForceMissingChoices()
    {
        foreach (Player player in Player.ActivePlayers)
        {
            if (!player.HasChosenLevelUpBonus)
                player.ForceLevelUpBonus(GetOfferedBonusType(Random.Range(0, BonusOptionCount), player.OwnerClientId));
        }
    }

    // Counts down on unscaled time (the game is frozen) and only replicates whole seconds; true once it ran out.
    private bool TickTimer()
    {
        int displaySeconds;

        remainingUnscaled -= Time.unscaledDeltaTime;
        displaySeconds = Mathf.CeilToInt(Mathf.Max(0f, (float)remainingUnscaled));
        if (displaySeconds != Mathf.CeilToInt(secondsRemaining.Value))
            secondsRemaining.Value = displaySeconds;
        return remainingUnscaled <= 0d;
    }

    private void TickActiveSequence()
    {
        if (IsResuming)
        {
            TickResumeCountdown();
            return;
        }
        if (IsTimed && TickTimer())
        {
            ForceMissingChoices();
            EndSequence();
            return;
        }
        if (AllPlayersChose())
            EndSequence();
    }

    // Fisher-Yates shuffles every bonus type and offers the first 3; System.Random gives every peer the same result for the same seed.
    private static OfferedBonusSet PickOffers(int seed, ulong clientId)
    {
        LevelUpBonusType[] types = (LevelUpBonusType[])System.Enum.GetValues(typeof(LevelUpBonusType));
        System.Random random = new System.Random(unchecked(seed + (int)clientId * OfferSeedPlayerStep));
        int j;

        for (int i = types.Length - 1; i > 0; i--)
        {
            j = random.Next(0, i + 1);
            (types[i], types[j]) = (types[j], types[i]);
        }
        return new OfferedBonusSet { first = types[0], second = types[1], third = types[2] };
    }

    private void PublishSequence(int level, int seed, bool multi)
    {
        offerSeed.Value = seed;
        timedSequence.Value = multi;
        if (multi)
            secondsRemaining.Value = multiplayerChoiceSeconds;
        else
            secondsRemaining.Value = 0f;
        sequenceLevel.Value = level;
        sequenceActive.Value = true;
    }

    private void StartSequence(int level)
    {
        bool multi = Player.AllActiveInstances.Count > 1;
        int seed = Random.Range(int.MinValue, int.MaxValue);

        processedLevel = level;
        remainingUnscaled = multiplayerChoiceSeconds;
        foreach (Player player in Player.ActivePlayers)
            player.ResetLevelUpChoice();
        if (IsSpawned)
        {
            PublishSequence(level, seed, multi);
            return;
        }
        offlineOfferSeed = seed;
        offlineSequenceLevel = level;
        offlineSequenceActive = true;
        SetFrozen(true);
    }

    // A chained level goes straight to its own choice; the countdown only comes before the game actually resumes.
    private void EndSequence()
    {
        if (HasNextLevelWaiting())
            FinishSequence();
        else
            StartResumeCountdown();
    }

    private void FinishSequence()
    {
        if (IsSpawned)
        {
            sequenceActive.Value = false;
            return;
        }
        offlineSequenceActive = false;
        SetFrozen(false);
    }
}
