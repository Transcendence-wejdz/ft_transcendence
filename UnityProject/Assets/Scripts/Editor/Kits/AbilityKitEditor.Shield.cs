using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Held shield Ultimate preview: the wall, its enemy push zone and the start/loop/end shield flipbook.
public partial class AbilityKitEditor
{
    private const float PushFlashSeconds = 0.12f;
    private const float ShieldPushQueryPadding = 0.8f;
    private const float MinShieldHold = 0.05f;
    private const int MaxPushMarkers = 200;
    private const string ShieldCaption = "Red: shield wall (blocks hits while raised, red span). Yellow: FX origin (drag it, right-click resets). Blue: enemy push zone, flashing on each push (blue markers). Markers: lowered (white), cooldown end (grey). Scroll to zoom, drag to pan.";
    private const string LoweredAtTooltip = "Simulates the second click that drops the shield early; at the max it drops on its own when Shield Hold Duration ends.";
    private static readonly Color PushFill = new Color(0.35f, 0.75f, 1f, 0.12f);
    private static readonly Color PushFlash = new Color(0.35f, 0.75f, 1f, 0.4f);
    private static readonly Color PushEdge = new Color(0.35f, 0.75f, 1f, 0.8f);

    private float shieldLowerAt = -1f;

    private static float ShieldHold(AbilityKit kit)
    {
        return Mathf.Max(kit.ShieldHoldDuration, MinShieldHold);
    }

    // The second-click time being simulated, clamped to the hold duration (the shield drops on its own then).
    private float ShieldLowerAt(AbilityKit kit)
    {
        float hold = ShieldHold(kit);

        if (shieldLowerAt < 0f || shieldLowerAt > hold)
            shieldLowerAt = hold;
        return shieldLowerAt;
    }

    private float ShieldTimelineLength(AbilityKit kit)
    {
        return ShieldLowerAt(kit) + FlipbookLength(FxSprites(kit.FxPrefab, "endFrames"), FrameRate(kit.FxPrefab)) + TailSeconds;
    }

    // ShieldHoldFX stages: start once, loop while raised, end once after it is lowered (second click, timer or broken).
    private static Sprite ShieldSprite(GameObject fxPrefab, float time, float lowerAt)
    {
        float rate = FrameRate(fxPrefab);
        Sprite[] start = FxSprites(fxPrefab, "startFrames");
        float startLength = FlipbookLength(start, rate);

        if (time >= lowerAt)
            return PreviewAnimation.SampleSprites(FxSprites(fxPrefab, "endFrames"), rate, time - lowerAt, false);
        if (time < startLength)
            return PreviewAnimation.SampleSprites(start, rate, time, false);
        return PreviewAnimation.SampleSprites(FxSprites(fxPrefab, "loopFrames"), rate, time - startLength, true);
    }

    // Player.ApplyPush query: the FX capsule scaled like the FX, times Size, plus padding.
    private static Vector2 PushSize(AbilityKit kit, CapsuleCollider2D template)
    {
        Vector2 prefabScale;

        if (template == null)
            return Vector2.zero;
        prefabScale = template.transform.lossyScale;
        return new Vector2(template.size.x * Mathf.Abs(prefabScale.x), template.size.y * Mathf.Abs(prefabScale.y)) * kit.Size + Vector2.one * ShieldPushQueryPadding;
    }

    // Lowered and cooldown-end markers, plus one per push while raised.
    private static List<(float, Color)> ShieldMarkers(AbilityKit kit, float lowerAt)
    {
        List<(float, Color)> markers = new List<(float, Color)> { (lowerAt, PreviewColors.Marker), (lowerAt + kit.Cooldown, CooldownColor) };

        if (kit.ShieldPushInterval <= 0f)
            return markers;
        for (float push = 0f; push < lowerAt && markers.Count < MaxPushMarkers; push += kit.ShieldPushInterval)
            markers.Add((push, LockColor));
        return markers;
    }

    // Push zone in blue while raised, flashing on each push.
    private static void DrawPushZone(PreviewCanvas canvas, AbilityKit kit, CapsuleCollider2D template, Vector2 wallCenter, Vector2 direction, float time, bool raised)
    {
        float facing = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector2[] push = PreviewShapes.CapsuleOutline(wallCenter, PushSize(kit, template), template.direction, facing);
        bool flash = raised && kit.ShieldPushInterval > 0f && Mathf.Repeat(time, kit.ShieldPushInterval) < PushFlashSeconds;

        if (!raised)
            canvas.Outline(push, true, Color.clear, IdleEdge);
        else if (flash)
            canvas.Outline(push, true, PushFlash, PushEdge);
        else
            canvas.Outline(push, true, PushFill, PushEdge);
    }

    private void DrawShieldScene(PreviewCanvas canvas, AbilityKit kit, CapsuleCollider2D template, float lowerAt, float time)
    {
        Vector2 direction = AimDirection;
        bool raised = time < lowerAt;
        float animLength = AnimLength(kit);
        float holdUntil = animLength;
        Matrix4x4 fxMatrix;

        if (kit.ShieldHoldAnimation)
            holdUntil = Mathf.Max(animLength, lowerAt);
        DrawAimIndicator(canvas);
        DrawCharacter(canvas, kit, direction, time, holdUntil, animLength);
        if (kit.FxPrefab != null)
        {
            fxMatrix = FxMatrix(kit, direction, false, 0f);
            DrawLayerSprite(canvas, kit.FxPrefab, fxMatrix, MainRenderer(kit.FxPrefab), ShieldSprite(kit.FxPrefab, time, lowerAt));
            if (template != null)
                DrawPushZone(canvas, kit, template, FxOffset(kit, direction), direction, time, raised);
            DrawHitbox(canvas, kit, fxMatrix, raised);
        }
        DrawOriginHandle(canvas, kit, direction);
    }

    // Wall collider (red, blocks hits while raised) and push query (blue, at FxDistance, flashing on each push).
    private void DrawShieldPreview(AbilityKit kit)
    {
        Vector2 direction = AimDirection;
        CapsuleCollider2D template = null;
        float lowerAt;
        float pushExtent;
        float fitScale;
        Rect rect;
        PreviewCanvas canvas;

        shieldLowerAt = EditorGUILayout.Slider(new GUIContent("Lowered At", LoweredAtTooltip), ShieldLowerAt(kit), MinShieldHold, ShieldHold(kit));
        lowerAt = shieldLowerAt;
        DrawPreviewControls();
        preview.DrawTimeline(ShieldTimelineLength(kit), ShieldMarkers(kit, lowerAt), new[] { (0f, lowerAt, PreviewColors.HitEdge) });
        rect = PreviewPanel.Reserve(PreviewHeight, ShieldCaption);
        if (kit.FxPrefab != null)
            template = kit.FxPrefab.GetComponent<CapsuleCollider2D>();
        pushExtent = FxOffset(kit, direction).magnitude + Mathf.Max(PushSize(kit, template).x, PushSize(kit, template).y) * 0.5f;
        fitScale = PreviewPanel.FitCentered(rect, Mathf.Max(FitRadius(kit, direction, false), pushExtent, MinExtent) * ExtentMargin);
        DragOrigin(preview, kit, rect, fitScale, direction);
        if (!preview.Begin(rect, rect.center, fitScale, Repaint, out canvas))
            return;
        DrawShieldScene(canvas, kit, template, lowerAt, preview.Time);
        PreviewPanel.End();
    }
}
