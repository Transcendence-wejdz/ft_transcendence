using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One permanent upgrade in the Upgrades panel: name with level, per-level effect, and a buy button showing the next cost.
public class UpgradeRowUI : MonoBehaviour
{
    private const float FlashSeconds = 0.35f;
    private static readonly Color FlashColor = new Color(1f, 0.25f, 0.2f, 1f);

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyText;
    [SerializeField] private GameObject costIcon;

    private LevelUpBonusType type;
    private Color normalColor;
    private Color imageColor;
    private Color textColor;
    private Coroutine flash;
    private bool bound;

    // The level sits in its own field when the row has one, else after the name.
    private void ShowNameAndLevel(int level)
    {
        string displayName = LevelUpBonusCatalog.GetDisplayName(type);
        string levelLabel = $"{level}/{PermanentUpgrades.MaxLevel}";

        if (levelText == null)
        {
            nameText.text = $"{displayName}  {levelLabel}";
            return;
        }
        nameText.text = displayName;
        levelText.text = levelLabel;
    }

    private void ShowBuyButton(bool maxed)
    {
        ColorBlock colors = buyButton.colors;

        if (maxed)
            buyText.text = "MAX";
        else
            buyText.text = PermanentUpgrades.NextCost(type).ToString();
        if (costIcon != null)
            costIcon.SetActive(!maxed);
        if (PermanentUpgrades.CanBuy(type))
            colors.normalColor = normalColor;
        else
            colors.normalColor = colors.disabledColor;
        buyButton.colors = colors;
    }

    private void RestoreColors()
    {
        buyButton.targetGraphic.color = imageColor;
        buyText.color = textColor;
    }

    // Unscaled, since a menu may be open over a paused game.
    private IEnumerator FlashRed()
    {
        float k;

        for (float t = 0f; t < FlashSeconds; t += Time.unscaledDeltaTime)
        {
            k = t / FlashSeconds;
            buyButton.targetGraphic.color = Color.Lerp(FlashColor, imageColor, k);
            buyText.color = Color.Lerp(FlashColor, textColor, k);
            yield return null;
        }
        RestoreColors();
        flash = null;
    }

    private void OnBuyClicked()
    {
        if (PermanentUpgrades.CanBuy(type))
        {
            PermanentUpgrades.Buy(type);
            return;
        }
        if (flash != null)
            StopCoroutine(flash);
        flash = StartCoroutine(FlashRed());
    }

    // The button stays interactable so gamepad navigation can reach it; an unaffordable one only looks disabled.
    public void Bind(LevelUpBonusType upgrade)
    {
        type = upgrade;
        normalColor = buyButton.colors.normalColor;
        imageColor = buyButton.targetGraphic.color;
        textColor = buyText.color;
        bound = true;
        buyButton.onClick.AddListener(OnBuyClicked);
        descriptionText.text = $"{LevelUpBonusCatalog.GetDescription(type)} per level";
        Refresh();
    }

    public void Refresh()
    {
        int level = PermanentUpgrades.GetLevel(type);

        ShowNameAndLevel(level);
        ShowBuyButton(level >= PermanentUpgrades.MaxLevel);
    }

    // Restores the colors a flash left mid-way; the unbound row template keeps its own.
    void OnDisable()
    {
        if (!bound)
            return;
        flash = null;
        RestoreColors();
    }
}
