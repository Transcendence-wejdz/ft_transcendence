using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Explicit gamepad navigation for the controls under this object: rows top to bottom in hierarchy order (controls at one height share a row), so up/down never skips an offset control and left/right stay within the row.
public class ExplicitVerticalNavigation : MonoBehaviour
{
    private const float RowTolerance = 8f;

    private readonly List<Selectable> found = new List<Selectable>();
    private readonly List<Selectable> chain = new List<Selectable>();
    private readonly List<List<Selectable>> rows = new List<List<Selectable>>();
    private int signature;
    private int buildFrame = -1;

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
        if (buildFrame >= 0 && Time.frameCount >= buildFrame)
        {
            buildFrame = -1;
            Build();
        }
    }

    private int CurrentSignature()
    {
        GetComponentsInChildren(found);
        int hash = found.Count;
        foreach (Selectable s in found)
            hash = hash * 31 + s.GetHashCode() * (IsNavigable(s) ? 1 : -1);
        return hash;
    }

    private static bool IsNavigable(Selectable s) => s.IsInteractable() && s.navigation.mode != Navigation.Mode.None;

    // Row edges keep their automatic exits out of this object; sliders too, since the gamepad only changes them in edit mode (see GamepadUINavigator).
    private void Build()
    {
        chain.Clear();
        rows.Clear();
        GetComponentsInChildren(found);
        foreach (Selectable s in found)
        {
            if (!IsNavigable(s))
                continue;
            chain.Add(s);
            float y = s.transform.position.y;
            if (rows.Count == 0 || Mathf.Abs(rows[rows.Count - 1][0].transform.position.y - y) > RowTolerance * s.transform.lossyScale.y)
                rows.Add(new List<Selectable>());
            rows[rows.Count - 1].Add(s);
        }
        foreach (List<Selectable> row in rows)
            row.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        var exits = new Dictionary<Selectable, (Selectable left, Selectable right)>();
        foreach (Selectable s in chain)
            exits[s] = (ExitToward(s, Vector3.left), ExitToward(s, Vector3.right));

        for (int r = 0; r < rows.Count; r++)
        {
            List<Selectable> row = rows[r];
            for (int i = 0; i < row.Count; i++)
            {
                Selectable s = row[i];
                s.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = r > 0 ? Nearest(rows[r - 1], s) : null,
                    selectOnDown = r < rows.Count - 1 ? Nearest(rows[r + 1], s) : null,
                    selectOnLeft = i > 0 ? row[i - 1] : exits[s].left,
                    selectOnRight = i < row.Count - 1 ? row[i + 1] : exits[s].right,
                };
            }
        }
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

    // Unity's automatic pick (direction over squared distance) among controls outside this object on the same canvas, so a nearer control in here never hides the exit.
    private Selectable ExitToward(Selectable from, Vector3 direction)
    {
        Canvas canvas = from.GetComponentInParent<Canvas>();
        Canvas root = canvas != null ? canvas.rootCanvas : null;
        Vector3 origin = from.transform.position;
        Selectable best = null;
        float bestScore = 0f;
        foreach (Selectable s in Selectable.allSelectablesArray)
        {
            if (s == from || chain.Contains(s) || !IsNavigable(s))
                continue;
            Canvas other = s.GetComponentInParent<Canvas>();
            if (other == null || other.rootCanvas != root)
                continue;

            Vector3 offset = s.transform.position - origin;
            float dot = Vector3.Dot(direction, offset);
            if (dot <= 0f)
                continue;
            float score = dot / offset.sqrMagnitude;
            if (score > bestScore)
            {
                bestScore = score;
                best = s;
            }
        }
        return best;
    }
}
