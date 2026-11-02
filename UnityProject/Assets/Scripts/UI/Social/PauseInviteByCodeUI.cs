using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pause-menu invite block: shows and copies our session code (or hosts to get one), joins a typed code, and sends a friend request by name.
public partial class PauseInviteByCodeUI : MonoBehaviour
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

    // Session codes are letters and digits; typing is forced to capitals so what you see is exactly the code.
    private static char UppercaseCodeChar(string text, int index, char added)
    {
        if (char.IsLetterOrDigit(added))
            return char.ToUpperInvariant(added);
        return '\0';
    }

    private static string CodeLabel(string code, bool canHost)
    {
        if (!string.IsNullOrEmpty(code))
            return code;
        if (ActiveSession.Current != null)
            return "In game";
        if (canHost)
            return "No session";
        return "Solo";
    }

    private void SetStatus(string message, bool isError)
    {
        statusText.text = message;
        if (isError)
            statusText.color = errorColor;
        else
            statusText.color = infoColor;
    }

    // The session is locked outside the Lobby, so its code is no use to anyone there.
    private static string ShareableCode()
    {
        if (!FriendInviteFlow.CanInvite)
            return null;
        return FriendsManager.CurrentSessionCode;
    }

    private void RefreshCode(bool force)
    {
        string code = ShareableCode();
        bool canHost = ActiveSession.Current == null && bootstrap != null;

        if (!force && code == shownCode && canHost == shownCanHost)
            return;
        shownCode = code;
        shownCanHost = canHost;
        codeText.text = CodeLabel(code, canHost);
        copyButton.gameObject.SetActive(!string.IsNullOrEmpty(code));
        hostButton.gameObject.SetActive(canHost);
    }

    private void CopyCode()
    {
        if (string.IsNullOrEmpty(shownCode))
            return;
        GUIUtility.systemCopyBuffer = shownCode;
        SetStatus("Code copied.", isError: false);
    }

    private void ShowHostResult(bool ok)
    {
        if (ok)
            SetStatus("Share your code to invite.", isError: false);
        else
            SetStatus("Couldn't start hosting, try again.", isError: true);
    }

    // Hosts without the loading screen, like an invite, so the player keeps their place while the code is created.
    private async void Host()
    {
        bool ok;

        if (busy || bootstrap == null)
            return;
        busy = true;
        hostButton.interactable = false;
        SetStatus("Creating your game...", isError: false);
        ok = await bootstrap.HostGameAsync(showLoadingScreen: false);
        busy = false;
        if (this == null)
            return;
        hostButton.interactable = true;
        ShowHostResult(ok);
    }

    private async System.Threading.Tasks.Task SendFriendRequestAsync(string friendName)
    {
        try
        {
            await FriendsManager.InitializeAsync();
            await FriendsManager.AddByNameAsync(friendName);
            if (this == null)
                return;
            addInput.text = string.Empty;
            SetStatus("Request sent.", isError: false);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PauseInviteByCodeUI: add friend failed - {e.Message}");
            if (this != null)
                SetStatus("Couldn't add that player, check the name.", isError: true);
        }
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
        await SendFriendRequestAsync(friendName);
        if (this != null)
            addButton.interactable = true;
    }

    private void AddFriendFromInput(string text)
    {
        AddFriend();
    }

    private void Awake()
    {
        copyButton.onClick.AddListener(CopyCode);
        hostButton.onClick.AddListener(Host);
        joinButton.onClick.AddListener(Join);
        addButton.onClick.AddListener(AddFriend);
        joinInput.onSubmit.AddListener(JoinFromInput);
        joinInput.onValidateInput = UppercaseCodeChar;
        addInput.onSubmit.AddListener(AddFriendFromInput);
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
}
