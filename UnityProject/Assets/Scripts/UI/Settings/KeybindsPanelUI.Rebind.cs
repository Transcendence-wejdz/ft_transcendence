using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Interactive rebind of one slot: waiting prompt, device filter, then the clash check.
public partial class KeybindsPanelUI
{
    // A gamepad slot only listens to the pad, a keyboard or mouse slot never does.
    private static InputActionRebindingExtensions.RebindingOperation FilterDevices(InputActionRebindingExtensions.RebindingOperation rebind, string prefix)
    {
        if (prefix == GamepadPrefix)
            return rebind.WithControlsExcluding("Mouse").WithControlsExcluding("Keyboard");
        return rebind.WithControlsExcluding("Gamepad");
    }

    private void FinishRebind(Row row, Slot slot, InputAction action, int bindingIndex, bool cancelled)
    {
        activeRebind?.Dispose();
        activeRebind = null;
        action.Enable();
        if (waitingPrompt != null)
            waitingPrompt.SetActive(false);
        if (!cancelled)
            ResolveRebind(row, slot, action, bindingIndex);
        if (pendingSwap == null)
            GamepadUINavigator.Focus(SlotButton(row, slot));
        RefreshAllLabels();
    }

    // The keyboard and mouse wait on the prompt's Cancel button; a gamepad press would land on it, so the pad keeps its slot.
    private void ShowWaiting(Row row, Slot slot)
    {
        SlotLabel(row, slot).text = "...";
        if (slot == Slot.Gamepad)
            row.gamepadIcon.enabled = false;
        if (waitingPrompt == null)
            return;
        waitingPrompt.SetActive(true);
        if (slot != Slot.Gamepad)
            GamepadUINavigator.Focus(waitingPrompt.GetComponentInChildren<Button>());
    }

    private void BeginListening(Row row, Slot slot, InputAction action, int bindingIndex, string prefix)
    {
        InputActionRebindingExtensions.RebindingOperation rebind = FilterDevices(action.PerformInteractiveRebinding(bindingIndex), prefix);

        activeRebind = rebind
            .OnMatchWaitForAnother(MatchWaitSeconds)
            .OnComplete(op => FinishRebind(row, slot, action, bindingIndex, cancelled: false))
            .OnCancel(op => FinishRebind(row, slot, action, bindingIndex, cancelled: true))
            .Start();
    }

    private void StartRebind(Row row, Slot slot)
    {
        string prefix = DevicePrefix(row, slot);
        InputAction action;
        int bindingIndex;

        if (activeRebind != null || pendingSwap != null || string.IsNullOrEmpty(prefix))
            return;
        action = FindAction(CurrentAsset(), row.actionName);
        bindingIndex = FindBindingIndex(action, row, slot);
        if (action == null || bindingIndex < 0)
        {
            SetStatus("Can't rebind right now.", true);
            return;
        }
        rebindOldPath = action.bindings[bindingIndex].effectivePath;
        action.Disable();
        ShowWaiting(row, slot);
        BeginListening(row, slot, action, bindingIndex, prefix);
    }
}
