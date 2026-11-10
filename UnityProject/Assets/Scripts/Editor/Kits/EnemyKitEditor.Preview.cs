using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// The attack preview: timeline, body sprite, trigger range and either the projectile path or the melee hit zone.
public partial class EnemyKitEditor
{
    private const float PreviewHeight = 220f;
    private const float PlayerRadius = 0.3f;
    private const float FlashSeconds = 0.15f;
    private const float TailSeconds = 0.4f;
    private const float FallbackBodyRadius = 0.4f;
    private const float ExtentMargin = 1.15f;
    private const float SideAnchorX = 0.25f;
    private const float SideFitWidth = 0.72f;
    private const float PathWidth = 3f;
    private const float OriginLineWidth = 1.5f;
    private const float MinProjectilePixels = 3f;
    private const float ProjectileRadius = 0.15f;
    private const float MinHitDelay = 0.0001f;
    private const string RangedCaption = "Preview: enemy body center, attack trigger range (white), projectile path (red), player (blue). Scroll to zoom, drag the background to pan.";
    private const string MeleeCaption = "Preview: attack trigger range from the body center (white), hit zone (red) from the attack origin (yellow, drag it, right-click resets), player at trigger range (blue). 1 square = 1 unit. Scroll to zoom, drag the background to pan.";
    private static readonly Color ZoneFill = new Color(1f, 0.25f, 0.15f, 0.28f);
    private static readonly Color ProgressFill = new Color(1f, 0.25f, 0.15f, 0.45f);
    private static readonly Color FlashFill = new Color(1f, 0.92f, 0.8f, 0.7f);
    private static readonly Color TriggerColor = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color SpriteTint = new Color(1f, 1f, 1f, 0.85f);

    private readonly PreviewPanel preview = new PreviewPanel();
    private ObjectReferenceKeyframe[] attackFrames;
    private ObjectReferenceKeyframe[] idleFrames;
    private float attackStateSpeed = 1f;
    private bool attackLoops;
    private Object cachedPrefab;
    private string cachedAttackState;

    // Attack and Idle sprite keys from the prefab's controller, re-read only when the prefab or attack state changes.
    private void CacheAnimation(EnemyKit kit)
    {
        AnimationClip attackClip;
        AnimatorState attackState;

        if (cachedPrefab == kit.EnemyPrefab && cachedAttackState == kit.AttackStateName)
            return;
        cachedPrefab = kit.EnemyPrefab;
        cachedAttackState = kit.AttackStateName;
        attackClip = PreviewAnimation.StateClip(kit.EnemyPrefab, kit.AttackStateName, Vector2.right);
        attackState = PreviewAnimation.State(kit.EnemyPrefab, kit.AttackStateName);
        attackFrames = PreviewAnimation.SpriteKeys(attackClip);
        attackStateSpeed = 1f;
        if (attackState != null)
            attackStateSpeed = attackState.speed;
        attackLoops = attackClip != null && attackClip.isLooping;
        idleFrames = PreviewAnimation.SpriteKeys(PreviewAnimation.StateClip(kit.EnemyPrefab, kit.IdleStateName, Vector2.right));
    }

    // Attack frames while the attack visual is held (Enemy.PlayAttackAnimation), then Idle; null falls back to the prefab's sprite.
    private Sprite CurrentSprite(EnemyKit kit)
    {
        float time = preview.Time;

        if (time < kit.AttackAnimationDuration && attackFrames != null && attackFrames.Length > 0)
            return PreviewAnimation.SampleFrames(attackFrames, time * attackStateSpeed, attackLoops);
        return PreviewAnimation.SampleFrames(idleFrames, time - kit.AttackAnimationDuration, true);
    }

    private void DrawTimeline(EnemyKit kit)
    {
        float hitTime = kit.AttackHitDelay;

        if (kit.IsRanged)
            hitTime = 0f;
        preview.DrawTimeline(TimelineLength(kit), new[] { (hitTime, PreviewColors.HitEdge), (kit.AttackAnimationDuration, TriggerColor) });
    }

