using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Reusable playable preview: playback state, a timeline bar and a zoomable, pannable canvas with a unit grid.
public partial class PreviewPanel
{
    private static readonly float[] Speeds = { 0.1f, 0.25f, 0.5f, 1f };
    private static readonly string[] SpeedLabels = { "x0.1", "x0.25", "x0.5", "x1" };
    private static readonly Color Background = new Color(0.13f, 0.14f, 0.16f, 1f);
    private static readonly Color GridColor = new Color(1f, 1f, 1f, 0.06f);
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 8f;
    private const float MinLength = 0.01f;
    private const float MinGridScale = 6f;
    private const float BarHeight = 18f;
    private const float MinRectSize = 10f;
    private const float SpanTop = 0.35f;
    private const float SpanHeight = 0.3f;
    private const float MarkerWidth = 2f;
    public const float HandleRadius = 5f;

    public bool Playing;
    public float Time;
    public float Zoom = 1f;
    public Vector2 Pan;
    public float LockedScale = -1f;
    private int speedIndex = 3;

    private void DrawControls(float length)
    {
        string playLabel = "Play";

        if (Playing)
            playLabel = "Pause";
        if (GUILayout.Button(playLabel, GUILayout.Width(60f)))
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

    private static float BarX(Rect bar, float time, float length)
    {
        return bar.x + Mathf.Clamp01(time / length) * bar.width;
    }

    private static void DrawSpans(Rect bar, float length, IList<(float start, float end, Color color)> spans)
    {
        float x0;
        float x1;

        foreach ((float start, float end, Color color) in spans)
        {
            x0 = BarX(bar, start, length);
            x1 = BarX(bar, end, length);
            EditorGUI.DrawRect(new Rect(x0, bar.y + bar.height * SpanTop, Mathf.Max(1f, x1 - x0), bar.height * SpanHeight), color);
        }
    }

    private static void DrawMarkers(Rect bar, float length, IList<(float time, Color color)> markers)
    {
        float x;

        foreach ((float time, Color color) in markers)
        {
            x = BarX(bar, time, length);
            EditorGUI.DrawRect(new Rect(x - MarkerWidth * 0.5f, bar.y, MarkerWidth, bar.height), color);
        }
    }

    // Scrubbing pauses playback.
    private void Scrub(Rect bar, float length)
    {
        float scrubbed = GUI.HorizontalSlider(bar, Time, 0f, length);

        if (Mathf.Approximately(scrubbed, Time))
            return;
        Time = scrubbed;
        Playing = false;
    }

    // One line per world unit, skipped when zoomed out too far to read.
    private static void DrawGrid(Vector2 size, Vector2 origin, float scale)
    {
        if (scale < MinGridScale)
            return;
        for (float x = Mathf.Repeat(origin.x, scale); x < size.x; x += scale)
            EditorGUI.DrawRect(new Rect(x, 0f, 1f, size.y), GridColor);
        for (float y = Mathf.Repeat(origin.y, scale); y < size.y; y += scale)
            EditorGUI.DrawRect(new Rect(0f, y, size.x, 1f), GridColor);
    }

    // Advances a playing preview and loops it; returns true when it needs a repaint.
    public bool Tick(float delta, float length)
    {
        if (!Playing)
            return false;
        Time = Mathf.Repeat(Time + delta * Speeds[speedIndex], Mathf.Max(length, MinLength));
        return true;
    }

    // Play/pause, speed, time, zoom and Fit on one row, then a scrub bar with colored markers and optional highlighted spans (seconds).
    public void DrawTimeline(float length, IList<(float time, Color color)> markers, IList<(float start, float end, Color color)> spans = null)
    {
        Rect bar;
        bool repaint;

        Time = Mathf.Min(Time, length);
        using (new EditorGUILayout.HorizontalScope())
            DrawControls(length);
        bar = GUILayoutUtility.GetRect(MinRectSize, BarHeight, GUILayout.ExpandWidth(true));
        repaint = Event.current.type == EventType.Repaint;
        if (repaint && spans != null)
            DrawSpans(bar, length, spans);
        Scrub(bar, length);
        if (repaint && markers != null)
            DrawMarkers(bar, length, markers);
    }

    // Reserves the canvas rect, with its caption underneath.
    public static Rect Reserve(float height, string caption)
    {
        Rect rect = GUILayoutUtility.GetRect(MinRectSize, height, GUILayout.ExpandWidth(true));

        EditorGUILayout.LabelField(caption, EditorStyles.wordWrappedMiniLabel);
        return rect;
    }

    // Scale that shows extent world units from the rect's center to its nearest edge.
    public static float FitCentered(Rect rect, float extent)
    {
        return Mathf.Min(rect.width, rect.height) * 0.5f / Mathf.Max(extent, MinLength);
    }

    // Current pixels per world unit; frozen while a custom handle drags.
    public float ScaleFor(float fitScale)
    {
        if (LockedScale > 0f)
            return LockedScale;
        return fitScale * Zoom;
    }

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

    public static void End()
    {
        GUI.EndClip();
    }
}
