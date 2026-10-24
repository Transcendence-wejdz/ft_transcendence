using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Lists rebindable actions (Primary/Secondary keyboard+mouse slots, plus a Gamepad slot) and drives Unity's interactive rebind flow.
public class KeybindsPanelUI : MonoBehaviour
{
    private enum Slot { Primary, Secondary, Gamepad }
    private static readonly Slot[] AllSlots = { Slot.Primary, Slot.Secondary, Slot.Gamepad };

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
    private PendingSwap pendingSwap;
    private static KeybindsPanelUI openPanel;

    // True while waiting for a key or for the swap answer, so gamepad B is not taken as Back.
    public static bool IsBusy => openPanel != null && (openPanel.activeRebind != null || openPanel.pendingSwap != null);

    private class PendingSwap
    {
        public InputAction action;
        public int bindingIndex;
        public string oldPath;
        public InputAction otherAction;
        public int otherIndex;
        public Button slotButton;
    }

    private void Awake()
    {
        foreach (Row row in rows)
        {
            Row captured = row;
            captured.primaryButton.onClick.AddListener(() => StartRebind(captured, Slot.Primary));
            captured.secondaryButton.onClick.AddListener(() => StartRebind(captured, Slot.Secondary));
            captured.gamepadButton.onClick.AddListener(() => StartRebind(captured, Slot.Gamepad));
            captured.gamepadIcon = CreateGamepadIcon(captured.gamepadButton.transform);
        }
        swapConfirmButton.onClick.AddListener(ConfirmSwap);
        swapCancelButton.onClick.AddListener(CancelSwap);
        LinkHorizontally(swapConfirmButton, swapCancelButton);
    }

