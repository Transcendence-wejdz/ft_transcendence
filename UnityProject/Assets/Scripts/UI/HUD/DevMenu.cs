using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    // Gold
    [SerializeField] private Button moreGoldButton;
    [SerializeField] private Button lessGoldButton;
    [SerializeField] private TMP_InputField goldInput;
    [SerializeField] private int goldStep = 100;

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
        unlimitedManaToggle.onValueChanged.AddListener((value) => LocalPlayer.GetPlayer()?.DevUnlimitedMana(value));
        unlimitedDashesToggle.onValueChanged.AddListener((value) => LocalPlayer.GetPlayer()?.DevUnlimitedDashes(value));
        noCooldownToggle.onValueChanged.AddListener((value) => LocalPlayer.GetPlayer()?.DevNoCooldown(value));
        fastAsFuckToggle.onValueChanged.AddListener((value) => LocalPlayer.GetPlayer()?.DevSpeedBoost(value));
        // Gold
        moreGoldButton.onClick.AddListener(() => GoldWallet.DevAdd(goldStep));
        lessGoldButton.onClick.AddListener(() => GoldWallet.DevAdd(-goldStep));
        goldInput.onEndEdit.AddListener(SetGold);
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
        // Gold
        GoldWallet.OnChanged -= Refresh;
    }

    private void Update()
    {
        PanelSlideIn slide;

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

    // Gold
    private void SetGold(string value)
    {
        if (int.TryParse(value, out int n))
            GoldWallet.DevSet(n);
        else
            Refresh(GoldWallet.Balance);
    }
#else
    private void Awake()
    {
        Destroy(gameObject);
    }
#endif
}
