using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One party slot: round class/color portrait (initial as fallback) and name, or a dim empty ring; hovering shows the player's Friends "Name#1234", clicking another player opens their menu.
public class PartySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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

    // Raised with this slot and its player when another player's slot is clicked.
    public event System.Action<PartySlotUI, Player> Clicked;

    public void Bind(Player player, bool local)
    {
        Unbind();
        bound = player;
        isLocal = local;
        player.OnDisplayNameChanged += SetName;
        player.OnClassOrColorChanged += SetPortrait;

        filledRoot.SetActive(true);
        emptyRoot.SetActive(false);
        ring.color = local ? localRingColor : otherRingColor;
        SetName(player.CurrentDisplayName);
        SetPortrait(player.ClassIndex, player.ColorIndex);
    }

    // Read on hover rather than cached, since a teammate's tag can arrive after they join.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (bound == null)
            return;

        string tag = isLocal ? FriendsManager.OwnName : bound.FriendsTag;
        tooltipText.text = string.IsNullOrEmpty(tag) ? "Unknown" : tag;
        tooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) => tooltip.SetActive(false);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (bound != null && !isLocal)
            Clicked?.Invoke(this, bound);
    }

    private void OnDisable()
    {
        tooltip.SetActive(false);
    }

    public void SetEmpty()
    {
        Unbind();
        tooltip.SetActive(false);
        filledRoot.SetActive(false);
        emptyRoot.SetActive(true);
        nameText.text = string.Empty;
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

    private void OnDestroy() => Unbind();

    // The offline player has no deduped display name yet, so fall back to its raw name, then the friends name.
    private void SetName(string displayName)
    {
        string shown = displayName;
        if (string.IsNullOrEmpty(shown) && bound != null)
            shown = bound.PlayerName;
        if (string.IsNullOrEmpty(shown) && isLocal)
            shown = FriendsManager.ShortName(FriendsManager.OwnName);
        if (string.IsNullOrEmpty(shown))
            shown = isLocal ? "You" : "Player";

        nameText.text = shown;
        initialText.text = char.ToUpperInvariant(shown[0]).ToString();
    }

    private void SetPortrait(int classIndex, int colorIndex)
    {
        CharacterCatalog catalog = CharacterCatalog.Instance;
        Sprite sprite = catalog != null ? catalog.GetPortrait(classIndex, colorIndex) : null;
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
        initialText.enabled = sprite == null;
    }
}
