using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Opens the ready-check panel on a proposal and lets the local player toggle ready.
public partial class ReadyCheckUI : MonoBehaviour
{
    // Per-mode display metadata keyed by scene name; no TeamNames means no team picker.
    private readonly struct ModeInfo
    {
        public readonly string DisplayName;
        public readonly string[] TeamNames;

        public ModeInfo(string displayName, string[] teamNames = null)
        {
            DisplayName = displayName;
            TeamNames = teamNames;
        }
    }

    private static readonly Dictionary<string, ModeInfo> ModeInfos = new Dictionary<string, ModeInfo>
    {
        { "Coop", new ModeInfo("4-Player Co-op") },
        { "Boss", new ModeInfo("Boss Fight") },
    };

    [SerializeField] private MenuPanel menuPanel;
    [SerializeField] private RectTransform rowTemplate;
    [SerializeField] private TextMeshProUGUI modeNameText;
    [SerializeField] private TextMeshProUGUI readyButtonLabel;

    [SerializeField] private RectTransform teamButtonContainer;
    [SerializeField] private RectTransform teamButtonTemplate;

    private bool isOpen;
    private ModeReadyCheck subscribedInstance;
    private readonly Dictionary<Player, ReadyCheckEntryUI> rows = new Dictionary<Player, ReadyCheckEntryUI>();
    private readonly List<RectTransform> teamButtons = new List<RectTransform>();
    private string[] currentTeamNames;

    private void UpdateReadyButtonLabel()
    {
        Player player = LocalPlayer.GetPlayer();

        if (readyButtonLabel == null)
            return;
        if (player != null && player.IsReady)
            readyButtonLabel.text = "Cancel";
        else
            readyButtonLabel.text = "Ready";
    }

    private void ShowModeName(string sceneName, ModeInfo modeInfo)
    {
        if (modeNameText == null)
            return;
        if (string.IsNullOrEmpty(modeInfo.DisplayName))
            modeNameText.text = sceneName;
        else
            modeNameText.text = modeInfo.DisplayName;
    }

    // Closing the panel early cancels the check server-side, or proposing that mode again would do nothing.
    private void HandlePanelClosed()
    {
        if (!isOpen)
            return;
        isOpen = false;
        ModeReadyCheck.Instance?.RequestCancelReadyCheck();
    }

    private void OpenFor(string sceneName)
    {
        ModeInfo modeInfo;

        ModeInfos.TryGetValue(sceneName, out modeInfo);
        ShowModeName(sceneName, modeInfo);
        currentTeamNames = modeInfo.TeamNames;
        RebuildRows();
        RebuildTeamButtons(modeInfo.TeamNames);
        UpdateReadyButtonLabel();
        menuPanel.Open();
    }

    private void HandlePendingSceneChanged(string sceneName)
    {
        isOpen = !string.IsNullOrEmpty(sceneName);
        if (isOpen)
        {
            OpenFor(sceneName);
            return;
        }
        currentTeamNames = null;
        menuPanel.Close();
    }

    private void SubscribeTo(ModeReadyCheck instance)
    {
        if (subscribedInstance != null)
            subscribedInstance.OnPendingSceneChanged -= HandlePendingSceneChanged;
        subscribedInstance = instance;
        if (subscribedInstance != null)
            subscribedInstance.OnPendingSceneChanged += HandlePendingSceneChanged;
    }

    public void ToggleReady()
    {
        Player player = LocalPlayer.GetPlayer();

        if (player == null)
            return;
        player.SetReady(!player.IsReady);
        UpdateReadyButtonLabel();
    }

    private void Awake()
    {
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
        if (teamButtonTemplate != null)
            teamButtonTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        Player.OnPlayerRegistered += HandlePlayerRegistered;
        Player.OnPlayerUnregistered += HandlePlayerUnregistered;
        if (menuPanel != null)
            menuPanel.OnClosed += HandlePanelClosed;
    }

    private void OnDisable()
    {
        SubscribeTo(null);
        Player.OnPlayerRegistered -= HandlePlayerRegistered;
        Player.OnPlayerUnregistered -= HandlePlayerUnregistered;
        if (menuPanel != null)
            menuPanel.OnClosed -= HandlePanelClosed;
        ClearRows();
        RebuildTeamButtons(null);
    }

    // Polls until the dynamically spawned ModeReadyCheck exists.
    private void Update()
    {
        if (subscribedInstance != ModeReadyCheck.Instance)
            SubscribeTo(ModeReadyCheck.Instance);
    }
}
