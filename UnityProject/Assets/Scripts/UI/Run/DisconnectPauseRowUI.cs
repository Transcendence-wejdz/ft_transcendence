using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One row of the disconnect pause: a player's portrait, name and whether they are disconnected, not ready or ready.
public class DisconnectPauseRowUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image statusBackground;
    [SerializeField] private Color disconnectedColor = new Color(0.72f, 0.2f, 0.18f, 1f);
    [SerializeField] private Color notReadyColor = new Color(0.4f, 0.4f, 0.43f, 1f);
    [SerializeField] private Color readyColor = new Color(0.4f, 0.78f, 0.42f, 1f);
    [SerializeField] private Color lightTextColor = new Color(0.93f, 0.9f, 0.84f, 1f);
    [SerializeField] private Color darkTextColor = new Color(0.08f, 0.07f, 0.06f, 1f);

    private void SetStatus(string label, Color background, Color text)
    {
        statusText.text = label;
        statusText.color = text;
        statusBackground.color = background;
    }

    private void SetAvatar(int classIndex, int colorIndex)
    {
        Sprite portrait;

        if (CharacterCatalog.Instance == null)
            return;
        portrait = CharacterCatalog.Instance.GetPortrait(classIndex, colorIndex);
        if (portrait != null)
            avatarImage.sprite = portrait;
    }

    public void Set(string playerName, int classIndex, int colorIndex, GameOverCheck.PausePlayerState state)
    {
        nameText.text = playerName;
        SetAvatar(classIndex, colorIndex);
        if (state == GameOverCheck.PausePlayerState.Disconnected)
            SetStatus("Disconnected", disconnectedColor, lightTextColor);
        else if (state == GameOverCheck.PausePlayerState.Ready)
            SetStatus("Ready", readyColor, darkTextColor);
        else
            SetStatus("Not Ready", notReadyColor, lightTextColor);
    }
}
