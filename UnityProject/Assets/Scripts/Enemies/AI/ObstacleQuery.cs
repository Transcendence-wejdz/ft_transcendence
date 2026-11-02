using UnityEngine;

// Physics2D queries against the static colliders enemies path around.
public static class ObstacleQuery
{
    private const float MinDistance = 0.01f;

    private static readonly Collider2D[] overlapBuffer = new Collider2D[16];
    private static readonly RaycastHit2D[] castBuffer = new RaycastHit2D[16];
    private static ContactFilter2D filter;
    private static bool filterReady;

    // True when nothing static blocks the circle's path.
    public static bool IsClear(Vector2 from, Vector2 to, float radius)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        int count;

        EnsureFilter();
        if (distance < MinDistance)
            return true;
        count = Physics2D.CircleCast(from, radius, delta / distance, filter, castBuffer, distance);
        for (int i = 0; i < count; i++)
        {
            if (IsObstacle(castBuffer[i].collider))
                return false;
        }
        return true;
    }

    // True when a static collider overlaps the circle.
    public static bool IsBlocked(Vector2 point, float radius)
    {
        int count;

        EnsureFilter();
        count = Physics2D.OverlapCircle(point, radius, filter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (IsObstacle(overlapBuffer[i]))
                return true;
        }
        return false;
    }

    private static bool IsObstacle(Collider2D collider)
    {
        Rigidbody2D body;

        if (collider == null || collider.isTrigger)
            return false;
        body = collider.attachedRigidbody;
        return body == null || body.bodyType == RigidbodyType2D.Static;
    }

    private static void EnsureFilter()
    {
        if (filterReady)
            return;
        filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        filterReady = true;
    }
}
