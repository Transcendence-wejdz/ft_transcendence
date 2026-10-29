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
        bool maxed = level >= PermanentUpgrades.MaxLevel;
        string displayName = LevelUpBonusCatalog.GetDisplayName(type);
        if (levelText != null)
        {
            nameText.text = displayName;
            levelText.text = $"{level}/{PermanentUpgrades.MaxLevel}";
        }
        else
            nameText.text = $"{displayName}  {level}/{PermanentUpgrades.MaxLevel}";
        buyText.text = maxed ? "MAX" : PermanentUpgrades.NextCost(type).ToString();
        if (costIcon != null)
            costIcon.SetActive(!maxed);

        ColorBlock colors = buyButton.colors;
        colors.normalColor = PermanentUpgrades.CanBuy(type) ? normalColor : colors.disabledColor;
        buyButton.colors = colors;
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

    // Restores the colors a flash left mid-way; the unbound row template keeps its own.
    void OnDisable()
    {
        if (!bound)
            return;
        flash = null;
        buyButton.targetGraphic.color = imageColor;
        buyText.color = textColor;
    }

    // Unscaled, since a menu may be open over a paused game.
    private IEnumerator FlashRed()
    {
        for (float t = 0f; t < FlashSeconds; t += Time.unscaledDeltaTime)
        {
            float k = t / FlashSeconds;
            buyButton.targetGraphic.color = Color.Lerp(FlashColor, imageColor, k);
            buyText.color = Color.Lerp(FlashColor, textColor, k);
            yield return null;
        }
        buyButton.targetGraphic.color = imageColor;
        buyText.color = textColor;
        flash = null;
    }
}
