using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Held shield Ultimate preview: the wall, its enemy push zone and the start/loop/end shield flipbook.
public partial class ClassKitEditor
{
    private const float PushFlashSeconds = 0.12f;
    private const float ShieldPushQueryPadding = 0.8f;
    private static readonly Color PushFill = new Color(0.35f, 0.75f, 1f, 0.12f);
    private static readonly Color PushFlash = new Color(0.35f, 0.75f, 1f, 0.4f);
    private static readonly Color PushEdge = new Color(0.35f, 0.75f, 1f, 0.8f);

    private float shieldLowerAt = -1f;

    // The second-click time being simulated, clamped to the hold duration (the shield drops on its own then).
    private float ShieldLowerAt(ClassKit kit)
    {
        float hold = Mathf.Max(kit.ShieldHoldDuration, 0.05f);
        if (shieldLowerAt < 0f || shieldLowerAt > hold)
            shieldLowerAt = hold;
        return shieldLowerAt;
    }

    private float ShieldTimelineLength(ClassKit kit)
    {
        Sprite[] end = ShieldFrames(kit.UltimateFxPrefab, "endFrames", out float rate);
        return ShieldLowerAt(kit) + end.Length / Mathf.Max(rate, 1f) + TailSeconds;
    }

    private static Sprite[] ShieldFrames(GameObject fxPrefab, string field, out float frameRate)
    {
        frameRate = DefaultFrameRate;
        ShieldHoldFX fx = fxPrefab != null ? fxPrefab.GetComponent<ShieldHoldFX>() : null;
        if (fx == null)
            return new Sprite[0];
        var serialized = new SerializedObject(fx);
        SerializedProperty rate = serialized.FindProperty("frameRate");
        if (rate != null)
            frameRate = rate.floatValue;
        return PreviewAnimation.SpriteArray(serialized, field);
    }

    // ShieldHoldFX stages: start once, loop while raised, end once after it is lowered (second click, timer or broken).
    private static Sprite ShieldSprite(GameObject fxPrefab, float time, float lowerAt)
    {
        Sprite[] start = ShieldFrames(fxPrefab, "startFrames", out float rate);
        if (time >= lowerAt)
            return PreviewAnimation.SampleSprites(ShieldFrames(fxPrefab, "endFrames", out _), rate, time - lowerAt, false);

        float startLength = start.Length / Mathf.Max(rate, 1f);
        if (time < startLength)
            return PreviewAnimation.SampleSprites(start, rate, time, false);
        return PreviewAnimation.SampleSprites(ShieldFrames(fxPrefab, "loopFrames", out _), rate, time - startLength, true);
    }

    // Wall collider (red, blocks hits while raised) and push query (blue, Player.ApplyPush: the FX capsule scaled like the FX + padding at FxDistance, flashing on each push).
    private void DrawShieldPreview(ClassKit kit, PreviewPanel panel)
    {
        ActionData data = Data(kit, Action.Ultimate);
        float hold = Mathf.Max(kit.ShieldHoldDuration, 0.05f);
        shieldLowerAt = EditorGUILayout.Slider(new GUIContent("Lowered At", "Simulates the second click that drops the shield early; at the max it drops on its own when Shield Hold Duration ends."), ShieldLowerAt(kit), 0.05f, hold);
        float lowerAt = shieldLowerAt;

        var markers = new List<(float, Color)> { (lowerAt, PreviewColors.Marker), (lowerAt + kit.UltimateCooldown, CooldownColor) };
        if (kit.ShieldPushInterval > 0f)
        {
            for (float push = 0f; push < lowerAt && markers.Count < 200; push += kit.ShieldPushInterval)
                markers.Add((push, LockColor));
        }

        DrawPreviewControls(kit);
        panel.DrawTimeline(ShieldTimelineLength(kit), markers, new[] { (0f, lowerAt, PreviewColors.HitEdge) });
        Rect rect = PreviewPanel.Reserve(PreviewHeight, "Red: shield wall (blocks hits while raised, red span). Yellow: FX origin (drag it, right-click resets). Blue: enemy push zone, flashing on each push (blue markers). Markers: lowered (white), cooldown end (grey). Scroll to zoom, drag to pan.");

        Vector2 direction = AimDirection;
        CapsuleCollider2D template = data.FxPrefab != null ? data.FxPrefab.GetComponent<CapsuleCollider2D>() : null;
        Vector2 prefabScale = template != null ? (Vector2)template.transform.lossyScale : Vector2.one;
        Vector2 pushSize = template != null ? new Vector2(template.size.x * Mathf.Abs(prefabScale.x), template.size.y * Mathf.Abs(prefabScale.y)) * kit.UltimateSize + new Vector2(ShieldPushQueryPadding, ShieldPushQueryPadding) : Vector2.zero;
        Vector2 wallCenter = FxOffset(kit, data, direction);
        float extent = Mathf.Max(FitRadius(kit, data, direction, false), wallCenter.magnitude + Mathf.Max(pushSize.x, pushSize.y) * 0.5f, 1.5f) * 1.15f;
        float fitScale = PreviewPanel.FitCentered(rect, extent);
        if (!data.FromAimIndicator)
            panel.DragPoint(rect, rect.center, fitScale, OriginProperty(data), direction.x < -0.01f, Repaint);
        if (!panel.Begin(rect, rect.center, fitScale, Repaint, out PreviewCanvas canvas))
            return;

        float time = panel.Time;
        DrawAimIndicator(canvas);
        bool raised = time < lowerAt;
        float animLength = AnimLength(kit, data);
        DrawCharacter(canvas, kit, data, direction, time, kit.ShieldHoldAnimation ? Mathf.Max(animLength, lowerAt) : animLength, animLength);

        if (data.FxPrefab != null)
        {
            Matrix4x4 fxMatrix = FxMatrix(kit, data, direction, false, 0f);
            SpriteRenderer shieldRenderer = new SerializedObject(data.FxPrefab.GetComponent<ShieldHoldFX>()).FindProperty("spriteRenderer").objectReferenceValue as SpriteRenderer;
            if (shieldRenderer == null)
                shieldRenderer = data.FxPrefab.GetComponent<SpriteRenderer>();
            Sprite sprite = ShieldSprite(data.FxPrefab, time, lowerAt);
            if (shieldRenderer != null && sprite != null)
                canvas.Sprite(sprite, fxMatrix * data.FxPrefab.transform.worldToLocalMatrix * shieldRenderer.transform.localToWorldMatrix, shieldRenderer.flipX, shieldRenderer.color);

            if (template != null)
            {
                float facing = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                bool flash = raised && kit.ShieldPushInterval > 0f && Mathf.Repeat(time, kit.ShieldPushInterval) < PushFlashSeconds;
                Vector2[] push = PreviewShapes.CapsuleOutline(wallCenter, pushSize, template.direction, facing);
                canvas.Outline(push, true, raised ? (flash ? PushFlash : PushFill) : Color.clear, raised ? PushEdge : IdleEdge);
            }

            foreach ((Vector2[] points, bool closed) in PreviewShapes.ColliderOutlines(data.FxPrefab, fxMatrix))
                canvas.Outline(points, closed, raised ? HitFill : Color.clear, raised ? PreviewColors.HitEdge : IdleEdge);
        }
        Vector2 fxOrigin = FxOrigin(kit, data, direction);
        canvas.Line(fxOrigin, wallCenter, PreviewColors.Handle, 1.5f);
        canvas.Handle(fxOrigin);
        PreviewPanel.End();
    }
}
