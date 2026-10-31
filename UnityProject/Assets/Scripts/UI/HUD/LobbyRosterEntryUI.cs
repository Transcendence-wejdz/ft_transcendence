using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One roster row, explicitly bound to a player; a downed ally swaps its health bar for a revive call, then a revive bar.
public class LobbyRosterEntryUI : MonoBehaviour
{
    private const float NeedReviveMinAlpha = 0.45f;

    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private GameObject healthBarRoot;
    [SerializeField] private TMP_Text needReviveLabel;
    [SerializeField] private GameObject reviveBarRoot;
    [SerializeField] private Image reviveFillImage;
    [SerializeField] private float needRevivePulseSpeed = 4f;

    private Player boundPlayer;

    private static void SetActiveIfChanged(GameObject target, bool active)
    {
        if (target.activeSelf != active)
            target.SetActive(active);
    }

    private void SetName(string value)
    {
        if (nameText != null)
            nameText.text = value;
    }

    private void SetAvatar(int classIndex, int colorIndex)
    {
        Sprite portrait;

        if (avatarImage == null || CharacterCatalog.Instance == null)
            return;
        portrait = CharacterCatalog.Instance.GetPortrait(classIndex, colorIndex);
        if (portrait != null)
            avatarImage.sprite = portrait;
    }

    private void SetHealth(float current, float max)
    {
        float fraction = 0f;

        if (healthFillImage == null)
            return;
        if (max > 0f)
            fraction = current / max;
        BrushFill.Set(healthFillImage, fraction);
    }

    private void PulseNeedRevive()
    {
        float wave = (Mathf.Sin(Time.unscaledTime * needRevivePulseSpeed) + 1f) * 0.5f;

        needReviveLabel.alpha = Mathf.Lerp(NeedReviveMinAlpha, 1f, wave);
    }

    // Health bar while alive; once downed, the revive call, replaced by the revive bar while someone rides.
    private void ShowDownedState(bool dead, bool reviving)
    {
        if (healthBarRoot != null)
            SetActiveIfChanged(healthBarRoot, !dead);
        SetActiveIfChanged(needReviveLabel.gameObject, dead && !reviving);
        SetActiveIfChanged(reviveBarRoot, reviving);
    }

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

    private void OnDestroy()
    {
        Unbind();
    }

    // Polled since the downed state and revive progress are derived per frame on Player, not raised as events.
    private void Update()
    {
        bool dead;
        float revive;
        bool reviving;

        if (boundPlayer == null || needReviveLabel == null || reviveBarRoot == null)
            return;
        dead = boundPlayer.IsDead;
        revive = boundPlayer.ReviveFraction;
        reviving = dead && revive >= 0f;
        ShowDownedState(dead, reviving);
        if (reviving && reviveFillImage != null)
            BrushFill.Set(reviveFillImage, revive);
        else if (dead)
            PulseNeedRevive();
    }
}
