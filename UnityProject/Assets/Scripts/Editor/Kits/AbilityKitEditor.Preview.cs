using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Action preview: the character's animation, the FX layers around it and the FX hitbox, placed like Player.SpawnLocalFx.
public partial class AbilityKitEditor
{
    private const float PreviewHeight = 240f;
    private const float TailSeconds = 0.3f;
    private const float MinExtent = 1.5f;
    private const float ExtentMargin = 1.15f;
    private const float OriginLineWidth = 1.5f;
    private const float FacingLeftThreshold = -0.01f;
    private const string PreviewCaption = "Red: hitbox (live while the bar's red span runs). Yellow: FX origin (drag it, right-click resets; fixed on the ring with From Aim Indicator), the FX spawns Distance away along the aim. Markers: animation end (white), move lock end (blue), cooldown (grey). 1 square = 1 unit. Scroll to zoom, drag to pan.";
    private static readonly Color HitFill = new Color(1f, 0.25f, 0.15f, 0.3f);
    private static readonly Color IdleEdge = new Color(1f, 0.3f, 0.2f, 0.25f);
    private static readonly Color LockColor = new Color(0.35f, 0.75f, 1f, 0.9f);
    private static readonly Color CooldownColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);

    // FX placement as Player.FxOffset, with the aim indicator center read from the Player prefab.
    private static Vector2 FxOffset(AbilityKit kit, Vector2 direction)
    {
        return kit.FxOffset(direction, AimIndicatorCenter());
    }

    // Where the FX distance starts: the aim indicator center, or FX Origin mirrored when aiming left.
    private static Vector2 FxOrigin(AbilityKit kit, Vector2 direction)
    {
        return FxOffset(kit, direction) - direction * kit.FxDistance;
    }

    // FX root placement as Player.SpawnLocalFx: player pivot + aim * distance, aim rotation unless ignored, prefab scale x Size; projectiles then fly along the aim.
    private static Matrix4x4 FxMatrix(AbilityKit kit, Vector2 direction, bool flies, float time)
    {
        Vector2 position = FxOffset(kit, direction);
        float angle = 0f;
        Vector3 rootScale = Vector3.one;

        if (flies)
            position += direction * kit.ProjectileSpeed * time;
        if (!kit.IgnoreAimRotation)
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (kit.FxPrefab != null)
            rootScale = kit.FxPrefab.transform.localScale;
        return Matrix4x4.TRS(position, Quaternion.Euler(0f, 0f, angle), rootScale * kit.Size);
    }

    // Farthest hitbox point (or projectile reach) from the player, to frame the preview.
    private static float FitRadius(AbilityKit kit, Vector2 direction, bool flies)
    {
        float radius;

        if (kit.FxPrefab == null)
            return 1f;
        radius = FxOffset(kit, direction).magnitude;
        if (flies)
            return radius + kit.ProjectileMaxDistance;
        foreach ((Vector2[] points, bool _) in PreviewShapes.ColliderOutlines(kit.FxPrefab, FxMatrix(kit, direction, false, 0f)))
        {
            foreach (Vector2 point in points)
                radius = Mathf.Max(radius, point.magnitude);
        }
        return radius;
    }

    // Aim angle slider and controller picker above the preview, one row each so the slider keeps its bar.
    private void DrawPreviewControls()
    {
        aimAngle = EditorGUILayout.Slider("Aim Angle", aimAngle, 0f, 360f);
        controllerOverride = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Controller", Controller(), typeof(RuntimeAnimatorController), false);
    }

    // Draggable FX origin, unless it is pinned to the aim ring.
    private void DragOrigin(PreviewPanel panel, AbilityKit kit, Rect rect, float fitScale, Vector2 direction)
    {
        if (!kit.FxFromAimIndicator)
            panel.DragPoint(rect, rect.center, fitScale, serializedObject.FindProperty("fxOrigin"), direction.x < FacingLeftThreshold, Repaint);
    }

    private static void DrawOriginHandle(PreviewCanvas canvas, AbilityKit kit, Vector2 direction)
    {
        Vector2 fxOrigin = FxOrigin(kit, direction);

        canvas.Line(fxOrigin, FxOffset(kit, direction), PreviewColors.Handle, OriginLineWidth);
        canvas.Handle(fxOrigin);
    }

    // Projectile kits fly their FX along the aim.
    private static bool Flies(AbilityKit kit)
    {
        if (kit.Kind != AbilityKind.Projectile || kit.FxPrefab == null)
            return false;
        return kit.FxPrefab.GetComponent<ArrowProjectile>() != null;
    }

    // Hitbox at its live position (spawn position once the FX is gone), red while it deals damage.
    private static void DrawHitbox(PreviewCanvas canvas, AbilityKit kit, Matrix4x4 hitMatrix, bool live)
    {
        Color fill = Color.clear;
        Color edge = IdleEdge;

        if (live)
        {
            fill = HitFill;
            edge = PreviewColors.HitEdge;
        }
        foreach ((Vector2[] points, bool closed) in PreviewShapes.ColliderOutlines(kit.FxPrefab, hitMatrix))
            canvas.Outline(points, closed, fill, edge);
    }

    private void DrawActionScene(PreviewCanvas canvas, AbilityKit kit, bool flies, float time)
    {
        Vector2 direction = AimDirection;
        float animLength = AnimLength(kit);
        (float hitFrom, float hitTo) = HitWindow(kit, kit.FxPrefab);
        Matrix4x4 fxMatrix = FxMatrix(kit, direction, flies, time);
        List<FxLayer> layers = FxLayers(kit.FxPrefab);
        bool fxAlive = kit.FxPrefab != null && time <= FxLifetime(kit, kit.FxPrefab);

        DrawAimIndicator(canvas);
        if (fxAlive)
            DrawFxLayers(canvas, layers, kit.FxPrefab, fxMatrix, time, true);
        DrawCharacter(canvas, kit, direction, time, animLength, animLength);
        if (fxAlive)
            DrawFxLayers(canvas, layers, kit.FxPrefab, fxMatrix, time, false);
        if (!fxAlive)
            fxMatrix = FxMatrix(kit, direction, flies, 0f);
        if (kit.FxPrefab != null)
            DrawHitbox(canvas, kit, fxMatrix, time >= hitFrom && time <= hitTo);
        if (flies)
            canvas.Line(FxOrigin(kit, direction), FxOffset(kit, direction) + direction * kit.ProjectileMaxDistance, IdleEdge);
        DrawOriginHandle(canvas, kit, direction);
    }

    private void DrawActionPreview(AbilityKit kit)
    {
        (float hitFrom, float hitTo) = HitWindow(kit, kit.FxPrefab);
        bool flies = Flies(kit);
        Vector2 direction = AimDirection;
        Rect rect;
        float fitScale;
        PreviewCanvas canvas;

        DrawPreviewControls();
        preview.DrawTimeline(TimelineLength(kit),
            new[] { (AnimLength(kit), PreviewColors.Marker), (kit.MoveLockDuration, LockColor), (kit.Cooldown, CooldownColor) },
            new[] { (hitFrom, hitTo, PreviewColors.HitEdge) });
        rect = PreviewPanel.Reserve(PreviewHeight, PreviewCaption);
        fitScale = PreviewPanel.FitCentered(rect, Mathf.Max(FitRadius(kit, direction, flies), MinExtent) * ExtentMargin);
        DragOrigin(preview, kit, rect, fitScale, direction);
        if (!preview.Begin(rect, rect.center, fitScale, Repaint, out canvas))
            return;
        DrawActionScene(canvas, kit, flies, preview.Time);
        PreviewPanel.End();
    }

    private void DrawPreview(AbilityKit kit)
    {
        EditorGUILayout.Space(2f);
        if (!InspectorSections.Foldout("AbilityKitEditor.Preview", "Preview", EditorStyles.foldoutHeader))
            return;
        if (kit.Kind == AbilityKind.Shield)
            DrawShieldPreview(kit);
        else
            DrawActionPreview(kit);
    }
}
