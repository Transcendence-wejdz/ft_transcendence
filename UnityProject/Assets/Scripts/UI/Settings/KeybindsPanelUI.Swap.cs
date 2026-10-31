using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Rebind clashes: a key already used by another slot asks to swap both, or reverts the rebound slot.
public partial class KeybindsPanelUI
{
    private class PendingSwap
    {
        public InputAction action;
        public int bindingIndex;
        public string oldPath;
        public InputAction otherAction;
        public int otherIndex;
        public Button slotButton;
    }

    private PendingSwap pendingSwap;

    // Keeps gamepad navigation inside the two-button swap prompt.
    private static void LinkHorizontally(Button left, Button right)
    {
        left.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = right };
        right.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = left };
    }

    // Drops the override instead of storing one when the path is the action asset's default.
    private static void SetPath(InputAction action, int bindingIndex, string path)
    {
        if (path == action.bindings[bindingIndex].path)
            action.RemoveBindingOverride(bindingIndex);
        else
            action.ApplyBindingOverride(bindingIndex, path);
    }

    // The slot of a row matching this path, other than the one just changed; false when none.
    private bool FindClash(Row row, InputAction other, InputAction changedAction, int changedBindingIndex, string changedPath, out int conflictIndex)
    {
        int idx;

        conflictIndex = -1;
        foreach (Slot slot in AllSlots)
        {
            idx = FindBindingIndex(other, row, slot);
            if (idx < 0 || (other == changedAction && idx == changedBindingIndex))
                continue;
            if (other.bindings[idx].effectivePath != changedPath)
                continue;
            conflictIndex = idx;
            return true;
        }
        return false;
    }

    // Effective path clash against any other rebindable slot, across every row, within the same asset.
    private Row FindConflict(InputActionAsset asset, InputAction changedAction, int changedBindingIndex, out InputAction conflictAction, out int conflictIndex)
    {
        string changedPath = changedAction.bindings[changedBindingIndex].effectivePath;
        InputAction other;

        conflictAction = null;
        conflictIndex = -1;
        foreach (Row row in rows)
        {
            other = asset.FindAction(row.actionName);
            if (other == null || !FindClash(row, other, changedAction, changedBindingIndex, changedPath, out conflictIndex))
                continue;
            conflictAction = other;
            return row;
        }
        return null;
    }

    private void OpenSwapPrompt(Row conflict, PendingSwap swap)
    {
        pendingSwap = swap;
        swapPromptText.text = "Already used by " + conflict.displayLabel + ".\nSwap them?";
        swapPrompt.SetActive(true);
        GamepadUINavigator.Focus(swapConfirmButton);
    }

    // A clash with another slot asks before swapping; otherwise the new key is saved right away.
    private void ResolveRebind(Row row, Slot slot, InputAction action, int bindingIndex)
    {
        InputAction otherAction;
        int otherIndex;
        Row conflict = FindConflict(action.actionMap.asset, action, bindingIndex, out otherAction, out otherIndex);

        if (conflict == null)
        {
            KeybindOverrides.Save(action.actionMap.asset);
            SetStatus(string.Empty, false);
            return;
        }
        OpenSwapPrompt(conflict, new PendingSwap { action = action, bindingIndex = bindingIndex, oldPath = rebindOldPath, otherAction = otherAction, otherIndex = otherIndex, slotButton = SlotButton(row, slot) });
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

    // Gamepad B on the swap prompt answers Cancel; false when no prompt is open.
    public static bool TryCancelSwap()
    {
        if (openPanel == null || openPanel.pendingSwap == null)
            return false;
        openPanel.CancelSwap();
        return true;
    }
}
