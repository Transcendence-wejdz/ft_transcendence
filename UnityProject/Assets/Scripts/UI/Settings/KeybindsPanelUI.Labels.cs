using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Slot labels: the current key or pad icon of every row, refreshed after each rebind.
public partial class KeybindsPanelUI
{
    // Falls back to the binding path's last segment for control keys Unity can't render as text.
    private static string DisplayName(InputAction action, int bindingIndex)
    {
        string display = action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);

        if (!string.IsNullOrEmpty(display) && display.Trim().Length > 0)
            return display;
        return KeybindOverrides.KeyNameFromPath(action.bindings[bindingIndex].path);
    }

    // Icon replaces the text label, which stays as the fallback for controls the sheet has no sprite for.
    private static void ShowGamepadIcon(Row row, Sprite sprite)
    {
        row.gamepadIcon.sprite = sprite;
        row.gamepadIcon.enabled = sprite != null;
        if (sprite != null)
            row.gamepadLabel.text = string.Empty;
    }

    private Image CreateGamepadIcon(Transform button)
    {
        GameObject go = new GameObject("GamepadIcon", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        Image image = go.GetComponent<Image>();

        go.layer = button.gameObject.layer;
        rt.SetParent(button, false);
        rt.sizeDelta = new Vector2(gamepadIconSize, gamepadIconSize);
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    // A slot without a device prefix is unused: greyed out and labelled "-".
    private void RefreshSlot(Row row, Slot slot, InputAction action)
    {
        string prefix = DevicePrefix(row, slot);
        TMP_Text label = SlotLabel(row, slot);
        Button button = SlotButton(row, slot);
        int bindingIndex;
        Sprite icon = null;

        if (string.IsNullOrEmpty(prefix))
        {
            label.text = EmptySlot;
            button.interactable = false;
            return;
        }
        bindingIndex = FindBindingIndex(action, row, slot);
        button.interactable = bindingIndex >= 0;
        label.text = EmptySlot;
        if (bindingIndex >= 0)
            label.text = DisplayName(action, bindingIndex);
        if (slot != Slot.Gamepad)
            return;
        if (bindingIndex >= 0)
            icon = GamepadIcons.ForPath(action.bindings[bindingIndex].effectivePath);
        ShowGamepadIcon(row, icon);
    }

    private void RefreshAllLabels()
    {
        InputActionAsset asset = CurrentAsset();
        InputAction action;

        iconStyle = GamepadIcons.CurrentStyle();
        foreach (Row row in rows)
        {
            action = FindAction(asset, row.actionName);
            foreach (Slot slot in AllSlots)
                RefreshSlot(row, slot, action);
        }
    }
}
