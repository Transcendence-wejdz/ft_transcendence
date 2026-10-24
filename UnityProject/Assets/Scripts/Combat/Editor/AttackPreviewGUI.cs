using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Playback, view and timeline state of one attack preview.
public class AttackPreviewState
{
    public bool Playing;
    public float Time;
    public int SpeedIndex = 3;
    public float Zoom = 1f;
    public Vector2 Pan;
    public float LockedScale = -1f;
}

// Shared drawing helpers for the EnemyKit and ClassKit attack previews: timeline, zoom/pan, grid, sprites, colliders and animation sampling.
public static class AttackPreviewGUI
{
    public static readonly float[] Speeds = { 0.1f, 0.25f, 0.5f, 1f };
    public static readonly string[] SpeedLabels = { "x0.1", "x0.25", "x0.5", "x1" };
    public static readonly Color Background = new Color(0.13f, 0.14f, 0.16f, 1f);
    public static readonly Color GridColor = new Color(1f, 1f, 1f, 0.06f);
    public const float MinZoom = 0.25f;
    public const float MaxZoom = 8f;

    // Advances a playing preview and loops it; returns true when it needs a repaint.
    public static bool Tick(AttackPreviewState state, float delta, float length)
    {
        if (!state.Playing)
            return false;
        state.Time = Mathf.Repeat(state.Time + delta * Speeds[state.SpeedIndex], Mathf.Max(length, 0.01f));
        return true;
    }

    // Play/pause, speed, time, zoom and Fit on one row, then a scrub bar with colored markers (time in seconds) and optional highlighted spans.
    public static void DrawTimeline(AttackPreviewState state, float length, IList<(float time, Color color)> markers, IList<(float start, float end, Color color)> spans = null)
    {
        state.Time = Mathf.Min(state.Time, length);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(state.Playing ? "Pause" : "Play", GUILayout.Width(60f)))
                state.Playing = !state.Playing;
            state.SpeedIndex = GUILayout.Toolbar(state.SpeedIndex, SpeedLabels, GUILayout.Width(200f));
            GUILayout.Label($"{state.Time:0.00}s / {length:0.00}s", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Zoom x{state.Zoom:0.##}", EditorStyles.miniLabel, GUILayout.Width(70f));
            if (GUILayout.Button("Fit", GUILayout.Width(40f)))
            {
                state.Zoom = 1f;
                state.Pan = Vector2.zero;
            }
        }

        Rect bar = GUILayoutUtility.GetRect(10f, 18f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint && spans != null)
        {
            foreach ((float start, float end, Color color) in spans)
            {
                float x0 = bar.x + Mathf.Clamp01(start / length) * bar.width;
                float x1 = bar.x + Mathf.Clamp01(end / length) * bar.width;
                EditorGUI.DrawRect(new Rect(x0, bar.y + bar.height * 0.35f, Mathf.Max(1f, x1 - x0), bar.height * 0.3f), color);
            }
        }

        float scrubbed = GUI.HorizontalSlider(bar, state.Time, 0f, length);
        if (!Mathf.Approximately(scrubbed, state.Time))
        {
            state.Time = scrubbed;
            state.Playing = false;
        }
        if (Event.current.type != EventType.Repaint || markers == null)
            return;

        foreach ((float time, Color color) in markers)
        {
            float x = bar.x + Mathf.Clamp01(time / length) * bar.width;
            EditorGUI.DrawRect(new Rect(x - 1f, bar.y, 2f, bar.height), color);
        }
    }

    public static float Scale(AttackPreviewState state, float fitScale) => state.LockedScale > 0f ? state.LockedScale : fitScale * state.Zoom;

