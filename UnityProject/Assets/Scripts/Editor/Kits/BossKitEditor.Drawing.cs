using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Draws a boss preview: options, timeline, range rings and the telegraphs and hits of the current step.
public partial class BossKitEditor
{
    private const float PreviewHeight = 260f;
    private const float FlashSeconds = 0.15f;
    private const float PlayerRadius = 0.3f;
    private const float MinExtent = 2f;
    private const float FitMargin = 1.1f;
    private const float ZoneAlpha = 0.15f;
    private const float ProgressAlpha = 0.4f;
    private const float EdgeThickness = 2f;
    private const string PreviewCaption = "Rings: charge min range (white), slam max range (blue), around the boss start. Blue dot: target. Red: telegraph then hit; purple: summon area and spawns. Timeline: step starts (white), hits (red/purple spans). Scroll to zoom, drag to pan.";
    private static readonly Color ZoneFill = new Color(1f, 0.25f, 0.15f, 0.15f);
    private static readonly Color ProgressFill = new Color(1f, 0.25f, 0.15f, 0.4f);
    private static readonly Color FlashFill = new Color(1f, 0.92f, 0.8f, 0.6f);
    private static readonly Color RangeColor = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color MeleeColor = new Color(0.35f, 0.75f, 1f, 0.6f);
    private static readonly Color SummonColor = new Color(0.75f, 0.45f, 1f, 0.9f);
    private static readonly Color PhaseColor = new Color(1f, 1f, 1f, 0.6f);

    private void DrawOptions(Preview preview)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            enraged = EditorGUILayout.ToggleLeft($"Enraged (x{Kit.EnrageTempo:0.##})", enraged, GUILayout.Width(140f));
            if (preview == Preview.Combo)
                combo = (Combo)GUILayout.Toolbar((int)combo, ComboLabels);
        }
    }

    // Red span over each hit (the whole run for a charge), purple for summons.
    private static (float, float, Color) HitSpan(Step step)
    {
        float hitStart = step.Start + step.Windup;

        if (step.Pattern == BossPattern.Charge)
            return (hitStart, hitStart + step.Active, PreviewColors.HitEdge);
        if (step.Pattern == BossPattern.Summon)
            return (hitStart, hitStart + FlashSeconds, SummonColor);
        return (hitStart, hitStart + FlashSeconds, PreviewColors.HitEdge);
    }

    private static void DrawTimeline(PreviewPanel panel, List<Step> steps)
    {
        List<(float, Color)> markers = new List<(float, Color)>();
        List<(float, float, Color)> spans = new List<(float, float, Color)>();

        foreach (Step step in steps)
        {
            markers.Add((step.Start, PhaseColor));
            spans.Add(HitSpan(step));
        }
        panel.DrawTimeline(TimelineLength(steps), markers, spans);
    }

    private float PreviewExtent(List<Step> steps, Vector2 target)
    {
        float extent = Mathf.Max(Kit.SlamRadius, Kit.SummonRadius, Kit.ChargeMinRange, target.magnitude, MinExtent);

        foreach (Step step in steps)
            extent = Mathf.Max(extent, (step.From + step.Direction * step.Length).magnitude + ChargeHitWidth * 0.5f);
        return extent;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    // Area, telegraph fill during the windup, then a short flash when it lands.
    private static void DrawCircle(PreviewCanvas canvas, Vector2 center, float radius, bool windup, float progress, float sinceHit, Color edge)
    {
        canvas.Disc(center, radius, WithAlpha(edge, ZoneAlpha));
        canvas.WireDisc(center, radius, edge, EdgeThickness);
        if (windup)
            canvas.Disc(center, radius * progress, WithAlpha(edge, ProgressAlpha));
        else if (sinceHit < FlashSeconds)
            canvas.Disc(center, radius, FlashFill);
    }

    private static Vector2[] Lane(Vector2 center, Vector2 side, Vector2 length)
    {
        return new[] { center - side, center + side, center + side + length, center - side + length };
    }

    // Lane, filled up to the windup progress, then a flash on the body while it runs.
    private void DrawCharge(PreviewCanvas canvas, Step step, float local, float progress, Vector2 body)
    {
        float width = ChargeHitWidth;
        Vector2 side = new Vector2(-step.Direction.y, step.Direction.x) * width * 0.5f;
        Vector2 length = step.Direction * step.Length;

        canvas.Outline(Lane(step.From, side, length), true, ZoneFill, PreviewColors.HitEdge);
        if (local < step.Windup)
            canvas.Outline(Lane(step.From, side, length * progress), true, ProgressFill, Color.clear);
        else if (local < step.Windup + step.Active)
            canvas.Disc(body, width * 0.5f, FlashFill);
    }

    private void DrawStep(PreviewCanvas canvas, Step step, float time, Vector2 body)
    {
        float local = time - step.Start;
        float progress = 1f;
        bool windup;

        if (local < 0f || local > step.Windup + step.Active + FlashSeconds)
            return;
        windup = local < step.Windup;
        if (step.Windup > 0f)
            progress = Mathf.Clamp01(local / step.Windup);
        if (step.Pattern == BossPattern.Charge)
            DrawCharge(canvas, step, local, progress, body);
        else if (step.Pattern == BossPattern.Slam)
            DrawCircle(canvas, step.From, Kit.SlamRadius, windup, progress, local - step.Windup, PreviewColors.HitEdge);
        else if (step.Pattern == BossPattern.Summon)
        {
            DrawCircle(canvas, step.From, Kit.SummonRadius, windup, progress, local - step.Windup, SummonColor);
            if (!windup)
                DrawSummons(canvas, step.From);
        }
    }

    private void DrawScene(PreviewCanvas canvas, List<Step> steps, float time, Vector2 target)
    {
        Step current = StepAt(steps, time);
        Vector2 body = BodyAt(steps, time);
        bool inCombo = steps.Count > 0 && time < steps[steps.Count - 1].End;

        canvas.WireDisc(Vector2.zero, Kit.ChargeMinRange, RangeColor);
        canvas.WireDisc(Vector2.zero, Kit.SlamMaxRange, MeleeColor);
        DrawStep(canvas, current, time, body);
        DrawBoss(canvas, current, time, body, inCombo);
        canvas.Disc(target, PlayerRadius, PreviewColors.Player);
    }

    private void DrawPreview(Preview preview)
    {
        PreviewPanel panel = previews[(int)preview];
        List<Step> steps;
        Vector2 target;
        Rect rect;
        PreviewCanvas canvas;

        if (!InspectorSections.Foldout("BossKitEditor.Preview." + preview, preview + " Preview"))
            return;
        DrawOptions(preview);
        steps = BuildSteps(preview);
        DrawTimeline(panel, steps);
        rect = PreviewPanel.Reserve(PreviewHeight, PreviewCaption);
        target = TargetFor(preview);
        if (!panel.Begin(rect, rect.center, PreviewPanel.FitCentered(rect, PreviewExtent(steps, target) * FitMargin), Repaint, out canvas))
            return;
        DrawScene(canvas, steps, panel.Time, target);
        PreviewPanel.End();
    }
}
