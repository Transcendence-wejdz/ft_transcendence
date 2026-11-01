using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One pause-menu friend row: initial avatar, name, presence line, a hover highlight, a round Invite button, and a click (or gamepad A) that opens the row's menu.
public class PauseFriendRowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISubmitHandler, IFocusVisual
{
    [SerializeField] private Image avatar;
    [SerializeField] private TMP_Text initialText;
    [SerializeField] private SiteAvatarPhoto sitePhoto;
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
    private Action<RectTransform> onClicked;
    private bool online;
    private float highlight;
    private Selectable rowSelectable;

    // The row itself is a gamepad stop, drawn by its own background instead of a color tint; created on first use since Setup can run before Awake.
    private Selectable RowSelectable
    {
        get
        {
            if (rowSelectable != null)
                return rowSelectable;
            rowSelectable = GetComponent<Selectable>();
            if (rowSelectable == null)
                rowSelectable = gameObject.AddComponent<Selectable>();
            rowSelectable.transition = Selectable.Transition.None;
            return rowSelectable;
        }
    }

    private void BindInvite(Action onInvite)
    {
        inviteButton.onClick.RemoveAllListeners();
        inviteButton.gameObject.SetActive(onInvite != null);
        if (onInvite != null)
            inviteButton.onClick.AddListener(() => onInvite());
    }

    // Offline friends stay dimmed, brightening a little on hover.
    private float RowAlpha()
    {
        if (online)
            return 1f;
        return Mathf.Lerp(offlineAlpha, offlineHoverAlpha, highlight);
    }

    private void Apply()
    {
        background.color = Color.Lerp(backgroundColor, hoverBackgroundColor, highlight);
        rowGroup.alpha = RowAlpha();
        inviteGroup.alpha = Mathf.Lerp(idleInviteAlpha, 1f, highlight);
    }

    // Gamepad focus on the row or its Invite counts as hover.
    private bool IsGamepadFocused()
    {
        GameObject selected;

        if (EventSystem.current == null || !InputModeTracker.UsingGamepad)
            return false;
        selected = EventSystem.current.currentSelectedGameObject;
        return selected != null && (selected == gameObject || selected == inviteButton.gameObject);
    }

    public void Setup(string memberId, string displayName, string status, Color statusColor, bool isOnline, Action onInvite, Action<RectTransform> onMenu = null)
    {
        onClicked = onMenu;
        RowSelectable.interactable = onMenu != null;
        online = isOnline;
        nameText.text = displayName;
        initialText.text = FriendAvatar.InitialFor(displayName);
        avatar.color = FriendAvatar.ColorFor(displayName);
        sitePhoto.Load(memberId);
        statusText.text = status;
        statusText.color = statusColor;
        BindInvite(onInvite);
        highlight = 0f;
        Apply();
    }

    public void SetInteractable(bool interactable)
    {
        inviteButton.interactable = interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }

    // The invite button handles its own clicks, so only clicks on the row itself open the menu.
    public void OnPointerClick(PointerEventData eventData)
    {
        onClicked?.Invoke((RectTransform)transform);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        onClicked?.Invoke((RectTransform)transform);
    }

    private void OnDisable()
    {
        hovered = false;
        highlight = 0f;
        Apply();
    }

    // Unscaled time since the pause menu freezes timeScale.
    private void LateUpdate()
    {
        float target = 0f;

        if (hovered || IsGamepadFocused())
            target = 1f;
        if (highlight == target)
            return;
        highlight = Mathf.MoveTowards(highlight, target, Time.unscaledDeltaTime * revealSpeed);
        Apply();
    }
}
