using System.Collections.Generic;
using UnityEngine;

// Whether this decor covers a point: cached area of its sprites, then per-sprite sorting checks.
public partial class RevealOccluder
{
    private static bool ContainsPoint(Bounds bounds, Vector3 point)
    {
        if (point.x < bounds.min.x || point.x > bounds.max.x)
            return false;
        return point.y >= bounds.min.y && point.y <= bounds.max.y;
    }

    // Union of the owned sprites' bounds, cached for static decor so the per-frame test stays in managed code.
    private Rect Area()
    {
        bool any = false;
        Bounds union = default;

        if (IsAreaCached)
            return area;
        areaFrame = Time.frameCount;
        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer == null)
                continue;
            if (!any)
                union = spriteRenderer.bounds;
            else
                union.Encapsulate(spriteRenderer.bounds);
            any = true;
        }
        area = NoArea;
        if (any)
            area = new Rect(union.min.x, union.min.y, union.size.x, union.size.y);
        return area;
    }

    // A sprite only hides the player when drawn over them, unless it always sits in front.
    private bool DrawsOver(SpriteRenderer spriteRenderer, int sortingLayerId, int sortingOrder)
    {
        if (alwaysInFront)
            return true;
        return spriteRenderer.sortingLayerID == sortingLayerId && spriteRenderer.sortingOrder > sortingOrder;
    }

    // Cheap cached-area rejection first; only occluders actually around the point pay for the per-sprite sorting checks.
    public bool Covers(Vector3 point, int sortingLayerId, int sortingOrder)
    {
        if (!Area().Contains(point))
            return false;
        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer == null || !spriteRenderer.enabled)
                continue;
            if (DrawsOver(spriteRenderer, sortingLayerId, sortingOrder) && ContainsPoint(spriteRenderer.bounds, point))
                return true;
        }
        return false;
    }
}
