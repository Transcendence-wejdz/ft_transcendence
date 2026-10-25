using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One pause-menu friend row: initial avatar, name, presence line, a hover highlight, and a round Invite button that brightens on hover or gamepad focus.
public class PauseFriendRowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image avatar;
    [SerializeField] private TMP_Text initialText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button inviteButton;
    [SerializeField] private CanvasGroup inviteGroup;
    [SerializeField] private CanvasGroup rowGroup;
    [SerializeField] private Image background;
    [SerializeField] private Color backgroundColor = new Color(0.13f, 0.14f, 0.15f, 0.88f);
    [SerializeField] private Color hoverBackgroundColor = new Color(0.24f, 0.25f, 0.26f, 0.95f);
    [SerializeField] private float offlineAlpha = 0.5f;
    [SerializeField] private float offlineHoverAlpha = 0.8f;
    [SerializeField] private float idleInviteAlpha = 0.35f;
    [SerializeField] private float revealSpeed = 8f;

    private bool hovered;
    private bool online;
    private float highlight;

    public void Setup(string displayName, string status, Color statusColor, bool isOnline, Action onInvite)
    {
        online = isOnline;
        nameText.text = displayName;
        initialText.text = displayName.Length > 0 ? char.ToUpperInvariant(displayName[0]).ToString() : "?";
        avatar.color = AvatarColor(displayName);
        statusText.text = status;
        statusText.color = statusColor;

        inviteButton.onClick.RemoveAllListeners();
        inviteButton.gameObject.SetActive(onInvite != null);
        if (onInvite != null)
            inviteButton.onClick.AddListener(() => onInvite());

        highlight = 0f;
        Apply();
    }

    public void SetInteractable(bool interactable)
    {
        inviteButton.interactable = interactable;
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void OnDisable()
    {
        hovered = false;
        highlight = 0f;
        Apply();
    }

    // Unscaled time since the pause menu freezes timeScale; gamepad focus on Invite counts as hover.
    private void LateUpdate()
    {
        bool focused = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == inviteButton.gameObject;
        float target = hovered || focused ? 1f : 0f;
        if (highlight == target)
            return;

        highlight = Mathf.MoveTowards(highlight, target, Time.unscaledDeltaTime * revealSpeed);
        Apply();
    }

    private void Apply()
    {
        background.color = Color.Lerp(backgroundColor, hoverBackgroundColor, highlight);
        rowGroup.alpha = online ? 1f : Mathf.Lerp(offlineAlpha, offlineHoverAlpha, highlight);
        inviteGroup.alpha = Mathf.Lerp(idleInviteAlpha, 1f, highlight);
    }

    // Stable per-name hue, muted so the initial stays readable.
    private static Color AvatarColor(string displayName)
    {
        uint hash = 2166136261;
        foreach (char c in displayName)
            hash = (hash ^ c) * 16777619;
        return Color.HSVToRGB((hash % 360) / 360f, 0.45f, 0.85f);
    }
}
