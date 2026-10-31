using TMPro;
using Unity.Netcode;
using UnityEngine;

// World-space name tag above a player, shown only in multiplayer and tinted to the player's color.
public class PlayerNameTag : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameLabel;

    private Player player;
    private bool inSession;
    private bool hidden;

    private void RefreshVisibility()
    {
        bool show = inSession && !hidden;

        if (gameObject.activeSelf != show)
            gameObject.SetActive(show);
    }

    private void ApplyColor(int classIndex, int colorIndex)
    {
        if (nameLabel != null && CharacterCatalog.Instance != null)
            nameLabel.color = CharacterCatalog.Instance.GetAccentColor(colorIndex);
    }

    public void SetName(string value)
    {
        if (nameLabel != null)
            nameLabel.text = value;
        inSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        RefreshVisibility();
    }

    // Hides the tag while something else (the revive bar) takes its place above the head.
    public void SetHidden(bool value)
    {
        if (hidden == value)
            return;
        hidden = value;
        RefreshVisibility();
    }

    void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    void OnEnable()
    {
        if (player == null)
            return;
        player.OnClassOrColorChanged += ApplyColor;
        ApplyColor(player.ClassIndex, player.ColorIndex);
    }

    void OnDisable()
    {
        if (player != null)
            player.OnClassOrColorChanged -= ApplyColor;
    }
}
