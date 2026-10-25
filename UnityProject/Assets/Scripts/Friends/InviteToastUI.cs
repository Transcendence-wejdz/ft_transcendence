using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Shows a friend's invite with Join/Decline on its own top canvas (clickable over the pause menu); stays on an always-active object.
public class InviteToastUI : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private CanvasGroup contentGroup;
    [SerializeField] private TMP_Text senderText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image avatar;
    [SerializeField] private TMP_Text avatarInitial;
    [SerializeField] private Image timerFill;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text joinLabel;
    [SerializeField] private Button dismissButton;
    [SerializeField] private NetworkBootstrap bootstrap;
    [SerializeField] private float autoHideSeconds = 30f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private float slideDistance = 30f;
    [SerializeField] private Color messageColor = new Color(0.7f, 0.69f, 0.66f, 1f);
    [SerializeField] private Color warningColor = new Color(0.95f, 0.75f, 0.3f, 1f);

    private string pendingCode;
    private string pendingSenderId;
    private bool waitingForCode;
    private Coroutine routine;
    private Vector2 restPosition;

    private void Awake()
    {
        restPosition = ((RectTransform)content.transform).anchoredPosition;
        content.SetActive(false);
        joinButton.onClick.AddListener(Join);
        dismissButton.onClick.AddListener(Hide);
    }

    private void OnEnable()
    {
        FriendsManager.OnInviteReceived += Show;
    }

    private void OnDisable()
    {
        FriendsManager.OnInviteReceived -= Show;
    }

    // Connects up front so invites arrive before the panel is ever opened.
    private async void Start()
    {
        try
        {
            await FriendsManager.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"InviteToastUI: friends service unavailable, invites disabled - {e.Message}");
        }
    }

    // A follow-up only completes the open popup from the same friend with its code; it never reopens a dismissed or expired one.
    private void Show(FriendInvite invite)
    {
        if (invite.FollowUp)
        {
            if (content.activeSelf && invite.SenderId == pendingSenderId && !string.IsNullOrEmpty(invite.SessionCode))
            {
                pendingCode = invite.SessionCode;
                if (waitingForCode)
                    Join();
            }
            return;
        }

        bool alreadyInSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        string sender = FriendsManager.ShortName(invite.SenderName);

        pendingCode = invite.SessionCode;
        pendingSenderId = invite.SenderId;
        waitingForCode = false;
        joinLabel.text = "Join";
        joinButton.interactable = true;
        senderText.text = sender;
        avatar.color = FriendAvatar.ColorFor(sender);
        avatarInitial.text = FriendAvatar.InitialFor(sender);
        messageText.text = alreadyInSession ? "Joining leaves your current game" : "invited you to play";
        messageText.color = alreadyInSession ? warningColor : messageColor;

        content.SetActive(true);
        Restart(ShowRoutine());
    }

    private void Hide()
    {
        if (content.activeSelf)
            Restart(HideRoutine());
    }

    private void Restart(IEnumerator next)
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(next);
    }

    // Unscaled time throughout, since the pause menu may have frozen timeScale.
    private IEnumerator ShowRoutine()
    {
        yield return Animate(contentGroup.alpha, 1f);
        float shownAt = Time.unscaledTime;
        while (Time.unscaledTime - shownAt < autoHideSeconds)
        {
            timerFill.fillAmount = 1f - (Time.unscaledTime - shownAt) / autoHideSeconds;
            yield return null;
        }
        yield return HideRoutine();
    }

    private IEnumerator HideRoutine()
    {
        yield return Animate(contentGroup.alpha, 0f);
        content.SetActive(false);
        routine = null;
    }

    // Slides down from above while fading in, and back up while fading out; buttons only take clicks once fully shown.
    private IEnumerator Animate(float from, float to)
    {
        contentGroup.interactable = false;
        contentGroup.blocksRaycasts = to > 0f;
        RectTransform rect = (RectTransform)content.transform;
        float t = 0f;
        while (t < animationDuration)
        {
            t += Time.unscaledDeltaTime;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / animationDuration), 3f);
            float alpha = Mathf.Lerp(from, to, eased);
            contentGroup.alpha = alpha;
            rect.anchoredPosition = restPosition + Vector2.up * slideDistance * (1f - alpha);
            yield return null;
        }
        contentGroup.alpha = to;
        rect.anchoredPosition = restPosition + Vector2.up * slideDistance * (1f - to);
        contentGroup.interactable = to > 0f;
        if (to > 0f)
            timerFill.fillAmount = 1f;
    }

    // Leaves any session first so the invite always wins; without a NetworkBootstrap here (e.g. solo in a mode scene) it joins from a fresh Lobby. Waits for the code if the host's session is still being created.
    private async void Join()
    {
        if (string.IsNullOrEmpty(pendingCode))
        {
            waitingForCode = true;
            joinButton.interactable = false;
            joinLabel.text = "...";
            messageText.text = "Waiting for the host...";
            messageText.color = messageColor;
            return;
        }

        string code = pendingCode;
        waitingForCode = false;
        joinButton.interactable = false;
        joinLabel.text = "...";

        var check = await SessionJoin.CheckAsync(code);
        if (this == null)
            return;
        if (!check.ok)
        {
            messageText.text = check.message;
            messageText.color = warningColor;
            joinLabel.text = "Join";
            joinButton.interactable = true;
            return;
        }

        Hide();
        await SessionJoin.JoinAsync(code, bootstrap, alreadyChecked: true);
    }
}
