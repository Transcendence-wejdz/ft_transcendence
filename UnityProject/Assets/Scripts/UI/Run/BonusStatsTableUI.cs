using System.Text;
using TMPro;
using UnityEngine;

// Table of the local player's bonuses during a run (permanent, earned this run, total), shared by the pause and level-up menus; the Lobby has no run to show.
public class BonusStatsTableUI : MonoBehaviour
{
    private const float RefreshSeconds = 0.25f;
    private const string EmptyColor = "#6E6C66";
    private const string HeaderColor = "#9E9C94";
    private const string PermanentTab = "<pos=36%>";
    private const string RunTab = "<pos=57.5%>";
    private const string TotalTab = "<pos=79%>";
    private const float MinShownStacks = 0.001f;
    private const string NoValue = "-";

    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text rowsText;

    private readonly StringBuilder builder = new StringBuilder();

    private float nextRefresh;

    // Shorter names than the catalog's, so the three value columns fit a narrow table.
    private static string RowLabel(LevelUpBonusType type)
    {
        switch (type)
        {
            case LevelUpBonusType.AttackSpeed: return "Atk Speed";
            case LevelUpBonusType.SpecialCooldown: return "Special CD";
            case LevelUpBonusType.HealthRegen: return "HP Regen";
            case LevelUpBonusType.ManaMax: return "Mana";
            case LevelUpBonusType.ManaRegen: return "MP Regen";
            case LevelUpBonusType.Size: return "Atk Size";
            case LevelUpBonusType.AttackMobility: return "Mobility";
            case LevelUpBonusType.MoveSpeed: return "Speed";
            case LevelUpBonusType.DodgeRecharge: return "Dash CD";
            case LevelUpBonusType.CritChance: return "Crit";
            default: return LevelUpBonusCatalog.GetDisplayName(type);
        }
    }

    private static string ValueText(LevelUpBonusType type, float count)
    {
        if (count < MinShownStacks)
            return NoValue;
        return LevelUpBonusCatalog.GetTotalText(type, count);
    }

    private void AppendLine(string color, string label, string permanent, string run, string total)
    {
        builder.Append("<color=").Append(color).Append('>').Append(label);
        builder.Append(PermanentTab).Append(permanent);
        builder.Append(RunTab).Append(run);
        builder.Append(TotalTab).Append(total).Append("</color>\n");
    }

    // The stacks hold both sources: a run starts at the permanent levels, so whatever is above them was earned this run.
    private void AppendRow(LevelUpBonusType type, LevelUpBonusStacks stacks)
    {
        float total = stacks.Get(type);
        float permanent = Mathf.Min(PermanentUpgrades.PermanentStacks(type), total);
        string color = EmptyColor;

        if (total > 0)
            color = "#" + ColorUtility.ToHtmlStringRGB(LevelUpBonusCatalog.GetColor(type));
        AppendLine(color, RowLabel(type), ValueText(type, permanent), ValueText(type, total - permanent), ValueText(type, total));
    }

    private void Refresh()
    {
        Player player = LocalPlayer.GetPlayer();
        bool visible = player != null && !ActiveSession.InLobby;

        if (content.activeSelf != visible)
            content.SetActive(visible);
        if (!visible)
            return;
        builder.Clear();
        AppendLine(HeaderColor, string.Empty, "Base", "Run", "Total");
        foreach (LevelUpBonusType type in LevelUpBonusCatalog.DisplayOrder)
            AppendRow(type, player.BonusStacks);
        foreach (LevelUpBonusType type in LevelUpBonusCatalog.SpecialTypes)
        {
            if (player.BonusStacks.Get(type) > 0)
                AppendRow(type, player.BonusStacks);
        }
        rowsText.text = builder.ToString();
    }

    private void OnEnable()
    {
        Refresh();
    }

    // Unscaled, since both menus can be open while time is frozen.
    private void Update()
    {
        if (Time.unscaledTime < nextRefresh)
            return;
        nextRefresh = Time.unscaledTime + RefreshSeconds;
        Refresh();
    }
}
