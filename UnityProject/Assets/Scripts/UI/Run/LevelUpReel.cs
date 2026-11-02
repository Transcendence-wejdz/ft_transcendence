using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One column of the level-up slot machine: the real card in the center, a look-alike above and below it, and one more coming down from out of view.
public class LevelUpReel
{
    private const float CardGap = 8f;
    private const int Above = 0;
    private const int Incoming = 1;
    private const int Below = 2;
    private const int GhostCount = 3;

    private readonly RectTransform card;
    private readonly TMP_Text cardLabel;
    private readonly Func<LevelUpBonusType, string> describe;
    private readonly RectTransform[] ghosts = new RectTransform[GhostCount];
    private readonly TMP_Text[] ghostLabels = new TMP_Text[GhostCount];
    private readonly LevelUpBonusType[] ghostBonuses = new LevelUpBonusType[GhostCount];
    private LevelUpBonusType cardBonus;
    private Vector2 restPosition;

    public int Passed { get; private set; }
    public bool Landed { get; private set; }
    public float Step => card.rect.height + CardGap;

    private void CreateGhost(int index)
    {
        GameObject ghost = UnityEngine.Object.Instantiate(card.gameObject, card.parent);

        UnityEngine.Object.Destroy(ghost.GetComponent<Button>());
        ghost.AddComponent<LayoutElement>().ignoreLayout = true;
        foreach (Graphic graphic in ghost.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        ghosts[index] = (RectTransform)ghost.transform;
        ghostLabels[index] = ghost.GetComponentInChildren<TMP_Text>();
        ghost.SetActive(false);
    }

    public LevelUpReel(RectTransform card, TMP_Text cardLabel, Func<LevelUpBonusType, string> describe)
    {
        this.card = card;
        this.cardLabel = cardLabel;
        this.describe = describe;
        for (int i = 0; i < GhostCount; i++)
            CreateGhost(i);
    }

    private void ShowOnCard(LevelUpBonusType bonus)
    {
        cardBonus = bonus;
        cardLabel.text = describe(bonus);
        card.GetComponent<Image>().color = LevelUpBonusCatalog.GetColor(bonus);
    }

    private void ShowOnGhost(int index, LevelUpBonusType bonus)
    {
        ghostBonuses[index] = bonus;
        ghostLabels[index].text = describe(bonus);
        ghosts[index].GetComponent<Image>().color = LevelUpBonusCatalog.GetColor(bonus);
    }

    // The ghosts were cloned before the row's first layout, which moves each card's anchors, so positions only compare once these match.
    private void MatchGhost(RectTransform ghost)
    {
        ghost.anchorMin = card.anchorMin;
        ghost.anchorMax = card.anchorMax;
        ghost.pivot = card.pivot;
        ghost.sizeDelta = card.sizeDelta;
    }

    public void Begin(Func<LevelUpBonusType> randomBonus)
    {
        restPosition = card.anchoredPosition;
        Passed = 0;
        Landed = false;
        ShowOnCard(randomBonus());
        for (int i = 0; i < GhostCount; i++)
        {
            MatchGhost(ghosts[i]);
            ShowOnGhost(i, randomBonus());
            ghosts[i].gameObject.SetActive(true);
        }
    }

    // Every card moves one place down, and a new one enters at the top.
    public void Shift(LevelUpBonusType entering)
    {
        Passed++;
        ShowOnGhost(Below, cardBonus);
        ShowOnCard(ghostBonuses[Above]);
        ShowOnGhost(Above, ghostBonuses[Incoming]);
        ShowOnGhost(Incoming, entering);
    }

    public void Place(float offset)
    {
        Vector2 center = restPosition + Vector2.down * offset;

        card.anchoredPosition = center;
        ghosts[Above].anchoredPosition = center + Vector2.up * Step;
        ghosts[Incoming].anchoredPosition = center + Vector2.up * (2f * Step);
        ghosts[Below].anchoredPosition = center + Vector2.down * Step;
    }

    public void Land(LevelUpBonusType offer)
    {
        Landed = true;
        ShowOnCard(offer);
        ghosts[Incoming].gameObject.SetActive(false);
    }

    public void Hide()
    {
        foreach (RectTransform ghost in ghosts)
            ghost.gameObject.SetActive(false);
    }
}