    // Projectile path and the projectile itself while it flies.
    private void DrawRanged(EnemyKit kit, PreviewCanvas canvas)
    {
        float travelled = kit.ProjectileSpeed * preview.Time;

        canvas.Line(Vector2.zero, Vector2.right * kit.ProjectileMaxDistance, PreviewColors.HitEdge, PathWidth);
        if (travelled > kit.ProjectileMaxDistance)
            return;
        Handles.color = FlashFill;
        Handles.DrawSolidDisc(canvas.ToGui(Vector2.right * travelled), Vector3.forward, Mathf.Max(MinProjectilePixels, ProjectileRadius * canvas.Scale));
    }

    // Zone outline, telegraph fill until the hit, then a flash, plus the draggable attack origin.
    private void DrawMelee(EnemyKit kit, PreviewCanvas canvas)
    {
        float hitDelay = Mathf.Max(kit.AttackHitDelay, MinHitDelay);

        DrawZone(kit, canvas, kit.AttackOrigin, 1f, ZoneFill, true);
        if (preview.Time < kit.AttackHitDelay)
            DrawZone(kit, canvas, kit.AttackOrigin, preview.Time / hitDelay, ProgressFill, false);
        else if (preview.Time < kit.AttackHitDelay + FlashSeconds)
            DrawZone(kit, canvas, kit.AttackOrigin, 1f, FlashFill, false);
        canvas.Line(Vector2.zero, kit.AttackOrigin, TriggerColor, OriginLineWidth);
        canvas.Handle(kit.AttackOrigin);
    }

    private void DrawScene(EnemyKit kit, PreviewCanvas canvas)
    {
        Vector2 bodyOffset;

        PreviewShapes.PrefabBody(kit.EnemyPrefab, FallbackBodyRadius, out bodyOffset, out _);
        canvas.PrefabSprite(kit.EnemyPrefab, CurrentSprite(kit), -bodyOffset, false, SpriteTint);
        canvas.WireDisc(Vector2.zero, kit.AttackRange, TriggerColor);
        if (isRanged)
            DrawRanged(kit, canvas);
        else
            DrawMelee(kit, canvas);
        canvas.Disc(Vector2.right * kit.AttackRange, PlayerRadius, PreviewColors.Player);
    }

    private float PreviewExtent(EnemyKit kit)
    {
        if (isRanged)
            return Mathf.Max(kit.ProjectileMaxDistance, kit.AttackRange, 1f) * ExtentMargin;
        return Mathf.Max(kit.HitReach + kit.AttackOrigin.magnitude, kit.AttackRange, 1f) * ExtentMargin;
    }

    // Ranged and circle kits are centered; directional melee zones start left of center so the reach fits.
    private void PlaceView(EnemyKit kit, Rect rect, out Vector2 anchor, out float fitScale)
    {
        float extent = PreviewExtent(kit);

        if (isRanged || kit.AttackShape == EnemyAttackShape.Circle)
        {
            anchor = rect.center;
            fitScale = PreviewPanel.FitCentered(rect, extent);
            return;
        }
        anchor = new Vector2(rect.x + rect.width * SideAnchorX, rect.center.y);
        fitScale = Mathf.Min(rect.width * SideFitWidth, rect.height * 0.5f) / extent;
    }

    // The body center sits at world zero; the view fits the largest of the zone, the trigger range and the projectile path.
    private void DrawPreview(EnemyKit kit)
    {
        string caption = MeleeCaption;
        Rect rect;
        Vector2 anchor;
        float fitScale;
        PreviewCanvas canvas;

        if (isRanged)
            caption = RangedCaption;
        EditorGUILayout.Space(4f);
        CacheAnimation(kit);
        DrawTimeline(kit);
        rect = PreviewPanel.Reserve(PreviewHeight, caption);
        PlaceView(kit, rect, out anchor, out fitScale);
        if (!isRanged)
            preview.DragPoint(rect, anchor, fitScale, serializedObject.FindProperty("attackOrigin"), false, Repaint);
        if (!preview.Begin(rect, anchor, fitScale, Repaint, out canvas))
            return;
        DrawScene(kit, canvas);
        PreviewPanel.End();
    }
}
