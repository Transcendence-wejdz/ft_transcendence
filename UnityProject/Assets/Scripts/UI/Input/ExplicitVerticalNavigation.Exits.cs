using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Row-edge exits: Unity's automatic pick among controls outside this object, on the same canvas.
public partial class ExplicitVerticalNavigation
{
    private static Canvas RootCanvasOf(Selectable s)
    {
        Canvas canvas = s.GetComponentInParent<Canvas>();

        if (canvas == null)
            return null;
        return canvas.rootCanvas;
    }

    // Candidate for an exit: another navigable control outside this object, on the same root canvas.
    private bool IsExitCandidate(Selectable s, Selectable from, Canvas root)
    {
        Canvas other;

        if (s == from || chain.Contains(s) || !IsNavigable(s))
            return false;
        other = s.GetComponentInParent<Canvas>();
        return other != null && other.rootCanvas == root;
    }

    // Unity's automatic pick (direction over squared distance) among controls outside this object on the same canvas, so a nearer control in here never hides the exit.
    private Selectable ExitToward(Selectable from, Vector3 direction)
    {
        Canvas root = RootCanvasOf(from);
        Vector3 origin = from.transform.position;
        Selectable best = null;
        float bestScore = 0f;
        Vector3 offset;
        float dot;

        foreach (Selectable s in Selectable.allSelectablesArray)
        {
            if (!IsExitCandidate(s, from, root))
                continue;
            offset = s.transform.position - origin;
            dot = Vector3.Dot(direction, offset);
            if (dot <= 0f || dot / offset.sqrMagnitude <= bestScore)
                continue;
            bestScore = dot / offset.sqrMagnitude;
            best = s;
        }
        return best;
    }

    private void CollectExits()
    {
        exits.Clear();
        foreach (Selectable s in chain)
            exits[s] = (ExitToward(s, Vector3.left), ExitToward(s, Vector3.right));
    }
}
