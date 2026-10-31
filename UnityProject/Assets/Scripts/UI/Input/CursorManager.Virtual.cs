using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Virtual cursor: with a non default mouse sensitivity, the OS cursor is locked and a virtual one moves by delta x sensitivity.
public partial class CursorManager
{
    private static bool IsInsideWindow(Vector2 osPosition)
    {
        if (!Application.isFocused)
            return false;
        if (osPosition.x < 0f || osPosition.x > Screen.width)
            return false;
        return osPosition.y >= 0f && osPosition.y <= Screen.height;
    }

    private bool WantsVirtualCursor(bool osInsideWindow)
    {
        if (Mathf.Approximately(GameSettings.MouseSensitivity, 1f))
            return false;
        if (PauseManager.IsPaused || InputModeTracker.UsingGamepad || !Application.isFocused)
            return false;
        return virtualActive || osInsideWindow;
    }

    private void StartVirtualCursor()
    {
        virtualPosition = Mouse.current.position.ReadValue();
        Cursor.lockState = CursorLockMode.Locked;
        virtualActive = true;
    }

    // Menus get the OS cursor back where the virtual one was.
    private void StopVirtualCursor()
    {
        virtualActive = false;
        Cursor.lockState = CursorLockMode.None;
        Mouse.current.WarpCursorPosition(virtualPosition);
    }

    private void MoveVirtualCursor()
    {
        virtualPosition += Mouse.current.delta.ReadValue() * GameSettings.MouseSensitivity;
        virtualPosition.x = Mathf.Clamp(virtualPosition.x, 0f, Screen.width);
        virtualPosition.y = Mathf.Clamp(virtualPosition.y, 0f, Screen.height);
    }

    // Outside menus with a non default mouse sensitivity, locks the OS cursor and moves a virtual one by mouse delta x sensitivity.
    private void UpdateVirtualCursor(bool osInsideWindow)
    {
        bool wanted = WantsVirtualCursor(osInsideWindow);

        if (wanted && !virtualActive)
            StartVirtualCursor();
        else if (!wanted && virtualActive)
            StopVirtualCursor();
        else if (virtualActive)
            MoveVirtualCursor();
    }
}
