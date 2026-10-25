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

    void Update()
    {
        Player player = LocalPlayer.GetPlayer();
        ClassKit kit = player != null ? player.ActiveKit : null;
        if (kit == null)
            return;

        Refresh(specialCost, specialIcon, kit.SpecialManaCost, player.CurrentMana, ref shownSpecial);
        Refresh(ultimateCost, ultimateIcon, kit.UltimateManaCost, player.CurrentMana, ref shownUltimate);
    }

    // Text only changes with the cost, so nothing allocates per frame.
    private void Refresh(TMP_Text label, Image icon, float cost, float mana, ref float shown)
    {
        if (label != null && !Mathf.Approximately(cost, shown))
        {
            shown = cost;
            label.text = cost > 0f ? Mathf.CeilToInt(cost).ToString() : string.Empty;
        }

        if (icon != null)
        {
            Color color = icon.color;
            color.a = mana >= cost ? 1f : dimmedAlpha;
            icon.color = color;
        }
    }
}