    // Keeps gamepad navigation inside the two-button swap prompt.
    private static void LinkHorizontally(Button left, Button right)
    {
        left.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = right };
        right.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = left };
    }

    // Redraws the gamepad icons when the player switches to a pad of another brand.
    private void Update()
    {
        if (activeRebind == null && GamepadIcons.CurrentStyle() != iconStyle)
            RefreshAllLabels();
    }

    private Image CreateGamepadIcon(Transform button)
    {
        GameObject go = new GameObject("GamepadIcon", typeof(RectTransform), typeof(Image));
        go.layer = button.gameObject.layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(button, false);
        rt.sizeDelta = new Vector2(gamepadIconSize, gamepadIconSize);

        Image image = go.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
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

    // Wired to the waiting-for-key prompt's own Cancel button, since Escape is avoided project-wide (WebGL fullscreen).
    public void CancelActiveRebind()
    {
        activeRebind?.Cancel();
    }

    private InputActionAsset CurrentAsset()
    {
        GameObject player = LocalPlayer.Get();
        PlayerInput input = player != null ? player.GetComponent<PlayerInput>() : null;
        return input != null ? input.actions : null;
    }

    private static string DevicePrefix(Row row, Slot slot)
    {
        if (slot == Slot.Primary)
            return row.primaryDevicePrefix;
        if (slot == Slot.Secondary)
            return row.secondaryDevicePrefix;
        return "<Gamepad>";
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

    private void StartRebind(Row row, Slot slot)
    {
        if (activeRebind != null || pendingSwap != null)
            return;

        string prefix = DevicePrefix(row, slot);
        if (string.IsNullOrEmpty(prefix))
            return;

        InputActionAsset asset = CurrentAsset();
        InputAction action = asset != null ? asset.FindAction(row.actionName) : null;
        int bindingIndex = FindBindingIndex(action, prefix);
        if (action == null || bindingIndex < 0)
        {
            SetStatus("Can't rebind right now.", true);
            return;
        }

        rebindOldPath = action.bindings[bindingIndex].effectivePath;
        TMP_Text label = SlotLabel(row, slot);
        action.Disable();
        label.text = "...";
        if (slot == Slot.Gamepad)
            row.gamepadIcon.enabled = false;
        if (waitingPrompt != null)
        {
            waitingPrompt.SetActive(true);
            if (slot != Slot.Gamepad)
                GamepadUINavigator.Focus(waitingPrompt.GetComponentInChildren<Button>());
        }

        InputActionRebindingExtensions.RebindingOperation rebind = action.PerformInteractiveRebinding(bindingIndex);
        rebind = prefix == "<Gamepad>"
            ? rebind.WithControlsExcluding("Mouse").WithControlsExcluding("Keyboard")
            : rebind.WithControlsExcluding("Gamepad");

        activeRebind = rebind
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op => FinishRebind(row, slot, action, bindingIndex, cancelled: false))
            .OnCancel(op => FinishRebind(row, slot, action, bindingIndex, cancelled: true))
            .Start();
    }

    private void FinishRebind(Row row, Slot slot, InputAction action, int bindingIndex, bool cancelled)
    {
        activeRebind?.Dispose();
        activeRebind = null;
        action.Enable();
        if (waitingPrompt != null)
            waitingPrompt.SetActive(false);

        if (!cancelled)
        {
            InputAction otherAction;
            int otherIndex;
            Row conflict = FindConflict(action.actionMap.asset, action, bindingIndex, out otherAction, out otherIndex);
            if (conflict != null)
            {
                pendingSwap = new PendingSwap { action = action, bindingIndex = bindingIndex, oldPath = rebindOldPath, otherAction = otherAction, otherIndex = otherIndex, slotButton = SlotButton(row, slot) };
                swapPromptText.text = "Already used by " + conflict.displayLabel + ".\nSwap them?";
                swapPrompt.SetActive(true);
                GamepadUINavigator.Focus(swapConfirmButton);
            }
            else
            {
                KeybindOverrides.Save(action.actionMap.asset);
                SetStatus(string.Empty, false);
            }
        }

        if (pendingSwap == null)
            GamepadUINavigator.Focus(SlotButton(row, slot));

        RefreshAllLabels();
    }

    // Gives the conflicting slot the key the rebound slot had before.
    private void ConfirmSwap()
    {
        if (pendingSwap == null)
            return;

        SetPath(pendingSwap.otherAction, pendingSwap.otherIndex, pendingSwap.oldPath);
        KeybindOverrides.Save(pendingSwap.action.actionMap.asset);
        CloseSwapPrompt();
    }

    // Reverts the rebound slot to its previous key.
    private void CancelSwap()
    {
        if (pendingSwap == null)
            return;

        SetPath(pendingSwap.action, pendingSwap.bindingIndex, pendingSwap.oldPath);
        CloseSwapPrompt();
    }

    private void CloseSwapPrompt()
    {
        Button slotButton = pendingSwap.slotButton;
        pendingSwap = null;
        swapPrompt.SetActive(false);
        GamepadUINavigator.Focus(slotButton);
        SetStatus(string.Empty, false);
        RefreshAllLabels();
    }

    // Drops the override instead of storing one when the path is the action asset's default.
    private static void SetPath(InputAction action, int bindingIndex, string path)
    {
        if (path == action.bindings[bindingIndex].path)
            action.RemoveBindingOverride(bindingIndex);
        else
            action.ApplyBindingOverride(bindingIndex, path);
    }

    // Effective path clash against any other rebindable slot, across every row, within the same asset.
    private Row FindConflict(InputActionAsset asset, InputAction changedAction, int changedBindingIndex, out InputAction conflictAction, out int conflictIndex)
    {
        conflictAction = null;
        conflictIndex = -1;
        string changedPath = changedAction.bindings[changedBindingIndex].effectivePath;

        foreach (Row row in rows)
        {
            InputAction other = asset.FindAction(row.actionName);
            if (other == null)
                continue;

            foreach (Slot slot in AllSlots)
            {
                string prefix = DevicePrefix(row, slot);
                if (string.IsNullOrEmpty(prefix))
                    continue;

                int idx = FindBindingIndex(other, prefix);
                if (idx < 0)
                    continue;
                if (other == changedAction && idx == changedBindingIndex)
                    continue;

                if (other.bindings[idx].effectivePath == changedPath)
                {
                    conflictAction = other;
                    conflictIndex = idx;
                    return row;
                }
            }
        }
        return null;
    }

    private static int FindBindingIndex(InputAction action, string devicePrefix)
    {
        if (action == null || string.IsNullOrEmpty(devicePrefix))
            return -1;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (!b.isComposite && !b.isPartOfComposite && b.path != null && b.path.StartsWith(devicePrefix))
                return i;
        }
        return -1;
    }

    // Falls back to the binding path's last segment for control keys Unity can't render as text.
    private static string DisplayName(InputAction action, int bindingIndex)
    {
        string display = action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        if (!string.IsNullOrEmpty(display) && display.Trim().Length > 0)
            return display;

        string path = action.bindings[bindingIndex].path;
        int slash = path.LastIndexOf('/');
        string keyName = slash >= 0 ? path.Substring(slash + 1) : path;
        return keyName.Length > 0 ? char.ToUpper(keyName[0]) + keyName.Substring(1) : "?";
    }

    private void RefreshAllLabels()
    {
        iconStyle = GamepadIcons.CurrentStyle();
        InputActionAsset asset = CurrentAsset();
        foreach (Row row in rows)
        {
            InputAction action = asset != null ? asset.FindAction(row.actionName) : null;
            foreach (Slot slot in AllSlots)
            {
                string prefix = DevicePrefix(row, slot);
                TMP_Text label = SlotLabel(row, slot);
                Button button = SlotButton(row, slot);

                if (string.IsNullOrEmpty(prefix))
                {
                    label.text = "-";
                    button.interactable = false;
                    continue;
                }

                int bindingIndex = FindBindingIndex(action, prefix);
                button.interactable = bindingIndex >= 0;
                label.text = bindingIndex >= 0 ? DisplayName(action, bindingIndex) : "-";

                if (slot == Slot.Gamepad)
                    ShowGamepadIcon(row, bindingIndex >= 0 ? GamepadIcons.ForPath(action.bindings[bindingIndex].effectivePath) : null);
            }
        }
    }

    // Icon replaces the text label, which stays as the fallback for controls the sheet has no sprite for.
    private static void ShowGamepadIcon(Row row, Sprite sprite)
    {
        row.gamepadIcon.sprite = sprite;
        row.gamepadIcon.enabled = sprite != null;
        if (sprite != null)
            row.gamepadLabel.text = string.Empty;
    }

    private void SetStatus(string message, bool isError)
    {
        if (statusText == null)
            return;

        statusText.text = message;
        statusText.color = isError ? new Color(0.85f, 0.2f, 0.2f) : Color.white;
    }
}
