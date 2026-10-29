using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Action preview: the character's animation, the FX layers around it and the FX hitbox, placed like Player.SpawnLocalFx.
public partial class ClassKitEditor
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    private const float PreviewHeight = 240f;
    private const float TailSeconds = 0.3f;
    private const float DefaultFrameRate = 24f;
    private static readonly Color HitFill = new Color(1f, 0.25f, 0.15f, 0.3f);
    private static readonly Color IdleEdge = new Color(1f, 0.3f, 0.2f, 0.25f);
    private static readonly Color LockColor = new Color(0.35f, 0.75f, 1f, 0.9f);
    private static readonly Color CooldownColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);

    private class FxLayer
    {
        public SpriteRenderer Renderer;
        public Sprite[] Frames;
        public bool Loop;
        public bool Behind;
    }

    private void DrawPreview(ClassKit kit, Action action)
    {
        if (!InspectorSections.Foldout("ClassKitEditor.Preview." + action, action + " Preview"))
            return;

        PreviewPanel panel = previews[(int)action];
        if (action == Action.Ultimate && kit.HasShield)
        {
            DrawShieldPreview(kit, panel);
            return;
        }

        ActionData data = Data(kit, action);
        float lifetime = FxLifetime(kit, data.FxPrefab);
        float animLength = AnimLength(kit, data);

        DrawPreviewControls(kit);
        panel.DrawTimeline(TimelineLength(kit, action),
            new[] { (animLength, PreviewColors.Marker), (data.MoveLock, LockColor), (data.Cooldown, CooldownColor) },
            new[] { (0f, lifetime, PreviewColors.HitEdge) });
        Rect rect = PreviewPanel.Reserve(PreviewHeight, "Red: hitbox (live while the bar's red span runs). Yellow: FX origin (drag it, right-click resets; fixed on the ring with From Aim Indicator), the FX spawns Distance away along the aim. Markers: animation end (white), move lock end (blue), cooldown (grey). 1 square = 1 unit. Scroll to zoom, drag to pan.");

        Vector2 direction = AimDirection;
        bool projectile = data.FxPrefab != null && data.FxPrefab.GetComponent<ArrowProjectile>() != null;
        float extent = Mathf.Max(FitRadius(kit, data, direction, projectile), 1.5f) * 1.15f;
        float fitScale = PreviewPanel.FitCentered(rect, extent);
        if (!data.FromAimIndicator)
            panel.DragPoint(rect, rect.center, fitScale, OriginProperty(data), direction.x < -0.01f, Repaint);
        if (!panel.Begin(rect, rect.center, fitScale, Repaint, out PreviewCanvas canvas))
            return;

        float time = panel.Time;
        DrawAimIndicator(canvas);
        Matrix4x4 fxMatrix = FxMatrix(kit, data, direction, projectile, time);
        List<FxLayer> layers = FxLayers(data.FxPrefab);
        bool fxAlive = data.FxPrefab != null && time <= lifetime;
        if (fxAlive)
            DrawFxLayers(canvas, layers, data.FxPrefab, fxMatrix, time, behind: true);
        DrawCharacter(canvas, kit, data, direction, time, animLength, animLength);
        if (fxAlive)
            DrawFxLayers(canvas, layers, data.FxPrefab, fxMatrix, time, behind: false);

        if (data.FxPrefab != null)
        {
            Matrix4x4 hitMatrix = fxAlive ? fxMatrix : FxMatrix(kit, data, direction, projectile, 0f);
            foreach ((Vector2[] points, bool closed) in PreviewShapes.ColliderOutlines(data.FxPrefab, hitMatrix))
                canvas.Outline(points, closed, fxAlive ? HitFill : Color.clear, fxAlive ? PreviewColors.HitEdge : IdleEdge);
        }
        Vector2 fxOrigin = FxOrigin(kit, data, direction);
        if (projectile)
            canvas.Line(fxOrigin, FxOffset(kit, data, direction) + direction * kit.ProjectileMaxDistance, IdleEdge);
        canvas.Line(fxOrigin, FxOffset(kit, data, direction), PreviewColors.Handle, 1.5f);
        canvas.Handle(fxOrigin);
        PreviewPanel.End();
    }

    // Projectiles live until their max distance; flipbook FX until their last frame, the whole time with a live hitbox.
    private static float FxLifetime(ClassKit kit, GameObject fxPrefab)
    {
        if (fxPrefab == null)
            return 0f;
        if (fxPrefab.GetComponent<ArrowProjectile>() != null)
            return kit.ProjectileSpeed > 0f ? kit.ProjectileMaxDistance / kit.ProjectileSpeed : 0f;

        MonoBehaviour fx = FxComponent(fxPrefab);
        if (fx == null)
            return 0f;
        var serialized = new SerializedObject(fx);
        int frameCount = 0;
        foreach (string field in new[] { "frames", "frontFrames", "backFrames" })
            frameCount = Mathf.Max(frameCount, PreviewAnimation.SpriteArray(serialized, field).Length);
        float frameRate = FrameRate(fxPrefab);
        return frameRate > 0f ? frameCount / frameRate : 0f;
    }

    private static MonoBehaviour FxComponent(GameObject fxPrefab)
    {
        foreach (MonoBehaviour behaviour in fxPrefab.GetComponents<MonoBehaviour>())
        {
            if (behaviour is IAttackFX)
                return behaviour;
        }
        return null;
    }

    private static float FrameRate(GameObject fxPrefab)
    {
        MonoBehaviour fx = fxPrefab != null ? FxComponent(fxPrefab) : null;
        SerializedProperty rate = fx != null ? new SerializedObject(fx).FindProperty("frameRate") : null;
        return rate != null ? rate.floatValue : DefaultFrameRate;
    }

    // Sprite layers of the FX: SlashAttackFX/ArrowProjectile frames on their renderer, LayeredAttackFX front/back pairs.
    private static List<FxLayer> FxLayers(GameObject fxPrefab)
    {
        var layers = new List<FxLayer>();
        MonoBehaviour fx = fxPrefab != null ? FxComponent(fxPrefab) : null;
        if (fx == null)
            return layers;

        var serialized = new SerializedObject(fx);
        SpriteRenderer Renderer(string field) => serialized.FindProperty(field)?.objectReferenceValue as SpriteRenderer;

        if (fx is LayeredAttackFX)
        {
            layers.Add(new FxLayer { Renderer = Renderer("backRenderer"), Frames = PreviewAnimation.SpriteArray(serialized, "backFrames"), Behind = true });
            layers.Add(new FxLayer { Renderer = Renderer("frontRenderer"), Frames = PreviewAnimation.SpriteArray(serialized, "frontFrames") });
        }
        else if (fx is ArrowProjectile)
        {
            SpriteRenderer baseRenderer = fxPrefab.GetComponent<SpriteRenderer>();
            SpriteRenderer animated = Renderer("fxSpriteRenderer");
            if (animated != null && baseRenderer != null)
                layers.Add(new FxLayer { Renderer = baseRenderer, Frames = new[] { baseRenderer.sprite }, Loop = true });
            layers.Add(new FxLayer { Renderer = animated != null ? animated : baseRenderer, Frames = PreviewAnimation.SpriteArray(serialized, "frames"), Loop = true });
        }
        else
        {
            layers.Add(new FxLayer { Renderer = fxPrefab.GetComponent<SpriteRenderer>(), Frames = PreviewAnimation.SpriteArray(serialized, "frames") });
        }
        layers.RemoveAll(layer => layer.Renderer == null);
        return layers;
    }

    // FX root placement as Player.SpawnLocalFx: player pivot + aim * distance, aim rotation unless ignored, prefab scale x Size; projectiles then fly along the aim.
    private static Matrix4x4 FxMatrix(ClassKit kit, ActionData data, Vector2 direction, bool projectile, float time)
    {
        Vector2 position = FxOffset(kit, data, direction);
        if (projectile)
            position += direction * kit.ProjectileSpeed * time;
        float angle = data.IgnoreRotation ? 0f : Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 rootScale = data.FxPrefab != null ? data.FxPrefab.transform.localScale : Vector3.one;
        return Matrix4x4.TRS(position, Quaternion.Euler(0f, 0f, angle), rootScale * data.Size);
    }

    // Farthest hitbox point (or projectile reach) from the player, to frame the preview.
    private static float FitRadius(ClassKit kit, ActionData data, Vector2 direction, bool projectile)
    {
        if (data.FxPrefab == null)
            return 1f;
        float offset = FxOffset(kit, data, direction).magnitude;
        if (projectile)
            return offset + kit.ProjectileMaxDistance;

        float radius = offset;
        foreach ((Vector2[] points, bool _) in PreviewShapes.ColliderOutlines(data.FxPrefab, FxMatrix(kit, data, direction, false, 0f)))
        {
            foreach (Vector2 point in points)
                radius = Mathf.Max(radius, point.magnitude);
        }
        return radius;
    }

    private static void DrawFxLayers(PreviewCanvas canvas, List<FxLayer> layers, GameObject fxPrefab, Matrix4x4 fxMatrix, float time, bool behind)
    {
        float frameRate = FrameRate(fxPrefab);
        foreach (FxLayer layer in layers)
        {
            if (layer.Behind != behind)
                continue;
            Sprite sprite = layer.Frames != null && layer.Frames.Length > 0
                ? PreviewAnimation.SampleSprites(layer.Frames, frameRate, time, layer.Loop)
                : layer.Renderer.sprite;
            if (sprite != null)
                canvas.Sprite(sprite, fxMatrix * fxPrefab.transform.worldToLocalMatrix * layer.Renderer.transform.localToWorldMatrix, layer.Renderer.flipX, layer.Renderer.color);
        }
    }

    // Center of the Player prefab's aim indicator ring, relative to its feet.
    private static Vector2 AimIndicatorCenter()
    {
        MouseDirectionIndicator indicator = AimIndicator();
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        return indicator != null ? (Vector2)(indicator.transform.position - player.transform.position) : Vector2.zero;
    }

    private static MouseDirectionIndicator AimIndicator()
    {
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        return player != null ? player.GetComponentInChildren<MouseDirectionIndicator>(true) : null;
    }

    // The aim indicator ring under the character, pointing along the aim like MouseDirectionIndicator.
    private void DrawAimIndicator(PreviewCanvas canvas)
    {
        MouseDirectionIndicator indicator = AimIndicator();
        if (indicator == null)
            return;
        Vector2 center = AimIndicatorCenter();
        foreach (SpriteRenderer ring in indicator.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Vector2 offset = ring.transform.position - indicator.transform.position;
            canvas.Sprite(ring.sprite, center + offset, ring.transform.lossyScale, aimAngle - 90f, false, new Color(1f, 1f, 1f, 0.35f));
        }
    }

    // Action clip (scaled by its anim speed) until holdUntil (its last frame held past animLength, like ShieldHeld), then Idle; mirrored when aiming left like the in-game flipX.
    private void DrawCharacter(PreviewCanvas canvas, ClassKit kit, ActionData data, Vector2 direction, float time, float holdUntil, float animLength)
    {
        Sprite sprite = time < holdUntil
            ? PreviewAnimation.SampleFrames(PreviewAnimation.SpriteKeys(ActionClip(kit, data.StateName)), Mathf.Min(time, animLength) * data.AnimSpeed, false)
            : PreviewAnimation.SampleFrames(PreviewAnimation.SpriteKeys(ActionClip(kit, "Idle_Tree")), time - holdUntil, true);
        canvas.PrefabSprite(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath), sprite, Vector2.zero, direction.x < -0.01f, Color.white);
    }
}
