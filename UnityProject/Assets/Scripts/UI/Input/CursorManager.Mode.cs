using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Arrow or crosshair depending on what the pointer is over, and the click pulse.
public partial class CursorManager
{
    private bool IsPointerOverClickable(Vector2 screenPosition)
    {
        PointerEventData pointerEventData;
        Selectable selectable;

        if (EventSystem.current == null)
            return false;
        pointerEventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);
        foreach (RaycastResult result in raycastResults)
        {
            selectable = result.gameObject.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.interactable)
                return true;
        }
        return false;
    }

    // Arrow (hover art over a clickable) in menus or over buttons, crosshair otherwise.
    private void UpdateMode(Vector2 screenPosition)
    {
        bool nowHovering = IsPointerOverClickable(screenPosition);
        bool arrow;

        if (nowHovering != isHoveringClickable)
        {
            isHoveringClickable = nowHovering;
            ApplyCurrentArrow();
        }
        arrow = PauseManager.IsPaused || isHoveringClickable;
        if (arrow == showingArrow)
            return;
        showingArrow = arrow;
        arrowRect.gameObject.SetActive(arrow);
        crosshairRect.gameObject.SetActive(!arrow);
    }

    // The arrow shrinks on a click, the crosshair spreads open.
    private IEnumerator AnimateClick()
    {
        float targetScale = crosshairClickScale;
        float t = 0f;
        float pulse;

        if (showingArrow)
            targetScale = clickScale;
        while (t < clickAnimationDuration)
        {
            t += Time.unscaledDeltaTime;
            pulse = Mathf.Sin(Mathf.Clamp01(t / clickAnimationDuration) * Mathf.PI);
            pointerRect.localScale = Vector3.one * Mathf.Lerp(1f, targetScale, pulse);
            yield return null;
        }
        pointerRect.localScale = Vector3.one;
        activeAnimation = null;
    }

    private void PlayClick()
    {
        if (activeAnimation != null)
            StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(AnimateClick());
    }
}
