using System.Text;
using TMPro;
using UnityEngine;

// Table of the local player's total bonuses during a run, shared by the pause and level-up menus; the Lobby has no run to show.
public class BonusStatsTableUI : MonoBehaviour
{
    private const float RefreshSeconds = 0.25f;
    private const string EmptyColor = "#6E6C66";
    private const string ValueTab = "<pos=70%>";

    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text rowsText;

    private readonly StringBuilder builder = new StringBuilder();

    private float nextRefresh;

    private void AppendRow(LevelUpBonusType type, LevelUpBonusStacks stacks)
    {
        int count = stacks.Get(type);
        string color = EmptyColor;
        string value = "-";

        if (count > 0)
        {
            color = "#" + ColorUtility.ToHtmlStringRGB(LevelUpBonusCatalog.GetColor(type));
            value = LevelUpBonusCatalog.GetTotalText(type, count);
        }
        builder.Append("<color=").Append(color).Append('>').Append(LevelUpBonusCatalog.GetDisplayName(type));
        builder.Append(ValueTab).Append(value).Append("</color>\n");
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
        foreach (LevelUpBonusType type in LevelUpBonusCatalog.DisplayOrder)
            AppendRow(type, player.BonusStacks);
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
