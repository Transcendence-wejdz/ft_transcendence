using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Reusable playable preview: playback state, a timeline bar and a zoomable, pannable canvas with a unit grid.
public class PreviewPanel
{
    private static readonly float[] Speeds = { 0.1f, 0.25f, 0.5f, 1f };
    private static readonly string[] SpeedLabels = { "x0.1", "x0.25", "x0.5", "x1" };
    private static readonly Color Background = new Color(0.13f, 0.14f, 0.16f, 1f);
    private static readonly Color GridColor = new Color(1f, 1f, 1f, 0.06f);
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 8f;
    public const float HandleRadius = 5f;

    public bool Playing;
    public float Time;
    public float Zoom = 1f;
    public Vector2 Pan;
    public float LockedScale = -1f;
    private int speedIndex = 3;

    // Advances a playing preview and loops it; returns true when it needs a repaint.
    public bool Tick(float delta, float length)
    {
        if (!Playing)
            return false;
        Time = Mathf.Repeat(Time + delta * Speeds[speedIndex], Mathf.Max(length, 0.01f));
        return true;
    }

    // Play/pause, speed, time, zoom and Fit on one row, then a scrub bar with colored markers and optional highlighted spans (seconds).
    public void DrawTimeline(float length, IList<(float time, Color color)> markers, IList<(float start, float end, Color color)> spans = null)
    {
        Time = Mathf.Min(Time, length);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(Playing ? "Pause" : "Play", GUILayout.Width(60f)))
                Playing = !Playing;
            speedIndex = GUILayout.Toolbar(speedIndex, SpeedLabels, GUILayout.Width(200f));
            GUILayout.Label($"{Time:0.00}s / {length:0.00}s", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Zoom x{Zoom:0.##}", EditorStyles.miniLabel, GUILayout.Width(70f));
            if (GUILayout.Button("Fit", GUILayout.Width(40f)))
            {
                Zoom = 1f;
                Pan = Vector2.zero;
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

        float scrubbed = GUI.HorizontalSlider(bar, Time, 0f, length);
        if (!Mathf.Approximately(scrubbed, Time))
        {
            Time = scrubbed;
            Playing = false;
        }
        if (Event.current.type != EventType.Repaint || markers == null)
            return;

        foreach ((float time, Color color) in markers)
        {
            float x = bar.x + Mathf.Clamp01(time / length) * bar.width;
            EditorGUI.DrawRect(new Rect(x - 1f, bar.y, 2f, bar.height), color);
        }
    }

    // Reserves the canvas rect, with its caption underneath.
    public static Rect Reserve(float height, string caption)
    {
        Rect rect = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField(caption, EditorStyles.wordWrappedMiniLabel);
        return rect;
    }

    // Scale that shows extent world units from the rect's center to its nearest edge.
    public static float FitCentered(Rect rect, float extent) => Mathf.Min(rect.width, rect.height) * 0.5f / Mathf.Max(extent, 0.01f);

    // Current pixels per world unit; frozen while a custom handle drags.
    public float ScaleFor(float fitScale) => LockedScale > 0f ? LockedScale : fitScale * Zoom;

    // Handles zoom and pan, then on Repaint clears and clips the canvas and draws the grid; call End after drawing when it returns true.
    public bool Begin(Rect rect, Vector2 anchor, float fitScale, System.Action repaint, out PreviewCanvas canvas)
    {
        HandleZoomPan(rect, anchor, fitScale, repaint);
        canvas = new PreviewCanvas(anchor + Pan - rect.position, ScaleFor(fitScale));
        if (Event.current.type != EventType.Repaint)
            return false;

        EditorGUI.DrawRect(rect, Background);
        GUI.BeginClip(rect);
        DrawGrid(rect.size, canvas.Origin, canvas.Scale);
        return true;
    }

    public static void End() => GUI.EndClip();

    // Draggable world point bound to a Vector2 property (snapped to 0.05, view frozen while dragging), right-click resets it; mirrorX shows and edits it flipped on X. Call before Begin.
    public void DragPoint(Rect rect, Vector2 anchor, float fitScale, SerializedProperty property, bool mirrorX, System.Action repaint)
    {
        float scale = ScaleFor(fitScale);
        Vector2 worldZero = anchor + Pan;
        Vector2 value = property.vector2Value;
        Vector2 gui = worldZero + new Vector2(mirrorX ? -value.x : value.x, -value.y) * scale;
        int id = GUIUtility.GetControlID(FocusType.Passive, rect);
        Event e = Event.current;
        float grab = HandleRadius + 4f;

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if ((e.mousePosition - gui).sqrMagnitude > grab * grab || !rect.Contains(e.mousePosition))
                    break;
                if (e.button == 1)
                {
                    property.vector2Value = Vector2.zero;
                    e.Use();
                    break;
                }
                GUIUtility.hotControl = id;
                LockedScale = scale;
                e.Use();
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl != id)
                    break;
                Vector2 local = (e.mousePosition - worldZero) / scale;
                property.vector2Value = new Vector2(Snap(mirrorX ? -local.x : local.x), Snap(-local.y));
                e.Use();
                repaint();
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    LockedScale = -1f;
                    e.Use();
                }
                break;
            case EventType.Repaint:
                EditorGUIUtility.AddCursorRect(new Rect(gui.x - HandleRadius, gui.y - HandleRadius, HandleRadius * 2f, HandleRadius * 2f), MouseCursor.MoveArrow);
                break;
        }
    }

    private static float Snap(float value) => Mathf.Round(value / 0.05f) * 0.05f;

    // Scroll zooms around the cursor (the point under it stays put), left or middle drag on the background pans.
    private void HandleZoomPan(Rect rect, Vector2 anchor, float fitScale, System.Action repaint)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive, rect);
        Event e = Event.current;
        switch (e.GetTypeForControl(id))
        {
            case EventType.ScrollWheel:
                if (!rect.Contains(e.mousePosition))
                    break;
                float oldScale = fitScale * Zoom;
                Zoom = Mathf.Clamp(Zoom * (1f - e.delta.y * 0.05f), MinZoom, MaxZoom);
                float ratio = fitScale * Zoom / oldScale;
                Vector2 world = anchor + Pan;
                Pan = e.mousePosition - (e.mousePosition - world) * ratio - anchor;
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
                Pan += e.delta;
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
    private static void DrawGrid(Vector2 size, Vector2 origin, float scale)
    {
        if (scale < 6f)
            return;
        for (float x = Mathf.Repeat(origin.x, scale); x < size.x; x += scale)
            EditorGUI.DrawRect(new Rect(x, 0f, 1f, size.y), GridColor);
        for (float y = Mathf.Repeat(origin.y, scale); y < size.y; y += scale)
            EditorGUI.DrawRect(new Rect(0f, y, size.x, 1f), GridColor);
    }
}
