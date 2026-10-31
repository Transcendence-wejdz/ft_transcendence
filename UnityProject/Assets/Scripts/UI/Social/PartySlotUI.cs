using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One party slot: round class/color portrait (initial as fallback) and name, or a dim empty ring; hovering (or gamepad focus) shows the player's Friends "Name#1234", clicking (or A on) another player opens their menu.
public class PartySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [SerializeField] private GameObject filledRoot;
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text initialText;
    [SerializeField] private Image ring;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Color localRingColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color otherRingColor = new Color(0.93f, 0.90f, 0.84f, 0.6f);
    [SerializeField] private GameObject tooltip;
    [SerializeField] private TMP_Text tooltipText;

    private Player bound;
    private bool isLocal;
    private Selectable slotSelectable;

    // Raised with this slot and its player when another player's slot is clicked.
    public event System.Action<PartySlotUI, Player> Clicked;

    // Another player's slot is a gamepad stop (empty and local slots have no menu); created on first use since Bind can run before Awake.
    private Selectable SlotSelectable
    {
        get
        {
            if (slotSelectable != null)
                return slotSelectable;
            slotSelectable = GetComponent<Selectable>();
            if (slotSelectable == null)
                slotSelectable = gameObject.AddComponent<Selectable>();
            slotSelectable.transition = Selectable.Transition.None;
            slotSelectable.interactable = false;
            return slotSelectable;
        }
    }

    private string FriendsTagShown()
    {
        string tag = bound.FriendsTag;

        if (isLocal)
            tag = FriendsManager.OwnName;
        if (string.IsNullOrEmpty(tag))
            return "Unknown";
        return tag;
    }

    // Read when shown rather than cached, since a teammate's tag can arrive after they join.
    private void ShowTooltip()
    {
        if (bound == null)
            return;
        tooltipText.text = FriendsTagShown();
        tooltip.SetActive(true);
    }

    private void Unbind()
    {
        if (bound != null)
        {
            bound.OnDisplayNameChanged -= SetName;
            bound.OnClassOrColorChanged -= SetPortrait;
        }
        bound = null;
    }

    // The offline player has no deduped display name yet, so fall back to its raw name, then the friends name.
    private string NameShown(string displayName)
    {
        string shown = displayName;

        if (string.IsNullOrEmpty(shown) && bound != null)
            shown = bound.PlayerName;
        if (string.IsNullOrEmpty(shown) && isLocal)
            shown = FriendsManager.ShortName(FriendsManager.OwnName);
        if (!string.IsNullOrEmpty(shown))
            return shown;
        if (isLocal)
            return "You";
        return "Player";
    }

    private void SetName(string displayName)
    {
        string shown = NameShown(displayName);

        nameText.text = shown;
        initialText.text = char.ToUpperInvariant(shown[0]).ToString();
    }

    private void SetPortrait(int classIndex, int colorIndex)
    {
        CharacterCatalog catalog = CharacterCatalog.Instance;
        Sprite sprite = null;

        if (catalog != null)
            sprite = catalog.GetPortrait(classIndex, colorIndex);
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
        initialText.enabled = sprite == null;
    }

    private void SetRing(bool local)
    {
        if (local)
            ring.color = localRingColor;
        else
            ring.color = otherRingColor;
    }

    public void Bind(Player player, bool local)
    {
        Unbind();
        bound = player;
        isLocal = local;
        player.OnDisplayNameChanged += SetName;
        player.OnClassOrColorChanged += SetPortrait;
        SlotSelectable.interactable = !local;
        filledRoot.SetActive(true);
        emptyRoot.SetActive(false);
        SetRing(local);
        SetName(player.CurrentDisplayName);
        SetPortrait(player.ClassIndex, player.ColorIndex);
    }

    public void SetEmpty()
    {
        Unbind();
        SlotSelectable.interactable = false;
        tooltip.SetActive(false);
        filledRoot.SetActive(false);
        emptyRoot.SetActive(true);
        nameText.text = string.Empty;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip.SetActive(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        ShowTooltip();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        tooltip.SetActive(false);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        OnPointerClick(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (bound != null && !isLocal)
            Clicked?.Invoke(this, bound);
    }

    private void Awake()
    {
        _ = SlotSelectable;
    }

    private void OnDisable()
    {
        tooltip.SetActive(false);
    }

    private void OnDestroy()
    {
        Unbind();
    }
}
