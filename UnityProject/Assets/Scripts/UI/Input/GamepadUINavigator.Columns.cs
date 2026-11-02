using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Menu columns: left/right return to the control last focused in a column, up/down wrap inside it.
public partial class GamepadUINavigator
{
    // The child of the control's full-screen panel holding it (pause buttons, friends list, a sidebar, a page), so wrapping never leaves that column.
    private static Transform MenuColumn(Transform control)
    {
        Canvas canvas = control.GetComponentInParent<Canvas>();
        Transform root;
        Transform t = control;

        if (canvas == null)
            return null;
        root = canvas.rootCanvas.transform;
        while (t.parent != null && t.parent != root && t.parent.parent != root)
            t = t.parent;
        if (t.parent == null)
            return null;
        return t;
    }

    private void RememberInColumn(GameObject selected)
    {
        Selectable selectable = null;
        Transform column;

        if (selected != null)
            selectable = selected.GetComponent<Selectable>();
        if (selectable == null)
            return;
        column = MenuColumn(selectable.transform);
        if (column != null)
            lastInColumn[column] = selectable;
    }

    // The control last focused in the target's column, when that is another column than the one we leave.
    private Selectable RememberedIn(Selectable target, Selectable from)
    {
        Transform column;
        Selectable remembered;

        if (target == null)
            return null;
        column = MenuColumn(target.transform);
        if (column == null || column == MenuColumn(from.transform))
            return null;
        if (!lastInColumn.TryGetValue(column, out remembered) || remembered == null || remembered == target)
            return null;
        if (!IsValidSelection(remembered.gameObject))
            return null;
        return remembered;
    }

    // Left/right into another column lands back on the control last focused there, instead of the nearest one.
    private bool TryCrossColumn(GameObject selected, MoveDirection direction)
    {
        Selectable current;
        Selectable remembered;

        if (!IsHorizontal(direction))
            return false;
        current = selected.GetComponent<Selectable>();
        if (current == null)
            return false;
        remembered = RememberedIn(SideNeighbour(current, direction), current);
        if (remembered == null)
            return false;
        EventSystem.current.SetSelectedGameObject(remembered.gameObject);
        return true;
    }

    private bool IsColumnEndCandidate(Selectable s, Selectable from, Transform column, float tolerance)
    {
        if (s == from || Mathf.Abs(s.transform.position.y - from.transform.position.y) <= tolerance)
            return false;
        if (!s.transform.IsChildOf(column) || s.navigation.mode == Navigation.Mode.None)
            return false;
        return IsValidSelection(s.gameObject);
    }

    // Further toward the end wins; at the same height, the one closest in x to where we come from.
    private static bool IsBetterEnd(Vector3 p, Vector3 b, Vector3 origin, bool top, float tolerance)
    {
        float further = b.y - p.y;

        if (top)
            further = p.y - b.y;
        if (further > tolerance)
            return true;
        return further >= -tolerance && Mathf.Abs(p.x - origin.x) < Mathf.Abs(b.x - origin.x);
    }

    // Top-most (or bottom-most) row of the column, skipping the current row, then the control closest in x.
    private Selectable ColumnEnd(Selectable from, Transform column, bool top)
    {
        int count = FillBuffer();
        float tolerance = RowTolerance * from.transform.lossyScale.y;
        Vector3 origin = from.transform.position;
        Selectable best = null;

        for (int i = 0; i < count; i++)
        {
            if (!IsColumnEndCandidate(buffer[i], from, column, tolerance))
                continue;
            if (best == null || IsBetterEnd(buffer[i].transform.position, best.transform.position, origin, top, tolerance))
                best = buffer[i];
        }
        return best;
    }

    private static Selectable VerticalNeighbour(Selectable from, bool down)
    {
        if (down)
            return from.FindSelectableOnDown();
        return from.FindSelectableOnUp();
    }

    // Up or down with nothing further that way inside the same column lands on the column's other end, nearest in x.
    private bool TryWrap(GameObject selected, MoveDirection direction)
    {
        Selectable current;
        Transform column;
        bool down = direction == MoveDirection.Down;
        Selectable next;
        Selectable end;

        if (IsHorizontal(direction))
            return false;
        current = selected.GetComponent<Selectable>();
        if (current == null)
            return false;
        column = MenuColumn(current.transform);
        if (column == null)
            return false;
        next = VerticalNeighbour(current, down);
        if (next != null && IsValidSelection(next.gameObject) && next.transform.IsChildOf(column))
            return false;
        end = ColumnEnd(current, column, top: down);
        if (end != null)
            EventSystem.current.SetSelectedGameObject(end.gameObject);
        return true;
    }
}
