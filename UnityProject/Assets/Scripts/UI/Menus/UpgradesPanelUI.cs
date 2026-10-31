using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Upgrade NPC menu: lists every permanent upgrade from a row template and refreshes on any gold or level change.
public class UpgradesPanelUI : MonoBehaviour
{
    [SerializeField] private UpgradeRowUI rowTemplate;
    [SerializeField] private TMP_Text goldText;

    private readonly List<UpgradeRowUI> rows = new List<UpgradeRowUI>();

    private UpgradeRowUI CreateRow(LevelUpBonusType type)
    {
        UpgradeRowUI row = Instantiate(rowTemplate, rowTemplate.transform.parent);

        row.gameObject.name = $"Row_{type}";
        row.gameObject.SetActive(true);
        row.Bind(type);
        return row;
    }

    private void Refresh()
    {
        if (goldText != null)
        {
            if (GoldWallet.CanEarn)
                goldText.text = GoldWallet.Balance.ToString();
            else
                goldText.text = "Sign in";
        }
        foreach (UpgradeRowUI row in rows)
            row.Refresh();
    }

    private void HandleGoldChanged(int balance)
    {
        Refresh();
    }

    void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        foreach (LevelUpBonusType type in PermanentUpgrades.Types)
            rows.Add(CreateRow(type));
    }

    void OnEnable()
    {
        GoldWallet.OnChanged += HandleGoldChanged;
        PermanentUpgrades.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        GoldWallet.OnChanged -= HandleGoldChanged;
        PermanentUpgrades.OnChanged -= Refresh;
    }
}
