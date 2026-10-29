using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Draws a boss preview: range rings, telegraphs and hits of the current step, summons and the animated boss sprite.
public partial class BossKitEditor
{
    private const float PreviewHeight = 260f;
    private const float FlashSeconds = 0.15f;
    private const float PlayerRadius = 0.3f;
    private static readonly Color ZoneFill = new Color(1f, 0.25f, 0.15f, 0.15f);
    private static readonly Color ProgressFill = new Color(1f, 0.25f, 0.15f, 0.4f);
    private static readonly Color FlashFill = new Color(1f, 0.92f, 0.8f, 0.6f);
    private static readonly Color RangeColor = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color MeleeColor = new Color(0.35f, 0.75f, 1f, 0.6f);
    private static readonly Color SummonColor = new Color(0.75f, 0.45f, 1f, 0.9f);
    private static readonly Color PhaseColor = new Color(1f, 1f, 1f, 0.6f);
    private static readonly Color SpriteTint = new Color(1f, 1f, 1f, 0.85f);

    private void DrawPreview(Preview preview)
    {
        if (!InspectorSections.Foldout("BossKitEditor.Preview." + preview, preview + " Preview"))
            return;

        PreviewPanel panel = previews[(int)preview];
        using (new EditorGUILayout.HorizontalScope())
        {
            enraged = EditorGUILayout.ToggleLeft($"Enraged (x{Kit.EnrageTempo:0.##})", enraged, GUILayout.Width(140f));
            if (preview == Preview.Combo)
                combo = (Combo)GUILayout.Toolbar((int)combo, ComboLabels);
        }

        List<Step> steps = BuildSteps(preview);
        var markers = new List<(float, Color)>();
        var spans = new List<(float, float, Color)>();
        foreach (Step step in steps)
        {
            markers.Add((step.Start, PhaseColor));
            spans.Add((step.Start + step.Windup, step.Start + step.Windup + (step.Pattern == BossPattern.Charge ? step.Active : FlashSeconds), step.Pattern == BossPattern.Summon ? SummonColor : PreviewColors.HitEdge));
        }
        panel.DrawTimeline(TimelineLength(steps), markers, spans);

        Rect rect = PreviewPanel.Reserve(PreviewHeight, "Rings: charge min range (white), slam max range (blue), around the boss start. Blue dot: target. Red: telegraph then hit; purple: summon area and spawns. Timeline: step starts (white), hits (red/purple spans). Scroll to zoom, drag to pan.");
        Vector2 target = TargetFor(preview);
        float extent = Mathf.Max(Kit.SlamRadius, Kit.SummonRadius, Kit.ChargeMinRange, target.magnitude, 2f);
        foreach (Step step in steps)
            extent = Mathf.Max(extent, (step.From + step.Direction * step.Length).magnitude + ChargeHitWidth * 0.5f);

        if (!panel.Begin(rect, rect.center, PreviewPanel.FitCentered(rect, extent * 1.1f), Repaint, out PreviewCanvas canvas))
            return;

        canvas.WireDisc(Vector2.zero, Kit.ChargeMinRange, RangeColor);
        canvas.WireDisc(Vector2.zero, Kit.SlamMaxRange, MeleeColor);

        float time = panel.Time;
        Step current = StepAt(steps, time);
        Vector2 body = BodyAt(steps, time);
        DrawStep(canvas, current, time, body);
        DrawBoss(canvas, current, time, body, steps.Count > 0 && time < steps[steps.Count - 1].End);
        canvas.Disc(target, PlayerRadius, PreviewColors.Player);
        PreviewPanel.End();
    }

