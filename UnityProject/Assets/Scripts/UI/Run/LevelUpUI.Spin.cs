using UnityEngine;
using UnityEngine.UI;

// Slot machine reveal: each column scrolls down through random cards, slows down, lands with a small bounce, and only then can a bonus be chosen.
public partial class LevelUpUI
{
    [SerializeField] private float reelFirstStopSeconds = 1f;
    [SerializeField] private float reelStopGapSeconds = 0.35f;
    [SerializeField] private int reelFirstCardCount = 10;
    [SerializeField] private int reelExtraCardsPerSlot = 3;
    [SerializeField] private float landBounceHeight = 10f;
    [SerializeField] private float landBounceSeconds = 0.25f;

    private LevelUpReel[] reels;
    private HorizontalLayoutGroup reelLayout;
    private float reelStartTime;
    private bool spinning;
    private bool allSlotsLanded = true;

    private float SlotStopTime(int slot)
    {
        return reelFirstStopSeconds + slot * reelStopGapSeconds;
    }

    private int SlotCardCount(int slot)
    {
        return reelFirstCardCount + slot * reelExtraCardsPerSlot;
    }

    private static LevelUpBonusType RandomBonus()
    {
        LevelUpBonusType[] types = (LevelUpBonusType[])System.Enum.GetValues(typeof(LevelUpBonusType));

        return types[Random.Range(0, types.Length)];
    }

    private LevelUpBonusType LocalOffer(int slot)
    {
        return manager.GetOfferedBonusType(slot, LocalOfferId());
    }

    private void CreateReels()
    {
        reels = new LevelUpReel[bonusButtons.Length];
        reelLayout = bonusButtons[0].transform.parent.GetComponent<HorizontalLayoutGroup>();
        for (int i = 0; i < reels.Length; i++)
            reels[i] = new LevelUpReel((RectTransform)bonusButtons[i].transform, bonusLabels[i], BonusText);
    }

    // The row's layout is switched off during the spin, or every text change would snap the cards back into place.
    private void BeginSpin()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)reelLayout.transform);
        reelLayout.enabled = false;
        reelStartTime = Time.unscaledTime;
        spinning = true;
        allSlotsLanded = false;
        foreach (LevelUpReel reel in reels)
        {
            reel.Begin(RandomBonus);
            reel.Place(0f);
        }
    }

    // The offer enters at the top two cards before the end, so the reel visibly comes down on it.
    private void AdvanceReel(int slot, int reached)
    {
        while (reels[slot].Passed < reached)
        {
            if (reels[slot].Passed + 3 == SlotCardCount(slot))
                reels[slot].Shift(LocalOffer(slot));
            else
                reels[slot].Shift(RandomBonus());
        }
    }

    // Cubic ease-out: fast at first, slowing right down onto the last card.
    private void TickRollingSlot(int slot, float elapsed)
    {
        float progress = Mathf.Clamp01(elapsed / SlotStopTime(slot));
        float eased = 1f - Mathf.Pow(1f - progress, 3f);
        float step = reels[slot].Step;
        float scrolled = eased * SlotCardCount(slot) * step;
        int reached = Mathf.Min(Mathf.FloorToInt(scrolled / step), SlotCardCount(slot));

        AdvanceReel(slot, reached);
        reels[slot].Place(scrolled - reached * step);
        if (progress >= 1f)
            reels[slot].Land(LocalOffer(slot));
    }

    private void TickLandedSlot(int slot, float elapsed)
    {
        float bounce = Mathf.Clamp01((elapsed - SlotStopTime(slot)) / landBounceSeconds);

        reels[slot].Place(Mathf.Sin(bounce * Mathf.PI) * (1f - bounce) * landBounceHeight);
    }

    // The neighbours stay visible after the spin, as part of the machine; they only go away with the panel.
    private void TickSpin()
    {
        float elapsed = Time.unscaledTime - reelStartTime;
        bool landed = true;

        if (!spinning)
            return;
        for (int i = 0; i < reels.Length; i++)
        {
            if (reels[i].Landed)
                TickLandedSlot(i, elapsed);
            else
                TickRollingSlot(i, elapsed);
            landed = landed && reels[i].Landed;
        }
        allSlotsLanded = landed;
        if (!landed || elapsed < SlotStopTime(reels.Length - 1) + landBounceSeconds)
            return;
        spinning = false;
        foreach (LevelUpReel reel in reels)
            reel.Place(0f);
        reelLayout.enabled = true;
    }

    // When the panel closes, even mid-spin, so the row always gets its layout back.
    private void EndSpin()
    {
        spinning = false;
        allSlotsLanded = true;
        foreach (LevelUpReel reel in reels)
            reel.Hide();
        reelLayout.enabled = true;
    }
}
