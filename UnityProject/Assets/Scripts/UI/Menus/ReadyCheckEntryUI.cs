using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One ready-check row: avatar, name, team and ready status.
public class ReadyCheckEntryUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI readyStatusText;
    [SerializeField] private TextMeshProUGUI teamText;
    [SerializeField] private Image readyStatusBackground;
    [SerializeField] private Color readyColor = new Color(0.4f, 0.78f, 0.42f, 1f);
    [SerializeField] private Color notReadyColor = new Color(0.4f, 0.4f, 0.43f, 1f);
    [SerializeField] private Color readyTextColor = new Color(0.08f, 0.07f, 0.06f, 1f);
    [SerializeField] private Color notReadyTextColor = new Color(0.93f, 0.90f, 0.84f, 1f);

    private Player boundPlayer;
    private string[] teamNames;

    public void Bind(Player player, string[] teamNames = null)
    {
        Unbind();

        this.teamNames = teamNames;
        boundPlayer = player;
        player.OnDisplayNameChanged += SetName;
        player.OnClassOrColorChanged += SetAvatar;
        player.OnReadyChanged += SetReady;
        player.OnTeamChanged += SetTeam;

        SetName(player.CurrentDisplayName);
        SetAvatar(player.ClassIndex, player.ColorIndex);
        SetReady(player.IsReady);
        SetTeam(player.TeamIndex);
    }

    public void Unbind()
    {
        if (boundPlayer != null)
        {
            boundPlayer.OnDisplayNameChanged -= SetName;
            boundPlayer.OnClassOrColorChanged -= SetAvatar;
            boundPlayer.OnReadyChanged -= SetReady;
            boundPlayer.OnTeamChanged -= SetTeam;
        }
        boundPlayer = null;
    }

    private void OnDestroy() => Unbind();

    private void SetName(string value)
    {
        if (nameText != null)
            nameText.text = value;
    }

    private void SetAvatar(int classIndex, int colorIndex)
    {
        if (avatarImage == null || CharacterCustomizationMenu.Instance == null)
            return;

        Sprite portrait = CharacterCustomizationMenu.Instance.GetPortrait(classIndex, colorIndex);
        if (portrait != null)
            avatarImage.sprite = portrait;
    }

    private void SetReady(bool ready)
    {
        if (readyStatusText == null)
            return;

        readyStatusText.text = ready ? "Ready" : "Not Ready";
        readyStatusText.color = ready ? readyTextColor : notReadyTextColor;
        if (readyStatusBackground != null)
            readyStatusBackground.color = ready ? readyColor : notReadyColor;
    }

    private void SetTeam(int index)
    {
        if (teamText == null)
            return;

        bool hasTeams = teamNames != null && teamNames.Length > 0;
        teamText.gameObject.SetActive(hasTeams);
        if (hasTeams)
            teamText.text = index >= 0 && index < teamNames.Length ? teamNames[index] : string.Empty;
    }
}
