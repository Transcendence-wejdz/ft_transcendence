using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows the local player's Special/Ultimate mana costs on their slots and dims a slot's icon while the mana isn't there.
public class AbilityManaCostUI : MonoBehaviour
{
    [SerializeField] private TMP_Text specialCost;
    [SerializeField] private TMP_Text ultimateCost;
    [SerializeField] private Image specialIcon;
    [SerializeField] private Image ultimateIcon;
    [SerializeField] private float dimmedAlpha = 0.35f;

    private float shownSpecial = -1f;
    private float shownUltimate = -1f;

    private static float ManaCost(Player player, AbilityType ability)
    {
        AbilityKit kit = player.Ability(ability);

        if (kit == null)
            return 0f;
        return kit.ManaCost;
    }

    // Text only changes with the cost, so nothing allocates per frame.
    private static string CostText(float cost)
    {
        if (cost > 0f)
            return Mathf.CeilToInt(cost).ToString();
        return string.Empty;
    }

    private void DimIcon(Image icon, bool affordable)
    {
        Color color = icon.color;

        color.a = dimmedAlpha;
        if (affordable)
            color.a = 1f;
        icon.color = color;
    }

    private void Refresh(TMP_Text label, Image icon, float cost, float mana, ref float shown)
    {
        if (label != null && !Mathf.Approximately(cost, shown))
        {
            shown = cost;
            label.text = CostText(cost);
        }
        if (icon != null)
            DimIcon(icon, mana >= cost);
    }

    void Update()
    {
        Player player = LocalPlayer.GetPlayer();

        if (player == null || player.ActiveKit == null)
            return;
        Refresh(specialCost, specialIcon, ManaCost(player, AbilityType.Special), player.CurrentMana, ref shownSpecial);
        Refresh(ultimateCost, ultimateIcon, ManaCost(player, AbilityType.Ultimate), player.CurrentMana, ref shownUltimate);
    }
}
