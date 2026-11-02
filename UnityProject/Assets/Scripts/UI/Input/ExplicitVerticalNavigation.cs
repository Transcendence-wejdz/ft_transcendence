using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Explicit gamepad navigation for the controls under this object: rows top to bottom in hierarchy order (controls at one height share a row), so up/down never skips an offset control and left/right stay within the row.
public partial class ExplicitVerticalNavigation : MonoBehaviour
{
    private const float RowTolerance = 8f;

    private readonly List<Selectable> found = new List<Selectable>();
    private readonly List<Selectable> chain = new List<Selectable>();
    private readonly List<List<Selectable>> rows = new List<List<Selectable>>();
    private readonly Dictionary<Selectable, (Selectable left, Selectable right)> exits = new Dictionary<Selectable, (Selectable left, Selectable right)>();
    private int signature;
    private int buildFrame = -1;

    private static bool IsNavigable(Selectable s)
    {
        return s.IsInteractable() && s.navigation.mode != Navigation.Mode.None;
    }

    private static int CompareX(Selectable a, Selectable b)
    {
        return a.transform.position.x.CompareTo(b.transform.position.x);
    }

    private static Selectable Nearest(List<Selectable> row, Selectable from)
    {
        float x = from.transform.position.x;
        Selectable best = row[0];

        foreach (Selectable s in row)
        {
            if (Mathf.Abs(s.transform.position.x - x) < Mathf.Abs(best.transform.position.x - x))
                best = s;
        }
        return best;
    }

    private int CurrentSignature()
    {
        int hash;
        int sign;

        GetComponentsInChildren(found);
        hash = found.Count;
        foreach (Selectable s in found)
        {
            sign = -1;
            if (IsNavigable(s))
                sign = 1;
            hash = hash * 31 + s.GetHashCode() * sign;
        }
        return hash;
    }

    private bool StartsNewRow(Selectable s)
    {
        float y = s.transform.position.y;

        if (rows.Count == 0)
            return true;
        return Mathf.Abs(rows[rows.Count - 1][0].transform.position.y - y) > RowTolerance * s.transform.lossyScale.y;
    }

    // Controls at one height (within RowTolerance) share a row, each row sorted left to right.
    private void GroupIntoRows()
    {
        chain.Clear();
        rows.Clear();
        GetComponentsInChildren(found);
        foreach (Selectable s in found)
        {
            if (!IsNavigable(s))
                continue;
            chain.Add(s);
            if (StartsNewRow(s))
                rows.Add(new List<Selectable>());
            rows[rows.Count - 1].Add(s);
        }
        foreach (List<Selectable> row in rows)
            row.Sort(CompareX);
    }

    private Selectable Up(int r, Selectable s)
    {
        if (r <= 0)
            return null;
        return Nearest(rows[r - 1], s);
    }

    private Selectable Down(int r, Selectable s)
    {
        if (r >= rows.Count - 1)
            return null;
        return Nearest(rows[r + 1], s);
    }

    private Selectable Left(List<Selectable> row, int i)
    {
        if (i > 0)
            return row[i - 1];
        return exits[row[i]].left;
    }

    private Selectable Right(List<Selectable> row, int i)
    {
        if (i < row.Count - 1)
            return row[i + 1];
        return exits[row[i]].right;
    }

    private void Link(int r, int i)
    {
        List<Selectable> row = rows[r];
        Selectable s = row[i];

        s.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = Up(r, s),
            selectOnDown = Down(r, s),
            selectOnLeft = Left(row, i),
            selectOnRight = Right(row, i),
        };
    }

    // Row edges keep their automatic exits out of this object; sliders too, since the gamepad only changes them in edit mode (see GamepadUINavigator).
    private void Build()
    {
        GroupIntoRows();
        CollectExits();
        for (int r = 0; r < rows.Count; r++)
        {
            for (int i = 0; i < rows[r].Count; i++)
                Link(r, i);
        }
    }

    void OnEnable()
    {
        signature = 0;
        buildFrame = -1;
    }

    // Rebuilds a frame after the controls change (rows added or removed, sections collapsed, buttons toggled), once layout has placed them.
    void LateUpdate()
    {
        int current = CurrentSignature();

        if (current != signature)
        {
            signature = current;
            buildFrame = Time.frameCount + 1;
        }
        if (buildFrame < 0 || Time.frameCount < buildFrame)
            return;
        buildFrame = -1;
        Build();
    }
}
