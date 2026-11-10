using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One spell in the customization tree: its icon, dimmed under a Coming soon banner while the variant doesn't exist; hovering an available one previews it.
public class AbilityVariantNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float LockedAlpha = 0.3f;

    [SerializeField] private Image icon;
    [SerializeField] private GameObject comingSoon;

    private AbilityVariantTree tree;
    private AbilityType ability;
    private bool isAvailable;

    public void Show(AbilityVariantTree owner, AbilityType slot, Sprite sprite, Color color, bool available)
    {
        tree = owner;
        ability = slot;
        isAvailable = available;
        icon.sprite = sprite;
        if (!available)
            color.a = LockedAlpha;
        icon.color = color;
        comingSoon.SetActive(!available);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isAvailable)
            tree.BeginPreview(ability);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isAvailable)
            tree.EndPreview();
    }
}
