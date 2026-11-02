using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Mouse side while the pad is used: hover suppressed and restored, focused controls scrolled into view.
public partial class GamepadUINavigator
{
    private static Vector2 MousePosition()
    {
        if (Mouse.current == null)
            return Vector2.zero;
        return Mouse.current.position.ReadValue();
    }

    // How far a list must scroll so the control's bounds sit inside the viewport; 0 when they already do.
    private static float ScrollShift(Bounds bounds, Rect view)
    {
        if (bounds.max.y > view.yMax)
            return bounds.max.y - view.yMax;
        if (bounds.min.y < view.yMin)
            return bounds.min.y - view.yMin;
        return 0f;
    }

    private static RectTransform ViewportOf(ScrollRect scroll)
    {
        if (scroll.viewport != null)
            return scroll.viewport;
        return (RectTransform)scroll.transform;
    }

    // Scrolls the list holding a newly focused control so it stays inside the viewport.
    private static void ScrollIntoView(GameObject selected)
    {
        ScrollRect scroll;
        RectTransform viewport;
        float shift;

        if (selected == null)
            return;
        scroll = selected.GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.content == null || !selected.transform.IsChildOf(scroll.content))
            return;
        viewport = ViewportOf(scroll);
        shift = ScrollShift(RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selected.transform), viewport.rect);
        if (shift == 0f)
            return;
        scroll.velocity = Vector2.zero;
        scroll.content.anchoredPosition -= new Vector2(0f, shift);
    }

    // Pointer enter/exit reach every object from the hit up to the root, as the input module sends them.
    private static void SendToHierarchy<T>(GameObject target, ExecuteEvents.EventFunction<T> handler) where T : IEventSystemHandler
    {
        PointerEventData pointer;

        if (target == null || EventSystem.current == null)
            return;
        pointer = new PointerEventData(EventSystem.current) { position = MousePosition() };
        for (Transform t = target.transform; t != null; t = t.parent)
            ExecuteEvents.Execute(t.gameObject, pointer, handler);
    }

    private GameObject UIUnderMouse()
    {
        EventSystem eventSystem = EventSystem.current;
        PointerEventData pointer;

        if (eventSystem == null || Mouse.current == null)
            return null;
        pointer = new PointerEventData(eventSystem) { position = Mouse.current.position.ReadValue() };
        hoverHits.Clear();
        eventSystem.RaycastAll(pointer, hoverHits);
        if (hoverHits.Count == 0)
            return null;
        return hoverHits[0].gameObject;
    }

    // A still mouse cursor keeps whatever it hovers lit while the pad selects elsewhere, so its hover is cancelled for as long as the pad is in use.
    private void SuppressMouseHover()
    {
        GameObject hovered = UIUnderMouse();

        if (hovered == suppressedHover)
            return;
        suppressedHover = hovered;
        SendToHierarchy(hovered, ExecuteEvents.pointerExitHandler);
    }

    // Back on the mouse, gives the hover back if the cursor still sits on it, since the input module never re-sends an enter.
    private void RestoreMouseHover()
    {
        GameObject hovered = suppressedHover;

        suppressedHover = null;
        if (hovered != null && hovered == UIUnderMouse())
            SendToHierarchy(hovered, ExecuteEvents.pointerEnterHandler);
    }
}
