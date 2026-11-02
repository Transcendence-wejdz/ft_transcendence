using UnityEditor;
using UnityEngine;

// Hit zone shapes of the preview, the same as EnemyKit.HitZoneContains, attacking towards +x.
public partial class EnemyKitEditor
{
    private const float ZoneEdgeWidth = 2f;
    private const float FullCircle = 360f;

    private static void DrawLane(EnemyKit kit, Vector2 origin, float reach, float scale, Color fill, bool outline)
    {
        float half = kit.AttackWidth * 0.5f * scale;
        Vector3[] corners = { origin + new Vector2(0f, -half), origin + new Vector2(reach, -half), origin + new Vector2(reach, half), origin + new Vector2(0f, half) };

        Handles.color = fill;
        Handles.DrawAAConvexPolygon(corners);
        if (!outline)
            return;
        Handles.color = PreviewColors.HitEdge;
        Handles.DrawAAPolyLine(ZoneEdgeWidth, corners[0], corners[1], corners[2], corners[3], corners[0]);
    }

    private static void DrawCircleZone(Vector2 origin, float reach, Color fill, bool outline)
    {
        Handles.color = fill;
        Handles.DrawSolidDisc(origin, Vector3.forward, reach);
        if (!outline)
            return;
        Handles.color = PreviewColors.HitEdge;
        Handles.DrawWireDisc(origin, Vector3.forward, reach, ZoneEdgeWidth);
    }

    private static void DrawCone(EnemyKit kit, Vector2 origin, float reach, Color fill, bool outline)
    {
        float angle = kit.AttackAngle;
        Vector3 from = Quaternion.Euler(0f, 0f, -angle * 0.5f) * Vector3.right;
        Vector3 to = Quaternion.Euler(0f, 0f, angle * 0.5f) * Vector3.right;

        Handles.color = fill;
        Handles.DrawSolidArc(origin, Vector3.forward, from, angle, reach);
        if (!outline)
            return;
        Handles.color = PreviewColors.HitEdge;
        Handles.DrawWireArc(origin, Vector3.forward, from, angle, reach, ZoneEdgeWidth);
        if (angle >= FullCircle)
            return;
        Handles.DrawAAPolyLine(ZoneEdgeWidth, origin, (Vector3)origin + from * reach);
        Handles.DrawAAPolyLine(ZoneEdgeWidth, origin, (Vector3)origin + to * reach);
    }

    // fraction < 1 draws the telegraph fill so far.
    private static void DrawZone(EnemyKit kit, PreviewCanvas canvas, Vector2 attackOrigin, float fraction, Color fill, bool outline)
    {
        float reach = kit.HitReach * canvas.Scale * Mathf.Clamp01(fraction);
        Vector2 origin;

        if (reach <= 0f)
            return;
        origin = canvas.ToGui(attackOrigin);
        if (kit.AttackShape == EnemyAttackShape.Lane)
            DrawLane(kit, origin, reach, canvas.Scale, fill, outline);
        else if (kit.AttackShape == EnemyAttackShape.Circle)
            DrawCircleZone(origin, reach, fill, outline);
        else
            DrawCone(kit, origin, reach, fill, outline);
    }
}
