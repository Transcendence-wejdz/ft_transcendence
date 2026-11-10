using UnityEngine;
using UnityEngine.UI;

// Shows the icon of the local player's ability in this slot, in that player's FX light color; an ability without one keeps the slot's default icon.
public class AbilitySlotUI : MonoBehaviour
{
    [SerializeField] private AbilityType ability;
    [SerializeField] private Image icon;

    private Sprite defaultSprite;
    private Color defaultColor;
    private AbilityKit shownKit;
    private int shownColorIndex = -1;

    // The light FX color, since the Black FX color would vanish on the slot's dark background (it glows white instead).
    private void Show(AbilityKit kit, Player player)
    {
        Color color = defaultColor;

        icon.sprite = defaultSprite;
        if (kit != null && kit.Icon != null)
        {
            icon.sprite = kit.Icon;
            color = CharacterCatalog.FxLightColorOf(player.gameObject);
        }
        color.a = icon.color.a;
        icon.color = color;
    }

    void Awake()
    {
        defaultSprite = icon.sprite;
        defaultColor = icon.color;
    }

    // Polled like the mana costs: the class and color can change at any time in the Lobby.
    void Update()
    {
        Player player = LocalPlayer.GetPlayer();
        AbilityKit kit;

        if (player == null || player.ActiveKit == null)
            return;
        kit = player.Ability(ability);
        if (kit == shownKit && player.ColorIndex == shownColorIndex)
            return;
        shownKit = kit;
        shownColorIndex = player.ColorIndex;
        Show(kit, player);
    }
}
