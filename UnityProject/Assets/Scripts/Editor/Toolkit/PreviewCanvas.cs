using UnityEditor;
using UnityEngine;

// Colors shared by every kit preview, so hits and the player read the same in all of them.
public static class PreviewColors
{
    public static readonly Color HitEdge = new Color(1f, 0.3f, 0.2f, 1f);
    public static readonly Color Player = new Color(0.35f, 0.75f, 1f, 0.9f);
    public static readonly Color Marker = new Color(1f, 1f, 1f, 0.7f);
    public static readonly Color Handle = new Color(1f, 0.85f, 0.2f, 1f);
}

// World-space drawing inside a PreviewPanel canvas: x right, y up, Scale pixels per unit around Origin.
public readonly struct PreviewCanvas
{
    public readonly Vector2 Origin;
    public readonly float Scale;

    public PreviewCanvas(Vector2 origin, float scale)
    {
        Origin = origin;
        Scale = scale;
    }

    public Vector2 ToGui(Vector2 world) => Origin + new Vector2(world.x, -world.y) * Scale;

    public void Disc(Vector2 center, float radius, Color color)
    {
        Handles.color = color;
        Handles.DrawSolidDisc(ToGui(center), Vector3.forward, radius * Scale);
    }

    public void WireDisc(Vector2 center, float radius, Color color, float thickness = 0f)
    {
        Handles.color = color;
        Handles.DrawWireDisc(ToGui(center), Vector3.forward, radius * Scale, thickness);
    }

    // Dot of a PreviewPanel.DragPoint, a fixed size whatever the zoom.
    public void Handle(Vector2 center)
    {
        Handles.color = PreviewColors.Handle;
        Handles.DrawSolidDisc(ToGui(center), Vector3.forward, PreviewPanel.HandleRadius);
    }

    public void Line(Vector2 from, Vector2 to, Color color, float width = 2f)
    {
        Handles.color = color;
        Handles.DrawAAPolyLine(width, ToGui(from), ToGui(to));
    }

    // Fan-filled from the centroid, so concave star-shaped hitboxes still read correctly.
    public void Outline(Vector2[] worldPoints, bool closed, Color fill, Color edge)
    {
        if (worldPoints.Length < 2)
            return;

        var gui = new Vector3[worldPoints.Length];
        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < worldPoints.Length; i++)
        {
            gui[i] = ToGui(worldPoints[i]);
            centroid += (Vector2)gui[i];
        }
        centroid /= worldPoints.Length;

        if (closed && fill.a > 0f)
        {
            Handles.color = fill;
            for (int i = 0; i < gui.Length; i++)
                Handles.DrawAAConvexPolygon(centroid, gui[i], gui[(i + 1) % gui.Length]);
        }

        Handles.color = edge;
        if (!closed)
        {
            Handles.DrawAAPolyLine(2f, gui);
            return;
        }
        var loop = new Vector3[gui.Length + 1];
        gui.CopyTo(loop, 0);
        loop[gui.Length] = gui[0];
        Handles.DrawAAPolyLine(2f, loop);
    }

    // Draws a sprite with its pivot on a world position, scaled and rotated (degrees, counter-clockwise).
    public void Sprite(Sprite sprite, Vector2 worldPivot, Vector2 worldScale, float rotationDegrees, bool flipX, Color tint)
    {
        if (sprite == null)
            return;

        float unitX = Mathf.Abs(worldScale.x) / sprite.pixelsPerUnit * Scale;
        float unitY = Mathf.Abs(worldScale.y) / sprite.pixelsPerUnit * Scale;
        Vector2 regionSize = sprite.textureRect.size;
        Vector2 regionOffset = sprite.textureRectOffset;
        Vector2 pivot = sprite.pivot;
        bool flip = flipX ^ (worldScale.x < 0f);
        if (flip)
        {
            regionOffset.x = sprite.rect.width - regionOffset.x - regionSize.x;
            pivot.x = sprite.rect.width - pivot.x;
        }

        Vector2 pivotGui = ToGui(worldPivot);
        Rect rect = new Rect(pivotGui.x + (regionOffset.x - pivot.x) * unitX, pivotGui.y - (regionOffset.y + regionSize.y - pivot.y) * unitY, regionSize.x * unitX, regionSize.y * unitY);
        Texture2D texture = sprite.texture;
        Rect uv = new Rect(sprite.textureRect.x / texture.width, sprite.textureRect.y / texture.height, sprite.textureRect.width / texture.width, sprite.textureRect.height / texture.height);
        if (flip)
            uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);

        Matrix4x4 previous = GUI.matrix;
        if (Mathf.Abs(rotationDegrees) > 0.01f)
            GUIUtility.RotateAroundPivot(-rotationDegrees, pivotGui);
        Color previousColor = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(rect, texture, uv);
        GUI.color = previousColor;
        GUI.matrix = previous;
    }

    // Same, placed by a renderer's world matrix (mirrored matrices flip the sprite).
    public void Sprite(Sprite sprite, Matrix4x4 rendererToWorld, bool flipX, Color tint)
    {
        Vector2 x = rendererToWorld.GetColumn(0);
        Vector2 y = rendererToWorld.GetColumn(1);
        float determinant = x.x * y.y - x.y * y.x;
        float rotation = Mathf.Atan2(x.y, x.x) * Mathf.Rad2Deg + (determinant < 0f ? 180f : 0f);
        Sprite(sprite, rendererToWorld.MultiplyPoint3x4(Vector3.zero), new Vector2(x.magnitude * Mathf.Sign(determinant), y.magnitude), rotation, flipX, tint);
    }

    // A prefab's main sprite with the prefab root pivot at worldRoot, as its renderer sits under the root.
    public void PrefabSprite(GameObject prefab, Sprite sprite, Vector2 worldRoot, bool flipX, Color tint)
    {
        SpriteRenderer renderer = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (renderer == null)
            return;
        Vector2 offset = renderer.transform.position - prefab.transform.position;
        Sprite(sprite != null ? sprite : renderer.sprite, worldRoot + offset, renderer.transform.lossyScale, 0f, flipX, tint);
    }
}
