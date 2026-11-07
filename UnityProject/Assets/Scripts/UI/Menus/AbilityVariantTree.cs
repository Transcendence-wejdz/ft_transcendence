using UnityEngine;

// Customization tree of the local player's Attack, Special and Ultimate variants; polled like the HUD slots, since the class and color can change here. Hovering a spell loops it alone on the preview.
public class AbilityVariantTree : MonoBehaviour
{
    [Tooltip("In use first, then its variants.")]
    [SerializeField] private AbilityVariantNode[] attackNodes;
    [Tooltip("In use first, then its variants.")]
    [SerializeField] private AbilityVariantNode[] specialNodes;
    [Tooltip("In use first, then its variants.")]
    [SerializeField] private AbilityVariantNode[] ultimateNodes;
    [Tooltip("For a class whose spell has no icon, same as the HUD slot.")]
    [SerializeField] private Sprite defaultAttackIcon;
    [Tooltip("For a class whose spell has no icon, same as the HUD slot.")]
    [SerializeField] private Sprite defaultSpecialIcon;
    [Tooltip("For a class whose spell has no icon, same as the HUD slot.")]
    [SerializeField] private Sprite defaultUltimateIcon;
    [Tooltip("Seconds between the end of a charge, or a cast without one, and the next loop.")]
    [SerializeField] private float previewRepeatSeconds = 2.5f;

    private ClassKit shownKit;
    private int shownColorIndex = -1;
    private bool isPreviewing;
    private AbilityType previewAbility;
    private float nextPreviewTime;

    public void BeginPreview(AbilityType ability)
    {
        isPreviewing = true;
        previewAbility = ability;
        nextPreviewTime = Time.time;
    }

    public void EndPreview()
    {
        Player player = LocalPlayer.GetPlayer();

        isPreviewing = false;
        if (player != null)
            player.ClearAbilityPreview();
    }

    private static AbilityKit VariantAt(AbilityKit[] variants, int index)
    {
        if (index >= variants.Length)
            return null;
        return variants[index];
    }

    private static Sprite IconOf(AbilityKit kit, Sprite fallback)
    {
        if (kit == null || kit.Icon == null)
            return fallback;
        return kit.Icon;
    }

    // Locked slots preview the spell in use, so the tree reads as that spell's branches.
    private void ShowBranch(AbilityVariantNode[] nodes, AbilityType ability, Sprite fallback, Color color)
    {
        AbilityKit[] variants = shownKit.Variants(ability);
        AbilityKit variant;

        for (int i = 0; i < nodes.Length; i++)
        {
            variant = VariantAt(variants, i);
            if (variant != null)
                nodes[i].Show(this, ability, IconOf(variant, fallback), color, true);
            else
                nodes[i].Show(this, ability, IconOf(VariantAt(variants, 0), fallback), color, false);
        }
    }

    private void RefreshBranches(Player player)
    {
        Color color;

        if (player.ActiveKit == shownKit && player.ColorIndex == shownColorIndex)
            return;
        shownKit = player.ActiveKit;
        shownColorIndex = player.ColorIndex;
        color = CharacterCatalog.FxLightColorOf(player.gameObject);
        ShowBranch(attackNodes, AbilityType.Attack, defaultAttackIcon, color);
        ShowBranch(specialNodes, AbilityType.Special, defaultSpecialIcon, color);
        ShowBranch(ultimateNodes, AbilityType.Ultimate, defaultUltimateIcon, color);
    }

    private void TickPreview(Player player)
    {
        if (!isPreviewing || Time.time < nextPreviewTime)
            return;
        nextPreviewTime = Time.time + previewRepeatSeconds + player.PlayAbilityPreview(previewAbility);
    }

    void Update()
    {
        Player player = LocalPlayer.GetPlayer();

        if (player == null || player.ActiveKit == null)
            return;
        RefreshBranches(player);
        TickPreview(player);
        player.TickPreviewCamera();
    }

    void OnDisable()
    {
        Player player = LocalPlayer.GetPlayer();

        EndPreview();
        if (player != null)
            player.ResetPreviewCamera();
    }
}
