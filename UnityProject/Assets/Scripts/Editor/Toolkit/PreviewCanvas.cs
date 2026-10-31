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
    private const float OutlineWidth = 2f;
    private const float MinRotation = 0.01f;
    private const float MirroredRotation = 180f;

    public readonly Vector2 Origin;
    public readonly float Scale;

    public PreviewCanvas(Vector2 origin, float scale)
    {
        Origin = origin;
        Scale = scale;
    }

    private void FillFan(Vector3[] gui, Color fill)
    {
        Vector2 centroid = Vector2.zero;

        for (int i = 0; i < gui.Length; i++)
            centroid += (Vector2)gui[i];
        centroid /= gui.Length;
        Handles.color = fill;
        for (int i = 0; i < gui.Length; i++)
            Handles.DrawAAConvexPolygon(centroid, gui[i], gui[(i + 1) % gui.Length]);
    }

    private static Vector3[] CloseLoop(Vector3[] gui)
    {
        Vector3[] loop = new Vector3[gui.Length + 1];

        gui.CopyTo(loop, 0);
        loop[gui.Length] = gui[0];
        return loop;
    }

    // Texture-space UVs of a sprite's packed region, mirrored on X when flipped.
    private static Rect SpriteUV(Sprite sprite, bool flip)
    {
        Texture2D texture = sprite.texture;
        Rect region = sprite.textureRect;
        Rect uv = new Rect(region.x / texture.width, region.y / texture.height, region.width / texture.width, region.height / texture.height);

        if (flip)
            uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
        return uv;
    }

    private static void DrawTexture(Rect rect, Texture2D texture, Rect uv, Vector2 pivotGui, float rotationDegrees, Color tint)
    {
        Matrix4x4 previous = GUI.matrix;
        Color previousColor = GUI.color;

        if (Mathf.Abs(rotationDegrees) > MinRotation)
            GUIUtility.RotateAroundPivot(-rotationDegrees, pivotGui);
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(rect, texture, uv);
        GUI.color = previousColor;
        GUI.matrix = previous;
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

    public void Line(Vector2 from, Vector2 to, Color color, float width = OutlineWidth)
    {
        Handles.color = color;
        Handles.DrawAAPolyLine(width, ToGui(from), ToGui(to));
    }

    // Fan-filled from the centroid, so concave star-shaped hitboxes still read correctly.
    public void Outline(Vector2[] worldPoints, bool closed, Color fill, Color edge)
    {
        Vector3[] gui;

        if (worldPoints.Length < 2)
            return;
        gui = new Vector3[worldPoints.Length];
        for (int i = 0; i < worldPoints.Length; i++)
            gui[i] = ToGui(worldPoints[i]);
        if (closed && fill.a > 0f)
            FillFan(gui, fill);
        Handles.color = edge;
        if (closed)
            Handles.DrawAAPolyLine(OutlineWidth, CloseLoop(gui));
        else
            Handles.DrawAAPolyLine(OutlineWidth, gui);
    }

    // Draws a sprite with its pivot on a world position, scaled and rotated (degrees, counter-clockwise).
    public void Sprite(Sprite sprite, Vector2 worldPivot, Vector2 worldScale, float rotationDegrees, bool flipX, Color tint)
    {
        float unitX;
        float unitY;
        Vector2 regionSize;
        Vector2 regionOffset;
        Vector2 pivot;
        Vector2 pivotGui;
        bool flip;
        Rect rect;

        if (sprite == null)
            return;
        unitX = Mathf.Abs(worldScale.x) / sprite.pixelsPerUnit * Scale;
        unitY = Mathf.Abs(worldScale.y) / sprite.pixelsPerUnit * Scale;
        regionSize = sprite.textureRect.size;
        regionOffset = sprite.textureRectOffset;
        pivot = sprite.pivot;
        flip = flipX ^ (worldScale.x < 0f);
        if (flip)
        {
            regionOffset.x = sprite.rect.width - regionOffset.x - regionSize.x;
            pivot.x = sprite.rect.width - pivot.x;
        }
        pivotGui = ToGui(worldPivot);
        rect = new Rect(pivotGui.x + (regionOffset.x - pivot.x) * unitX, pivotGui.y - (regionOffset.y + regionSize.y - pivot.y) * unitY, regionSize.x * unitX, regionSize.y * unitY);
        DrawTexture(rect, sprite.texture, SpriteUV(sprite, flip), pivotGui, rotationDegrees, tint);
    }

    // Same, placed by a renderer's world matrix (mirrored matrices flip the sprite).
    public void Sprite(Sprite sprite, Matrix4x4 rendererToWorld, bool flipX, Color tint)
    {
        Vector2 x = rendererToWorld.GetColumn(0);
        Vector2 y = rendererToWorld.GetColumn(1);
        float determinant = x.x * y.y - x.y * y.x;
        float rotation = Mathf.Atan2(x.y, x.x) * Mathf.Rad2Deg;

        if (determinant < 0f)
            rotation += MirroredRotation;
        Sprite(sprite, rendererToWorld.MultiplyPoint3x4(Vector3.zero), new Vector2(x.magnitude * Mathf.Sign(determinant), y.magnitude), rotation, flipX, tint);
    }

    // A prefab's main sprite with the prefab root pivot at worldRoot, as its renderer sits under the root.
    public void PrefabSprite(GameObject prefab, Sprite sprite, Vector2 worldRoot, bool flipX, Color tint)
    {
        SpriteRenderer renderer = null;
        Vector2 offset;

        if (prefab != null)
            renderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer == null)
            return;
        offset = renderer.transform.position - prefab.transform.position;
        if (sprite == null)
            sprite = renderer.sprite;
        Sprite(sprite, worldRoot + offset, renderer.transform.lossyScale, 0f, flipX, tint);
    }
}