    // Scroll zooms around the cursor (the point under it stays put), left or middle drag on the background pans; call after any handle with priority.
    public static void HandleZoomPan(AttackPreviewState state, Rect rect, Vector2 anchor, float fitScale, System.Action repaint)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive, rect);
        Event e = Event.current;
        switch (e.GetTypeForControl(id))
        {
            case EventType.ScrollWheel:
                if (!rect.Contains(e.mousePosition))
                    break;
                float oldScale = fitScale * state.Zoom;
                state.Zoom = Mathf.Clamp(state.Zoom * (1f - e.delta.y * 0.05f), MinZoom, MaxZoom);
                float ratio = fitScale * state.Zoom / oldScale;
                Vector2 body = anchor + state.Pan;
                state.Pan = e.mousePosition - (e.mousePosition - body) * ratio - anchor;
                e.Use();
                repaint();
                break;
            case EventType.MouseDown:
                if ((e.button != 0 && e.button != 2) || !rect.Contains(e.mousePosition))
                    break;
                GUIUtility.hotControl = id;
                e.Use();
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl != id)
                    break;
                state.Pan += e.delta;
                e.Use();
                repaint();
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;
        }
    }

    // One line per world unit, skipped when zoomed out too far to read.
    public static void DrawGrid(Vector2 size, Vector2 origin, float scale)
    {
        if (scale < 6f)
            return;
        for (float x = Mathf.Repeat(origin.x, scale); x < size.x; x += scale)
            EditorGUI.DrawRect(new Rect(x, 0f, 1f, size.y), GridColor);
        for (float y = Mathf.Repeat(origin.y, scale); y < size.y; y += scale)
            EditorGUI.DrawRect(new Rect(0f, y, size.x, 1f), GridColor);
    }

    // World (x right, y up) to preview GUI (y down) around a GUI-space origin.
    public static Vector2 ToGui(Vector2 origin, Vector2 world, float scale) => origin + new Vector2(world.x, -world.y) * scale;

    // Draws a sprite whose pivot lands on a world position, with a world scale and rotation (degrees, counter-clockwise); only the trimmed texture region is drawn, placed at its offset inside the sprite rect.
    public static void DrawSprite(Sprite sprite, Vector2 origin, float scale, Vector2 worldPivot, Vector2 worldScale, float rotationDegrees, bool flipX, Color tint)
    {
        if (sprite == null)
            return;

        float unitX = Mathf.Abs(worldScale.x) / sprite.pixelsPerUnit * scale;
        float unitY = Mathf.Abs(worldScale.y) / sprite.pixelsPerUnit * scale;
        Vector2 regionSize = sprite.textureRect.size;
        Vector2 regionOffset = sprite.textureRectOffset;
        Vector2 pivot = sprite.pivot;
        bool flip = flipX ^ (worldScale.x < 0f);
        if (flip)
        {
            regionOffset.x = sprite.rect.width - regionOffset.x - regionSize.x;
            pivot.x = sprite.rect.width - pivot.x;
        }

        Vector2 pivotGui = ToGui(origin, worldPivot, scale);
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

    // Fan-filled from the centroid, so concave star-shaped hitboxes still read correctly.
    public static void DrawOutline(Vector2[] worldPoints, bool closed, Vector2 origin, float scale, Color fill, Color edge)
    {
        if (worldPoints.Length < 2)
            return;

        var gui = new Vector3[worldPoints.Length];
        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < worldPoints.Length; i++)
        {
            gui[i] = ToGui(origin, worldPoints[i], scale);
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
        if (closed)
        {
            var loop = new Vector3[gui.Length + 1];
            gui.CopyTo(loop, 0);
            loop[gui.Length] = gui[0];
            Handles.DrawAAPolyLine(2f, loop);
        }
        else
        {
            Handles.DrawAAPolyLine(2f, gui);
        }
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

    // World outline of a capsule (e.g. a Physics2D.OverlapCapsule query) centered at center and rotated by angleDegrees.
    public static Vector2[] CapsuleOutline(Vector2 center, Vector2 size, CapsuleDirection2D direction, float angleDegrees)
    {
        return Transform(Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, angleDegrees), Vector3.one), CapsulePoints(Vector2.zero, size, direction));
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

    // Base controller plus override clip lookup, so preview code reads the same clips the Animator plays.
    public static AnimatorController ResolveController(RuntimeAnimatorController runtime, out AnimatorOverrideController overrides)
    {
        overrides = runtime as AnimatorOverrideController;
        return (overrides != null ? overrides.runtimeAnimatorController : runtime) as AnimatorController;
    }

    public static AnimatorState FindState(AnimatorController controller, string stateName)
    {
        if (controller == null || controller.layers.Length == 0)
            return null;
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
        {
            if (child.state.name == stateName)
                return child.state;
        }
        return null;
    }

    // A state's clip, or for a 2D blend tree the child whose position points closest to direction.
    public static AnimationClip ClipFor(AnimatorState state, AnimatorOverrideController overrides, Vector2 direction)
    {
        if (state == null)
            return null;

        AnimationClip clip = state.motion as AnimationClip;
        if (clip == null && state.motion is BlendTree tree)
        {
            float best = float.MinValue;
            foreach (ChildMotion child in tree.children)
            {
                AnimationClip candidate = child.motion as AnimationClip;
                if (candidate == null)
                    continue;
                float score = child.position.sqrMagnitude < 0.0001f ? -2f : Vector2.Dot(child.position.normalized, direction.normalized);
                if (score > best)
                {
                    best = score;
                    clip = candidate;
                }
            }
        }
        if (clip != null && overrides != null && overrides[clip] != null)
            clip = overrides[clip];
        return clip;
    }

    public static ObjectReferenceKeyframe[] SpriteKeys(AnimationClip clip)
    {
        if (clip == null)
            return null;
        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite")
                return AnimationUtility.GetObjectReferenceCurve(clip, binding);
        }
        return null;
    }

    // Length of a sprite-key clip including its last frame's display time.
    public static float FramesLength(ObjectReferenceKeyframe[] frames)
    {
        if (frames == null || frames.Length == 0)
            return 0f;
        float last = frames[frames.Length - 1].time;
        float step = frames.Length > 1 ? last / (frames.Length - 1) : 0f;
        return last + step;
    }

    public static Sprite SampleFrames(ObjectReferenceKeyframe[] frames, float t, bool loop)
    {
        if (frames == null || frames.Length == 0)
            return null;

        float clipLength = frames[frames.Length - 1].time;
        if (clipLength > 0f)
            t = loop ? Mathf.Repeat(t, FramesLength(frames)) : Mathf.Min(t, clipLength);

        Sprite sprite = frames[0].value as Sprite;
        foreach (ObjectReferenceKeyframe frame in frames)
        {
            if (frame.time > t)
                break;
            sprite = frame.value as Sprite;
        }
        return sprite;
    }

    // Flipbook frame at time t for a sprite array played at frameRate; null once a non-looping one has finished.
    public static Sprite SampleSprites(Sprite[] frames, float frameRate, float t, bool loop)
    {
        if (frames == null || frames.Length == 0 || frameRate <= 0f)
            return null;
        int index = Mathf.FloorToInt(t * frameRate);
        if (loop)
            return frames[((index % frames.Length) + frames.Length) % frames.Length];
        return index >= 0 && index < frames.Length ? frames[index] : null;
    }
}
