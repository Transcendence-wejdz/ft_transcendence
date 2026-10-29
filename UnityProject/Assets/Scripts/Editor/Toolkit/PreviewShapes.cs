using System.Collections.Generic;
using UnityEngine;

// World-space shapes for previews: prefab colliders, capsule queries and a prefab's body collider.
public static class PreviewShapes
{
    // Outline of every 2D collider under root, as world polygons through the given root-space to world matrix.
    public static List<(Vector2[] points, bool closed)> ColliderOutlines(GameObject root, Matrix4x4 rootToWorld)
    {
        var outlines = new List<(Vector2[] points, bool closed)>();
        foreach (Collider2D collider in root.GetComponentsInChildren<Collider2D>(true))
        {
            Matrix4x4 matrix = rootToWorld * root.transform.worldToLocalMatrix * collider.transform.localToWorldMatrix;
            switch (collider)
            {
                case CircleCollider2D circle:
                    outlines.Add((Transform(matrix, CirclePoints(circle.offset, circle.radius, 40)), true));
                    break;
                case BoxCollider2D box:
                    Vector2 half = box.size * 0.5f;
                    outlines.Add((Transform(matrix, new[] { box.offset + new Vector2(-half.x, -half.y), box.offset + new Vector2(half.x, -half.y), box.offset + new Vector2(half.x, half.y), box.offset + new Vector2(-half.x, half.y) }), true));
                    break;
                case CapsuleCollider2D capsule:
                    outlines.Add((Transform(matrix, CapsulePoints(capsule.offset, capsule.size, capsule.direction)), true));
                    break;
                case PolygonCollider2D polygon:
                    for (int i = 0; i < polygon.pathCount; i++)
                    {
                        Vector2[] path = polygon.GetPath(i);
                        for (int p = 0; p < path.Length; p++)
                            path[p] += polygon.offset;
                        outlines.Add((Transform(matrix, path), true));
                    }
                    break;
                case EdgeCollider2D edge:
                    Vector2[] points = edge.points;
                    for (int p = 0; p < points.Length; p++)
                        points[p] += edge.offset;
                    outlines.Add((Transform(matrix, points), false));
                    break;
            }
        }
        return outlines;
    }

    // World outline of a capsule (e.g. a Physics2D.OverlapCapsule query) centered at center and rotated by angleDegrees.
    public static Vector2[] CapsuleOutline(Vector2 center, Vector2 size, CapsuleDirection2D direction, float angleDegrees)
    {
        return Transform(Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, angleDegrees), Vector3.one), CapsulePoints(Vector2.zero, size, direction));
    }

    // Offset and radius of a prefab root's CircleCollider2D, scaled like the prefab; zero offset and fallbackRadius without one.
    public static void PrefabBody(GameObject prefab, float fallbackRadius, out Vector2 offset, out float radius)
    {
        CircleCollider2D circle = prefab != null ? prefab.GetComponent<CircleCollider2D>() : null;
        Vector3 scale = prefab != null ? prefab.transform.localScale : Vector3.one;
        offset = circle != null ? Vector2.Scale(circle.offset, scale) : Vector2.zero;
        radius = circle != null ? circle.radius * Mathf.Max(scale.x, scale.y) : fallbackRadius;
    }

    private static Vector2[] Transform(Matrix4x4 matrix, Vector2[] points)
    {
        var result = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
            result[i] = matrix.MultiplyPoint3x4(points[i]);
        return result;
    }

    private static Vector2[] CirclePoints(Vector2 center, float radius, int segments)
    {
        var points = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return points;
    }

    private static Vector2[] CapsulePoints(Vector2 offset, Vector2 size, CapsuleDirection2D direction)
    {
        bool vertical = direction == CapsuleDirection2D.Vertical;
        float radius = (vertical ? size.x : size.y) * 0.5f;
        float halfLength = Mathf.Max(0f, (vertical ? size.y : size.x) * 0.5f - radius);
        var points = new List<Vector2>();
        for (int cap = 0; cap < 2; cap++)
        {
            float sign = cap == 0 ? 1f : -1f;
            Vector2 center = offset + (vertical ? new Vector2(0f, halfLength * sign) : new Vector2(halfLength * sign, 0f));
            float start = vertical ? (cap == 0 ? 0f : Mathf.PI) : (cap == 0 ? -Mathf.PI * 0.5f : Mathf.PI * 0.5f);
            for (int i = 0; i <= 16; i++)
            {
                float angle = start + i * Mathf.PI / 16f;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }
        return points.ToArray();
    }
}
