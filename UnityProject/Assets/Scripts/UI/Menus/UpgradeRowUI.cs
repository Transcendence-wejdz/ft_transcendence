using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One permanent upgrade in the Upgrades panel: name with level, per-level effect, and a buy button showing the next cost.
public class UpgradeRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyText;
    [SerializeField] private GameObject costIcon;

    private LevelUpBonusType type;

    public void Bind(LevelUpBonusType upgrade)
    {
        type = upgrade;
        buyButton.onClick.AddListener(() => PermanentUpgrades.Buy(type));
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
        buyButton.interactable = PermanentUpgrades.CanBuy(type);
        if (costIcon != null)
            costIcon.SetActive(!maxed);
    }
}
