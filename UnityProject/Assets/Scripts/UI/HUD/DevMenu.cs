using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;

// Debug panel
public class DevMenu : MonoBehaviour
{
    // Base
    [SerializeField] private GameObject panel;
    [SerializeField] private InputActionReference devAction;
    // Player
    [SerializeField] private MasterToggle godMasterToggle;  // Master toggle for all god fields
    [SerializeField] private Toggle infiniteHealthToggle;
    [SerializeField] private Toggle unlimitedManaToggle;
    [SerializeField] private Toggle unlimitedDashesToggle;
    [SerializeField] private Toggle noCooldownToggle;
    [SerializeField] private Toggle fastAsFuckToggle;
    [SerializeField] private Toggle KGBAgentToggle;
    // Execute
    [SerializeField] private TMP_Dropdown biomeDropdown;
    [SerializeField] private Button startButton;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button bossButton;
    [SerializeField] private Button forceWinButton;
    [SerializeField] private Button forceLoseButton;
    [SerializeField] private Button levelUpButton;
    // Gold
    [SerializeField] private Button moreGoldButton;
    [SerializeField] private Button lessGoldButton;
    [SerializeField] private TMP_InputField goldInput;
    [SerializeField] private int goldStep = 100;
    // Network
    [SerializeField] private Slider lagSlider;
    [SerializeField] private TMP_Text lagValue;

    public static bool IsHovered { get; private set; }

// Unity live / dev build
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Refresh(int balance)
    {
        goldInput.SetTextWithoutNotify(balance.ToString());
    }

    private void Awake()
    {
        panel.SetActive(false);
        // Player
        infiniteHealthToggle.onValueChanged.AddListener((value) => LocalPlayer.GetPlayer()?.DevInfiniteHealth(value));
        PersistToggle(infiniteHealthToggle, "health");
        unlimitedManaToggle.onValueChanged.AddListener((value) => LocalPlayer.GetPlayer()?.DevUnlimitedMana(value));
        PersistToggle(unlimitedManaToggle, "mana");
        unlimitedDashesToggle.onValueChanged.AddListener((value) => LocalPlayer.GetPlayer()?.DevUnlimitedDashes(value));
        PersistToggle(unlimitedDashesToggle, "dashes");
        noCooldownToggle.onValueChanged.AddListener((value) => LocalPlayer.GetPlayer()?.DevNoCooldown(value));
        PersistToggle(noCooldownToggle, "cooldown");
        KGBAgentToggle.onValueChanged.AddListener((value) => LocalPlayer.GetPlayer()?.DevKgbAgent(value));
        PersistToggle(KGBAgentToggle, "kgb");
        fastAsFuckToggle.onValueChanged.AddListener((value) => LocalPlayer.GetPlayer()?.DevSpeedBoost(value));
        PersistToggle(fastAsFuckToggle, "speed");
        // Execute
        startButton.onClick.AddListener(() => StartRun((RunBiome)biomeDropdown.value));
        lobbyButton.onClick.AddListener(() => GameModeLoader.LoadNow("Lobby"));
        restartButton.onClick.AddListener(() => GameModeLoader.LoadNow(SceneManager.GetActiveScene().name));
        bossButton.onClick.AddListener(() => GameModeLoader.LoadNow("Boss"));
        forceWinButton.onClick.AddListener(() => GameOverCheck.DevEndRun(true));
        forceLoseButton.onClick.AddListener(() => GameOverCheck.DevEndRun(false));
        levelUpButton.onClick.AddListener(XPManager.DevLevelUp);
        // Gold
        moreGoldButton.onClick.AddListener(() => GoldWallet.DevAdd(goldStep));
        lessGoldButton.onClick.AddListener(() => GoldWallet.DevAdd(-goldStep));
        goldInput.onEndEdit.AddListener(SetGold);
        // Network
        lagSlider.onValueChanged.AddListener(SetLag);
        PersistSlider(lagSlider, "lag");
        SetLag(lagSlider.value);
    }

    private void OnEnable()
    {
        devAction.action.Enable();
        // Gold
        GoldWallet.OnChanged += Refresh;
        Refresh(GoldWallet.Balance);
    }

    private void OnDisable()
    {
        devAction.action.Disable();
        IsHovered = false;
        // Gold
        GoldWallet.OnChanged -= Refresh;
    }

    private void Update()
    {
        PanelSlideIn slide;

        // Mouse hover click prevention
        IsHovered = panel.activeInHierarchy && Mouse.current != null
            && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)panel.transform, Mouse.current.position.ReadValue());
        if (!devAction.action.WasPressedThisFrame())
            return;
        slide = panel.GetComponent<PanelSlideIn>();
        if (slide == null)
            panel.SetActive(!panel.activeSelf);
        else if (slide.IsOpen)
            slide.Hide();
        else
            slide.Show();
    }

    // Persistence listener setups
    private void PersistToggle(Toggle toggle, string key)
    {
        toggle.SetIsOnWithoutNotify(Player.DevPrefInt(key));  // Load first
        toggle.onValueChanged.AddListener((value) => Player.SetDevPrefInt(key, value));  // Then register listener
    }
    private void PersistSlider(Slider slider, string key)
    {
        slider.SetValueWithoutNotify(Player.DevPrefFloat(key));  // Load first
        slider.onValueChanged.AddListener((value) => Player.SetDevPrefFloat(key, value));  // Then register listener
    }

    // Execute
    private void StartRun(RunBiome biome)
    {
        RunOptions options = RunOptions.Default;

        options.biome = biome;
        RunOptions.Current = options;
        GameModeLoader.LoadNow("Coop");
    }

    // Gold
    private void SetGold(string value)
    {
        if (int.TryParse(value, out int n))
            GoldWallet.DevSet(n);
        else
            Refresh(GoldWallet.Balance);
    }

    // Network
    private void SetLag(float ms)
    {
        NetworkSimulator simulator = NetworkManager.Singleton?.GetComponent<NetworkSimulator>();
        int msRounded = (int)ms;

        lagValue.text = $"{msRounded} ms";
        if (simulator != null)
            simulator.ConnectionPreset = NetworkSimulatorPreset.Create("Dev", packetDelayMs: msRounded);
    }
#else
    private void Awake()
    {
        Destroy(gameObject);
    }
#endif
}
