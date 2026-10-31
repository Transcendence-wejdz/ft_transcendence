using System.Collections.Generic;
using UnityEngine;

// Hitbox outline read from the FX prefab's Collider2D, and the fan mesh that fills it.
public partial class AbilityAimPreview
{
    private Vector3 ShapeCenter()
    {
        Vector3 center = Vector3.zero;

        foreach (Vector3 point in shape)
            center += point;
        if (shape.Count > 0)
            center /= shape.Count;
        return center;
    }

    private int[] FanTriangles()
    {
        int[] triangles = new int[shape.Count * 3];

        for (int i = 0; i < shape.Count; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % shape.Count + 1;
        }
        return triangles;
    }

    // Vertex and triangle layout for the current shape; positions are filled per frame by ShowFill.
    private void RebuildFillMesh()
    {
        if (fillMesh == null)
            return;
        shapeCenter = ShapeCenter();
        fillMesh.Clear();
        fillVertices = new Vector3[shape.Count + 1];
        fillColors = new Color[shape.Count + 1];
        fillMesh.vertices = fillVertices;
        fillMesh.triangles = FanTriangles();
    }

    private void AddArc(Vector2 center, float radius, float fromDeg, float toDeg, bool closed)
    {
        int steps = Mathf.Max(2, Mathf.RoundToInt(arcSegments * (toDeg - fromDeg) / 360f));
        int count = steps + 1;
        float angle;

        if (closed)
            count = steps;
        for (int i = 0; i < count; i++)
        {
            angle = Mathf.Lerp(fromDeg, toDeg, (float)i / steps) * Mathf.Deg2Rad;
            shape.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    // Two half circles joined by straight sides, along the capsule's long axis.
    private void AddCapsule(CapsuleCollider2D capsule)
    {
        bool vertical = capsule.direction == CapsuleDirection2D.Vertical;
        float width = capsule.size.y;
        float length = capsule.size.x;
        Vector2 axis = Vector2.right;
        float start = -90f;
        float radius;
        float straight;

        if (vertical)
        {
            width = capsule.size.x;
            length = capsule.size.y;
            axis = Vector2.up;
            start = 0f;
        }
        radius = width * 0.5f;
        straight = Mathf.Max(0f, length * 0.5f - radius);
        AddArc(capsule.offset + axis * straight, radius, start, start + 180f, closed: false);
        AddArc(capsule.offset - axis * straight, radius, start + 180f, start + 360f, closed: false);
    }

    private void AddBox(BoxCollider2D box)
    {
        Vector2 half = box.size * 0.5f;

        shape.Add(box.offset + new Vector2(-half.x, -half.y));
        shape.Add(box.offset + new Vector2(half.x, -half.y));
        shape.Add(box.offset + new Vector2(half.x, half.y));
        shape.Add(box.offset + new Vector2(-half.x, half.y));
    }

    private void AddPolygon(PolygonCollider2D polygon)
    {
        foreach (Vector2 point in polygon.GetPath(0))
            shape.Add(polygon.offset + point);
    }

    private void AddColliderOutline(Collider2D hitbox)
    {
        if (hitbox is CircleCollider2D circle)
            AddArc(circle.offset, circle.radius, 0f, 360f, closed: true);
        else if (hitbox is CapsuleCollider2D capsule)
            AddCapsule(capsule);
        else if (hitbox is BoxCollider2D box)
            AddBox(box);
        else if (hitbox is PolygonCollider2D polygon)
            AddPolygon(polygon);
    }

    // Hitbox outline in the FX root's local space, from its first Collider2D (circle, capsule, box or polygon).
    private void BuildShape(GameObject fx)
    {
        Collider2D hitbox = null;
        Matrix4x4 toRoot;

        shape.Clear();
        if (fx != null)
            hitbox = fx.GetComponentInChildren<Collider2D>(true);
        if (hitbox == null)
            return;
        AddColliderOutline(hitbox);
        toRoot = fx.transform.worldToLocalMatrix * hitbox.transform.localToWorldMatrix;
        for (int i = 0; i < shape.Count; i++)
            shape[i] = toRoot.MultiplyPoint3x4(shape[i]);
        RebuildFillMesh();
    }
}
