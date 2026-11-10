using UnityEngine;
using UnityEngine.UI;

// Which controls the gamepad may land on: the open context menu, else the open NPC panel, else the front screen-space canvas.
public partial class GamepadUINavigator
{
    private const float SameRowTolerance = 1f;

    private Selectable[] buffer = new Selectable[256];

    private static bool IsCandidate(Selectable s, Canvas canvas)
    {
        if (!s.IsInteractable() || s.navigation.mode == Navigation.Mode.None)
            return false;
        return canvas.rootCanvas.renderMode != RenderMode.WorldSpace;
    }

    private static bool IsBefore(Vector3 a, Vector3 b)
    {
        if (Mathf.Abs(a.y - b.y) > SameRowTolerance)
            return a.y > b.y;
        return a.x < b.x;
    }

    // The open context menu, else the open NPC panel (unless the pause menu is over it); null means the front canvas rules.
    private static Transform FrontPanel()
    {
        if (SocialContextMenuUI.OpenMenu != null)
            return SocialContextMenuUI.OpenMenu;
        if (MenuPanel.CurrentOpen != null && !PauseManager.IsMenuOpen)
            return MenuPanel.CurrentOpen.transform;
        return null;
    }

    private int FillBuffer()
    {
        if (Selectable.allSelectableCount > buffer.Length)
            buffer = new Selectable[Selectable.allSelectableCount * 2];
        return Selectable.AllSelectablesNoAlloc(buffer);
    }

    private int FrontSortingOrder()
    {
        int count = FillBuffer();
        int front = int.MinValue;
        Canvas canvas;

        for (int i = 0; i < count; i++)
        {
            canvas = buffer[i].GetComponentInParent<Canvas>();
            if (canvas != null && IsCandidate(buffer[i], canvas))
                front = Mathf.Max(front, canvas.rootCanvas.sortingOrder);
        }
        return front;
    }

    // Invalid when gone, disabled, or behind the front menu (e.g. a HUD button left selected under the pause menu).
    private bool IsValidSelection(GameObject selected)
    {
        Selectable selectable;
        Transform panel = FrontPanel();
        Canvas canvas;

        if (selected == null || !selected.activeInHierarchy)
            return false;
        selectable = selected.GetComponent<Selectable>();
        if (selectable == null || !selectable.IsInteractable())
            return false;
        if (panel != null)
            return selected.transform.IsChildOf(panel);
        canvas = selectable.GetComponentInParent<Canvas>();
        return canvas != null && canvas.rootCanvas.sortingOrder >= FrontSortingOrder();
    }

    private static bool IsInFrontMenu(Selectable s, Canvas canvas, Transform panel, int front)
    {
        if (panel != null)
            return s.transform.IsChildOf(panel);
        return canvas.rootCanvas.sortingOrder == front;
    }

    private static Selectable MarkedDefault(Transform panel, int front)
    {
        Selectable s;
        Canvas canvas;

        foreach (DefaultSelection marked in DefaultSelection.All)
        {
            s = marked.Selectable;
            if (s == null)
                continue;
            canvas = s.GetComponentInParent<Canvas>();
            if (canvas != null && IsCandidate(s, canvas) && IsInFrontMenu(s, canvas, panel, front))
                return s;
        }
        return null;
    }

    private Selectable TopLeftIn(Transform panel, int front)
    {
        int count = FillBuffer();
        Selectable best = null;
        Canvas canvas;

        for (int i = 0; i < count; i++)
        {
            canvas = buffer[i].GetComponentInParent<Canvas>();
            if (canvas == null || !IsCandidate(buffer[i], canvas) || !IsInFrontMenu(buffer[i], canvas, panel, front))
                continue;
            if (best == null || IsBefore(buffer[i].transform.position, best.transform.position))
                best = buffer[i];
        }
        return best;
    }

    // The menu's DefaultSelection if it has one, else its top-most then left-most control.
    private Selectable FirstInFrontMenu()
    {
        int front = FrontSortingOrder();
        Transform panel = FrontPanel();
        Selectable marked = MarkedDefault(panel, front);

        if (marked != null)
            return marked;
        return TopLeftIn(panel, front);
    }

    private GameObject FirstInFrontMenuObject()
    {
        Selectable first = FirstInFrontMenu();

        if (first == null)
            return null;
        return first.gameObject;
    }
}
