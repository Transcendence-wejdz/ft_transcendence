using UnityEngine;

// Hit zone geometry shared by the telegraph and the hit check: cone, lane or circle from the attack origin.
public partial class EnemyKit
{
    // Attack origin in world space: the body center moved by attackOrigin, expressed in the swing's own frame.
    public Vector2 AttackOriginWorld(Vector2 bodyCenter, Vector2 direction)
    {
        Vector2 forward = Vector2.right;
        Vector2 left;

        if (direction.sqrMagnitude > MinDirectionSqr)
            forward = direction.normalized;
        left = new Vector2(-forward.y, forward.x);
        return bodyCenter + forward * attackOrigin.x + left * attackOrigin.y;
    }

    private bool IsInLane(Vector2 direction, Vector2 toPoint)
    {
        Vector2 forward = direction.normalized;
        float along = Vector2.Dot(toPoint, forward);
        float side = Mathf.Abs(forward.x * toPoint.y - forward.y * toPoint.x);

        if (along < 0f || along > HitReach)
            return false;
        return side <= attackWidth * 0.5f;
    }

    private bool IsInReach(Vector2 toPoint)
    {
        return toPoint.sqrMagnitude <= HitReach * HitReach;
    }

    private bool IsInCone(Vector2 direction, Vector2 toPoint)
    {
        if (!IsInReach(toPoint))
            return false;
        return Vector2.Angle(direction, toPoint) <= attackAngle * 0.5f;
    }

    // The zone the telegraph shows, from the attack origin along the swing direction.
    public bool HitZoneContains(Vector2 bodyCenter, Vector2 direction, Vector2 point)
    {
        Vector2 toPoint = point - AttackOriginWorld(bodyCenter, direction);

        switch (attackShape)
        {
            case EnemyAttackShape.Lane:
                return IsInLane(direction, toPoint);
            case EnemyAttackShape.Circle:
                return IsInReach(toPoint);
            default:
                return IsInCone(direction, toPoint);
        }
    }

    public void ShowTelegraph(Transform anchor, Vector2 bodyCenter, Vector2 direction, float duration)
    {
        Vector2 origin = AttackOriginWorld(bodyCenter, direction);

        switch (attackShape)
        {
            case EnemyAttackShape.Lane:
                AttackTelegraph.ShowLane(anchor, origin, direction, HitReach, attackWidth, duration);
                break;
            case EnemyAttackShape.Circle:
                AttackTelegraph.Show(anchor, origin, Vector2.right, 360f, HitReach, duration);
                break;
            default:
                AttackTelegraph.Show(anchor, origin, direction, attackAngle, HitReach, duration);
                break;
        }
    }
}
