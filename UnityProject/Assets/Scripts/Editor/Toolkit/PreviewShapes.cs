using System.Collections.Generic;
using UnityEngine;

// World-space shapes for previews: prefab colliders, capsule queries and a prefab's body collider.
public static class PreviewShapes
{
    private const int CircleSegments = 40;
    private const int CapSegments = 16;

    private static Vector2[] Transform(Matrix4x4 matrix, Vector2[] points)
    {
        Vector2[] result = new Vector2[points.Length];

        for (int i = 0; i < points.Length; i++)
            result[i] = matrix.MultiplyPoint3x4(points[i]);
        return result;
    }

    private static Vector2[] Offset(Vector2[] points, Vector2 offset)
    {
        for (int i = 0; i < points.Length; i++)
            points[i] += offset;
        return points;
    }

    private static Vector2[] CirclePoints(Vector2 center, float radius, int segments)
    {
        Vector2[] points = new Vector2[segments];
        float angle;

        for (int i = 0; i < segments; i++)
        {
            angle = i * Mathf.PI * 2f / segments;
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return points;
    }

    private static Vector2[] BoxPoints(Vector2 offset, Vector2 size)
    {
        Vector2 half = size * 0.5f;

        return new[]
        {
            offset + new Vector2(-half.x, -half.y),
            offset + new Vector2(half.x, -half.y),
            offset + new Vector2(half.x, half.y),
            offset + new Vector2(-half.x, half.y),
        };
    }

    // Half circle of one capsule end, starting at angle start.
    private static void AddCap(List<Vector2> points, Vector2 center, float radius, float start)
    {
        float angle;

        for (int i = 0; i <= CapSegments; i++)
        {
            angle = start + i * Mathf.PI / CapSegments;
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    private static Vector2[] CapsulePoints(Vector2 offset, Vector2 size, CapsuleDirection2D direction)
    {
        List<Vector2> points = new List<Vector2>();
        float radius;
        float halfLength;

        if (direction == CapsuleDirection2D.Vertical)
        {
            radius = size.x * 0.5f;
            halfLength = Mathf.Max(0f, size.y * 0.5f - radius);
            AddCap(points, offset + new Vector2(0f, halfLength), radius, 0f);
            AddCap(points, offset + new Vector2(0f, -halfLength), radius, Mathf.PI);
            return points.ToArray();
        }
        radius = size.y * 0.5f;
        halfLength = Mathf.Max(0f, size.x * 0.5f - radius);
        AddCap(points, offset + new Vector2(halfLength, 0f), radius, -Mathf.PI * 0.5f);
        AddCap(points, offset + new Vector2(-halfLength, 0f), radius, Mathf.PI * 0.5f);
        return points.ToArray();
    }

    // Collider-space outlines of one collider; edges are the only open ones.
    private static void AddOutlines(List<(Vector2[] points, bool closed)> outlines, Collider2D collider, Matrix4x4 matrix)
    {
        if (collider is CircleCollider2D circle)
            outlines.Add((Transform(matrix, CirclePoints(circle.offset, circle.radius, CircleSegments)), true));
        else if (collider is BoxCollider2D box)
            outlines.Add((Transform(matrix, BoxPoints(box.offset, box.size)), true));
        else if (collider is CapsuleCollider2D capsule)
            outlines.Add((Transform(matrix, CapsulePoints(capsule.offset, capsule.size, capsule.direction)), true));
        else if (collider is EdgeCollider2D edge)
            outlines.Add((Transform(matrix, Offset(edge.points, edge.offset)), false));
        else if (collider is PolygonCollider2D polygon)
        {
            for (int i = 0; i < polygon.pathCount; i++)
                outlines.Add((Transform(matrix, Offset(polygon.GetPath(i), polygon.offset)), true));
        }
    }

    // Outline of every 2D collider under root, as world polygons through the given root-space to world matrix.
    public static List<(Vector2[] points, bool closed)> ColliderOutlines(GameObject root, Matrix4x4 rootToWorld)
    {
        List<(Vector2[] points, bool closed)> outlines = new List<(Vector2[] points, bool closed)>();
        Matrix4x4 matrix;

        foreach (Collider2D collider in root.GetComponentsInChildren<Collider2D>(true))
        {
            matrix = rootToWorld * root.transform.worldToLocalMatrix * collider.transform.localToWorldMatrix;
            AddOutlines(outlines, collider, matrix);
        }
        return outlines;
    }

    // World outline of a capsule (e.g. a Physics2D.OverlapCapsule query) centered at center and rotated by angleDegrees.
    public static Vector2[] CapsuleOutline(Vector2 center, Vector2 size, CapsuleDirection2D direction, float angleDegrees)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, angleDegrees), Vector3.one);

        return Transform(matrix, CapsulePoints(Vector2.zero, size, direction));
    }

    // Offset and radius of a prefab root's CircleCollider2D, scaled like the prefab; zero offset and fallbackRadius without one.
    public static void PrefabBody(GameObject prefab, float fallbackRadius, out Vector2 offset, out float radius)
    {
        CircleCollider2D circle = null;
        Vector3 scale;

        offset = Vector2.zero;
        radius = fallbackRadius;
        if (prefab != null)
            circle = prefab.GetComponent<CircleCollider2D>();
        if (circle == null)
            return;
        scale = prefab.transform.localScale;
        offset = Vector2.Scale(circle.offset, scale);
        radius = circle.radius * Mathf.Max(scale.x, scale.y);
    }
}