    private void DrawStep(PreviewCanvas canvas, Step step, float time, Vector2 body)
    {
        float local = time - step.Start;
        if (local < 0f || local > step.Windup + step.Active + FlashSeconds)
            return;
        bool windup = local < step.Windup;
        float progress = step.Windup > 0f ? Mathf.Clamp01(local / step.Windup) : 1f;
        Vector2 center = step.From;

        switch (step.Pattern)
        {
            case BossPattern.Charge:
            {
                float width = ChargeHitWidth;
                Vector2 side = new Vector2(-step.Direction.y, step.Direction.x) * width * 0.5f;
                Vector2 length = step.Direction * step.Length;
                canvas.Outline(new[] { center - side, center + side, center + side + length, center - side + length }, true, ZoneFill, PreviewColors.HitEdge);
                if (windup)
                {
                    Vector2 reach = length * progress;
                    canvas.Outline(new[] { center - side, center + side, center + side + reach, center - side + reach }, true, ProgressFill, Color.clear);
                }
                else if (local < step.Windup + step.Active)
                {
                    canvas.Disc(body, width * 0.5f, FlashFill);
                }
                break;
            }
            case BossPattern.Slam:
                DrawCircle(canvas, center, Kit.SlamRadius, windup, progress, local - step.Windup, PreviewColors.HitEdge);
                break;
            case BossPattern.Summon:
                DrawCircle(canvas, center, Kit.SummonRadius, windup, progress, local - step.Windup, SummonColor);
                if (!windup)
                    DrawSummons(canvas, center);
                break;
        }
    }

    // Area, telegraph fill during the windup, then a short flash when it lands.
    private static void DrawCircle(PreviewCanvas canvas, Vector2 center, float radius, bool windup, float progress, float sinceHit, Color edge)
    {
        canvas.Disc(center, radius, new Color(edge.r, edge.g, edge.b, 0.15f));
        canvas.WireDisc(center, radius, edge, 2f);
        if (windup)
            canvas.Disc(center, radius * progress, new Color(edge.r, edge.g, edge.b, 0.4f));
        else if (sinceHit < FlashSeconds)
            canvas.Disc(center, radius, FlashFill);
    }

    // Spawn spots are random in game; a fixed golden-angle spread shows the count and area, with each summoned kit's sprite.
    private void DrawSummons(PreviewCanvas canvas, Vector2 center)
    {
        EnemyKit[] kits = Kit.SummonKits;
        for (int i = 0; i < Kit.SummonCount; i++)
        {
            float angle = i * 2.39996f;
            float distance = Kit.SummonRadius * Mathf.Sqrt((i + 0.5f) / Mathf.Max(1, Kit.SummonCount));
            Vector2 spot = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            EnemyKit summon = kits != null && kits.Length > 0 ? kits[i % kits.Length] : null;
            SpriteRenderer spriteRenderer = summon != null && summon.EnemyPrefab != null ? summon.EnemyPrefab.GetComponentInChildren<SpriteRenderer>(true) : null;
            if (spriteRenderer != null && spriteRenderer.sprite != null)
                canvas.Sprite(spriteRenderer.sprite, spot, spriteRenderer.transform.lossyScale, 0f, false, SpriteTint);
            else
                canvas.Disc(spot, 0.2f, SummonColor);
        }
    }

    // State per phase as Boss.PlayPhaseVisual: windup and recovery clips fit their duration, the charge runs Walk at its own speed, slam/summon fit Attack; Idle otherwise.
    private void DrawBoss(PreviewCanvas canvas, Step step, float time, Vector2 body, bool inCombo)
    {
        GameObject prefab = BossPrefab();
        float local = time - step.Start;
        Sprite sprite;
        if (!inCombo || local < 0f)
            sprite = PreviewAnimation.SampleFrames(Frames(Kit.IdleState), time, true);
        else if (local < step.Windup)
            sprite = PreviewAnimation.SampleFitted(Frames(Kit.WindupState), local, step.Windup);
        else if (local < step.Windup + step.Active)
            sprite = step.Pattern == BossPattern.Charge
                ? PreviewAnimation.SampleFrames(Frames(Kit.WalkState), (local - step.Windup) * Kit.ChargeAnimationSpeed, true)
                : PreviewAnimation.SampleFitted(Frames(Kit.AttackState), local - step.Windup, step.Active);
        else
            sprite = PreviewAnimation.SampleFitted(Frames(Kit.RecoveryState), local - step.Windup - step.Active, step.Recovery);

        PreviewShapes.PrefabBody(prefab, 0.5f, out Vector2 bodyOffset, out _);
        canvas.PrefabSprite(prefab, sprite, body - bodyOffset, step.Direction.x < -0.01f, Color.white);
    }

    private ObjectReferenceKeyframe[] Frames(string state) => PreviewAnimation.SpriteKeys(PreviewAnimation.StateClip(BossPrefab(), state, Vector2.right));
}
