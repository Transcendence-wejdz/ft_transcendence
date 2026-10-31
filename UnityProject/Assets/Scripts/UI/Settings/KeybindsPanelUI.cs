using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Lists rebindable actions (Primary/Secondary keyboard+mouse slots, plus a Gamepad slot) and drives Unity's interactive rebind flow; clashes are handled in KeybindsPanelUI.Swap.cs.
public partial class KeybindsPanelUI : MonoBehaviour
{
    private enum Slot { Primary, Secondary, Gamepad }

    [System.Serializable]
    private class Row
    {
        public string actionName;
        public string displayLabel;
        // "<Mouse>" or "<Keyboard>"; secondaryDevicePrefix is empty when the action has no second slot.
        public string primaryDevicePrefix;
        public string secondaryDevicePrefix;
        public TMP_Text primaryLabel;
        public Button primaryButton;
        public TMP_Text secondaryLabel;
        public Button secondaryButton;
        public TMP_Text gamepadLabel;
        public Button gamepadButton;
        [System.NonSerialized] public Image gamepadIcon;
    }

    private const string GamepadPrefix = "<Gamepad>";
    private const string EmptySlot = "-";
    private const float MatchWaitSeconds = 0.1f;

    private static readonly Slot[] AllSlots = { Slot.Primary, Slot.Secondary, Slot.Gamepad };
    private static readonly Color ErrorColor = new Color(0.85f, 0.2f, 0.2f);

    private static KeybindsPanelUI openPanel;

    [SerializeField] private Row[] rows;
    [SerializeField] private GameObject waitingPrompt;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float gamepadIconSize = 26f;
    [SerializeField] private GameObject swapPrompt;
    [SerializeField] private TMP_Text swapPromptText;
    [SerializeField] private Button swapConfirmButton;
    [SerializeField] private Button swapCancelButton;

    private InputActionRebindingExtensions.RebindingOperation activeRebind;
    private string iconStyle;
    private string rebindOldPath;

    // True while waiting for a key or for the swap answer, so gamepad B is not taken as Back.
    public static bool IsBusy => openPanel != null && (openPanel.activeRebind != null || openPanel.pendingSwap != null);

    // True only while waiting for a key, when every press belongs to the rebind rather than to menu navigation.
    public static bool IsListening => openPanel != null && openPanel.activeRebind != null;

    private static string DevicePrefix(Row row, Slot slot)
    {
        if (slot == Slot.Primary)
            return row.primaryDevicePrefix;
        if (slot == Slot.Secondary)
            return row.secondaryDevicePrefix;
        return GamepadPrefix;
    }

    private static TMP_Text SlotLabel(Row row, Slot slot)
    {
        if (slot == Slot.Primary)
            return row.primaryLabel;
        if (slot == Slot.Secondary)
            return row.secondaryLabel;
        return row.gamepadLabel;
    }

    private static Button SlotButton(Row row, Slot slot)
    {
        if (slot == Slot.Primary)
            return row.primaryButton;
        if (slot == Slot.Secondary)
            return row.secondaryButton;
        return row.gamepadButton;
    }

    // A secondary slot on the primary's device takes that device's second binding (e.g. Tab then Escape).
    private static int FindBindingIndex(InputAction action, Row row, Slot slot)
    {
        string devicePrefix = DevicePrefix(row, slot);
        int skip = 0;
        InputBinding b;

        if (action == null || string.IsNullOrEmpty(devicePrefix))
            return -1;
        if (slot == Slot.Secondary && devicePrefix == row.primaryDevicePrefix)
            skip = 1;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite || b.path == null || !b.path.StartsWith(devicePrefix))
                continue;
            if (skip == 0)
                return i;
            skip--;
        }
        return -1;
    }

    private InputActionAsset CurrentAsset()
    {
        PlayerInput input = null;
        GameObject player = LocalPlayer.Get();

        if (player != null)
            input = player.GetComponent<PlayerInput>();
        if (input == null)
            return null;
        return input.actions;
    }

    private InputAction FindAction(InputActionAsset asset, string actionName)
    {
        if (asset == null)
            return null;
        return asset.FindAction(actionName);
    }

    private void SetStatus(string message, bool isError)
    {
        if (statusText == null)
            return;
        statusText.text = message;
        if (isError)
            statusText.color = ErrorColor;
        else
            statusText.color = Color.white;
    }

    private void BindRow(Row row)
    {
        row.primaryButton.onClick.AddListener(() => StartRebind(row, Slot.Primary));
        row.secondaryButton.onClick.AddListener(() => StartRebind(row, Slot.Secondary));
        row.gamepadButton.onClick.AddListener(() => StartRebind(row, Slot.Gamepad));
        row.gamepadIcon = CreateGamepadIcon(row.gamepadButton.transform);
    }

    // Wired to the waiting-for-key prompt's own Cancel button, since Escape is avoided project-wide (WebGL fullscreen).
    public void CancelActiveRebind()
    {
        activeRebind?.Cancel();
    }

    private void Awake()
    {
        foreach (Row row in rows)
            BindRow(row);
        swapConfirmButton.onClick.AddListener(ConfirmSwap);
        swapCancelButton.onClick.AddListener(CancelSwap);
        LinkHorizontally(swapConfirmButton, swapCancelButton);
    }

    private void OnEnable()
    {
        openPanel = this;
        SetStatus(string.Empty, false);
        RefreshAllLabels();
    }

    private void OnDisable()
    {
        activeRebind?.Cancel();
        CancelSwap();
        if (openPanel == this)
            openPanel = null;
    }

    // Redraws the gamepad icons when the player switches to a pad of another brand.
    private void Update()
    {
        if (activeRebind == null && GamepadIcons.CurrentStyle() != iconStyle)
            RefreshAllLabels();
    }
}
