using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One friend-request row: avatar, name, a status line, Accept for incoming requests and a close button (decline or cancel).
public class PauseRequestRowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image avatar;
    [SerializeField] private TMP_Text initialText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private Image background;
    [SerializeField] private Color backgroundColor = new Color(0.82f, 0.82f, 0.82f, 0.93f);
    [SerializeField] private Color hoverBackgroundColor = new Color(1f, 1f, 1f, 1f);

    public void Setup(string displayName, string status, Color statusColor, Action onAccept, Action onRemove)
    {
        nameText.text = displayName;
        initialText.text = FriendAvatar.InitialFor(displayName);
        avatar.color = FriendAvatar.ColorFor(displayName);
        statusText.text = status;
        statusText.color = statusColor;
        Bind(acceptButton, onAccept);
        Bind(removeButton, onRemove);
        background.color = backgroundColor;
    }

    public void SetInteractable(bool interactable)
    {
        acceptButton.interactable = interactable;
        removeButton.interactable = interactable;
    }

    public void OnPointerEnter(PointerEventData eventData) => background.color = hoverBackgroundColor;
    public void OnPointerExit(PointerEventData eventData) => background.color = backgroundColor;

    private static void Bind(Button button, Action action)
    {
        button.onClick.RemoveAllListeners();
        button.gameObject.SetActive(action != null);
        if (action != null)
            button.onClick.AddListener(() => action());
    }
}
