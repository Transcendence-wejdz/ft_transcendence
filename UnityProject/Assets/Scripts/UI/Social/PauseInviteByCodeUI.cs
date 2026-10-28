using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pause-menu invite block: shows and copies our session code (or hosts to get one), joins a typed code, and sends a friend request by name.
public class PauseInviteByCodeUI : MonoBehaviour
{
    [SerializeField] private TMP_Text codeText;
    [SerializeField] private Button copyButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private TMP_InputField joinInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField addInput;
    [SerializeField] private Button addButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Color errorColor = new Color(0.9f, 0.4f, 0.35f, 1f);
    [SerializeField] private Color infoColor = new Color(0.7f, 0.69f, 0.66f, 1f);

    private NetworkBootstrap bootstrap;
    private string shownCode;
    private bool shownCanHost;
    private bool busy;

    private void Awake()
    {
        copyButton.onClick.AddListener(CopyCode);
        hostButton.onClick.AddListener(Host);
        joinButton.onClick.AddListener(Join);
        addButton.onClick.AddListener(AddFriend);
        joinInput.onSubmit.AddListener(_ => Join());
        joinInput.onValidateInput = UppercaseCodeChar;
        addInput.onSubmit.AddListener(_ => AddFriend());
    }

    // Session codes are letters and digits; typing is forced to capitals so what you see is exactly the code.
    private static char UppercaseCodeChar(string text, int index, char added)
    {
        return char.IsLetterOrDigit(added) ? char.ToUpperInvariant(added) : '\0';
    }

    private void OnEnable()
    {
        bootstrap = FindAnyObjectByType<NetworkBootstrap>(FindObjectsInactive.Include);
        RefreshCode(force: true);
    }

    // The code appears once hosting finishes and disappears when the session ends, with no event to tell us.
    private void LateUpdate()
    {
        RefreshCode(force: false);
    }

    private void RefreshCode(bool force)
    {
        string code = FriendsManager.CurrentSessionCode;
        bool canHost = string.IsNullOrEmpty(code) && bootstrap != null;
        if (!force && code == shownCode && canHost == shownCanHost)
            return;

        shownCode = code;
        shownCanHost = canHost;
        bool hasCode = !string.IsNullOrEmpty(code);
        codeText.text = hasCode ? code : (canHost ? "No session" : "Solo");
        copyButton.gameObject.SetActive(hasCode);
        hostButton.gameObject.SetActive(canHost);
    }

    private void CopyCode()
    {
        if (string.IsNullOrEmpty(shownCode))
            return;

        GUIUtility.systemCopyBuffer = shownCode;
        SetStatus("Code copied.", isError: false);
    }

    // Hosts without the loading screen, like an invite, so the player keeps their place while the code is created.
    private async void Host()
    {
        if (busy || bootstrap == null)
            return;

        busy = true;
        hostButton.interactable = false;
        SetStatus("Creating your game...", isError: false);
        bool ok = await bootstrap.HostGameAsync(showLoadingScreen: false);
        busy = false;
        if (this == null)
            return;

        hostButton.interactable = true;
        SetStatus(ok ? "Share your code to invite." : "Couldn't start hosting, try again.", isError: !ok);
    }

    // Checks the code before closing the pause menu, so a bad code costs nothing; then joining reloads or repopulates the scene under it.
    private async void Join()
    {
        string code = joinInput.text.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            SetStatus("Enter a session code.", isError: true);
            return;
        }
        if (code == shownCode)
        {
            SetStatus("That's your own session.", isError: true);
            return;
        }

        if (busy)
            return;
        busy = true;
        joinButton.interactable = false;
        SetStatus("Looking for that game...", isError: false);
        var check = await SessionJoin.CheckAsync(code);
        busy = false;
        if (this == null)
            return;
        joinButton.interactable = true;
        if (!check.ok)
        {
            SetStatus(check.message, isError: true);
            return;
        }

        joinInput.text = string.Empty;
        SetStatus(string.Empty, isError: false);
        FindAnyObjectByType<PauseManager>()?.Resume();
        var result = await SessionJoin.JoinAsync(code, alreadyChecked: true);
        if (this != null && !result.ok)
            SetStatus(result.message, isError: true);
    }

    private async void AddFriend()
    {
        string friendName = addInput.text.Trim();
        if (string.IsNullOrEmpty(friendName))
        {
            SetStatus("Enter a player name (Name#1234).", isError: true);
            return;
        }

        addButton.interactable = false;
        try
        {
            await FriendsManager.InitializeAsync();
            await FriendsManager.AddByNameAsync(friendName);
            if (this != null)
            {
                addInput.text = string.Empty;
                SetStatus("Request sent.", isError: false);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PauseInviteByCodeUI: add friend failed - {e.Message}");
            if (this != null)
                SetStatus("Couldn't add that player, check the name.", isError: true);
        }
        if (this != null)
            addButton.interactable = true;
    }

    private void SetStatus(string message, bool isError)
    {
        statusText.text = message;
        statusText.color = isError ? errorColor : infoColor;
    }
}
