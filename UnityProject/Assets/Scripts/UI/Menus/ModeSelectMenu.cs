using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mode select: modes in the sidebar, the chosen one's description and options on the right, started by the Play button.
public class ModeSelectMenu : MonoBehaviour
{
    [System.Serializable]
    private class ModeEntry
    {
        public Button tab;
        public string sceneName;
        public string title;
        [TextArea] public string description;
        public bool offersFriendlyFire;
        [Tooltip("Party size this mode can start with; the tab is locked outside of it.")]
        public int minPlayers = 1;
        public int maxPlayers = 4;
    }

    private const string FriendlyFireOffNote = "Score x0.75.";
    private const float LockedLabelAlpha = 0.4f;

    [SerializeField] private GameModeLoader loader;
    [SerializeField] private ModeEntry[] modes;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("In RunBiome order: Random, Forest, Plains, Mountain.")]
    [SerializeField] private Button[] biomeButtons;
    [SerializeField] private GameObject friendlyFireSection;
    [SerializeField] private Button friendlyFireOnButton;
    [SerializeField] private Button friendlyFireOffButton;
    [SerializeField] private TMP_Text friendlyFireNote;
    [SerializeField] private Button playButton;

    private ModeEntry selected;
    private RunOptions options = RunOptions.Default;
    private int shownPartySize = -1;

    private static int PartySize => Mathf.Max(1, Player.AllActiveInstances.Count);

    private static bool IsLocked(ModeEntry mode)
    {
        if (ComingSoonBanner.IsOn(mode.tab))
            return true;
        return PartySize < mode.minPlayers || PartySize > mode.maxPlayers;
    }

    // A dimmed label tells why the tab doesn't react.
    private static void ShowLock(ModeEntry mode)
    {
        bool locked = IsLocked(mode);
        TMP_Text label = mode.tab.GetComponentInChildren<TMP_Text>();

        mode.tab.interactable = !locked;
        label.alpha = 1f;
        if (locked)
            label.alpha = LockedLabelAlpha;
    }

    private static void Pin(Button button, bool active)
    {
        HoverColorWipe wipe = button.GetComponent<HoverColorWipe>();

        if (wipe != null)
            wipe.SetPinned(active);
    }

    private void Refresh()
    {
        foreach (ModeEntry mode in modes)
            Pin(mode.tab, mode == selected);
        for (int i = 0; i < biomeButtons.Length; i++)
            Pin(biomeButtons[i], i == (int)options.biome);
        Pin(friendlyFireOnButton, options.friendlyFire);
        Pin(friendlyFireOffButton, !options.friendlyFire);
        friendlyFireNote.text = string.Empty;
        if (!options.friendlyFire)
            friendlyFireNote.text = FriendlyFireOffNote;
        titleText.text = selected.title;
        descriptionText.text = selected.description;
        friendlyFireSection.SetActive(selected.offersFriendlyFire);
    }

    private void Select(ModeEntry mode)
    {
        selected = mode;
        Refresh();
    }

    private void SetBiome(RunBiome biome)
    {
        options.biome = biome;
        Refresh();
    }

    private void SetFriendlyFire(bool enabled)
    {
        options.friendlyFire = enabled;
        Refresh();
    }

    // A mode without the friendly fire option keeps the default rule, whatever was picked on another mode.
    private void Play()
    {
        RunOptions chosen = options;

        if (!selected.offersFriendlyFire)
            chosen.friendlyFire = true;
        loader.LoadScene(selected.sceneName, chosen);
    }

    // Separate so each listener captures its own value.
    private void BindMode(ModeEntry mode)
    {
        mode.tab.onClick.AddListener(() => Select(mode));
    }

    private void BindBiome(Button button, RunBiome biome)
    {
        button.onClick.AddListener(() => SetBiome(biome));
    }

    void Awake()
    {
        foreach (ModeEntry mode in modes)
            BindMode(mode);
        for (int i = 0; i < biomeButtons.Length; i++)
            BindBiome(biomeButtons[i], (RunBiome)i);
        friendlyFireOnButton.onClick.AddListener(() => SetFriendlyFire(true));
        friendlyFireOffButton.onClick.AddListener(() => SetFriendlyFire(false));
        playButton.onClick.AddListener(Play);
    }

    // Opens on the first mode this party can start; the same pick applies when the party changes under a locked mode.
    private void ApplyPartySize()
    {
        ModeEntry firstOpen = null;

        shownPartySize = PartySize;
        foreach (ModeEntry mode in modes)
        {
            ShowLock(mode);
            if (firstOpen == null && !IsLocked(mode))
                firstOpen = mode;
        }
        if (selected == null || IsLocked(selected))
            selected = firstOpen;
        Refresh();
    }

    void OnEnable()
    {
        ApplyPartySize();
    }

    // Polled while the menu is open: a friend can join or leave at any time.
    void Update()
    {
        if (PartySize != shownPartySize)
            ApplyPartySize();
    }
}
