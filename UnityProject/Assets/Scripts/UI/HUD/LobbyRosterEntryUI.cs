using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One roster row, explicitly bound to a player; a downed ally swaps its health bar for a revive call, then a revive bar.
public class LobbyRosterEntryUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private GameObject healthBarRoot;
    [SerializeField] private TMP_Text needReviveLabel;
    [SerializeField] private GameObject reviveBarRoot;
    [SerializeField] private Image reviveFillImage;
    [SerializeField] private float needRevivePulseSpeed = 4f;

    private Player boundPlayer;

    public void Bind(Player player)
    {
        Unbind();

        boundPlayer = player;

        player.OnDisplayNameChanged += SetName;
        player.OnClassOrColorChanged += SetAvatar;
        player.OnHealthReplicated += SetHealth;

        SetName(player.CurrentDisplayName);
        SetAvatar(player.ClassIndex, player.ColorIndex);
        SetHealth(player.CurrentHealth, player.MaxHealth);
    }

    public void Unbind()
    {
        if (boundPlayer != null)
        {
            boundPlayer.OnDisplayNameChanged -= SetName;
            boundPlayer.OnClassOrColorChanged -= SetAvatar;
            boundPlayer.OnHealthReplicated -= SetHealth;
        }

        boundPlayer = null;
    }

    private void OnDestroy() => Unbind();

    // Polled since the downed state and revive progress are derived per frame on Player, not raised as events.
    private void Update()
    {
        if (boundPlayer == null || needReviveLabel == null || reviveBarRoot == null)
            return;

        bool dead = boundPlayer.IsDead;
        float revive = boundPlayer.ReviveFraction;
        bool reviving = dead && revive >= 0f;

        if (healthBarRoot != null && healthBarRoot.activeSelf == dead)
            healthBarRoot.SetActive(!dead);
        if (needReviveLabel.gameObject.activeSelf != (dead && !reviving))
            needReviveLabel.gameObject.SetActive(dead && !reviving);
        if (reviveBarRoot.activeSelf != reviving)
            reviveBarRoot.SetActive(reviving);

        if (reviving && reviveFillImage != null)
            BrushFill.Set(reviveFillImage, revive);
        else if (dead)
            needReviveLabel.alpha = Mathf.Lerp(0.45f, 1f, (Mathf.Sin(Time.unscaledTime * needRevivePulseSpeed) + 1f) * 0.5f);
    }

    private void SetName(string value)
    {
        if (nameText != null)
            nameText.text = value;
    }

    private void SetAvatar(int classIndex, int colorIndex)
    {
        if (avatarImage == null || CharacterCatalog.Instance == null)
            return;

        Sprite portrait = CharacterCatalog.Instance.GetPortrait(classIndex, colorIndex);
        if (portrait != null)
            avatarImage.sprite = portrait;
    }

    private void SetHealth(float current, float max)
    {
        if (healthFillImage != null)
            BrushFill.Set(healthFillImage, max > 0f ? current / max : 0f);
    }
}
