using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Debug panel
public class DevMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private InputActionReference devAction;
    [SerializeField] private Button moreGoldButton;
    [SerializeField] private Button lessGoldButton;
    [SerializeField] private TMP_InputField goldInput;
    [SerializeField] private int step = 100;

// Unity live / dev build
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Refresh(int balance)
    {
        goldInput.SetTextWithoutNotify(balance.ToString());
    }

    private void Awake()
    {
        panel.SetActive(false);
        // Gold
        moreGoldButton.onClick.AddListener(() => GoldWallet.DevAdd(step));
        lessGoldButton.onClick.AddListener(() => GoldWallet.DevAdd(-step));
        goldInput.onEndEdit.AddListener(SetGold);
    }

    private void OnEnable()
    {
        devAction.action.Enable();
        GoldWallet.OnChanged += Refresh;
        Refresh(GoldWallet.Balance);
    }

    private void OnDisable()
    {
        devAction.action.Disable();
        GoldWallet.OnChanged -= Refresh;
    }

    private void Update()
    {
        if (devAction.action.WasPressedThisFrame())
            panel.SetActive(!panel.activeSelf);
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
