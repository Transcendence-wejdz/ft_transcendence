using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One permanent upgrade in the Upgrades panel: name with level, per-level effect, and a buy button showing the next cost.
public class UpgradeRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyText;

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
        nameText.text = $"{LevelUpBonusCatalog.GetDisplayName(type)}  {level}/{PermanentUpgrades.MaxLevel}";
        buyText.text = maxed ? "MAX" : PermanentUpgrades.NextCost(type).ToString();
        buyButton.interactable = PermanentUpgrades.CanBuy(type);
    }
}
