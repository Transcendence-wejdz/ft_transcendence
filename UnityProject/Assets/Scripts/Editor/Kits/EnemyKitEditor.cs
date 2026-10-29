using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Foldout sections, only the fields relevant to the kit (ranged or melee, attack shape) and a to-scale, playable preview of the attack timing.
[CustomEditor(typeof(EnemyKit))]
public class EnemyKitEditor : KitPreviewEditor
{
    private static readonly string[] RangedOnlyFields = { "projectilePrefab", "projectileSpeed", "projectileMaxDistance", "projectileArcHeight" };
    private const float PreviewHeight = 220f;
    private const float PlayerRadius = 0.3f;
    private const float FlashSeconds = 0.15f;
    private const float TailSeconds = 0.4f;
    private static readonly Color ZoneFill = new Color(1f, 0.25f, 0.15f, 0.28f);
    private static readonly Color ProgressFill = new Color(1f, 0.25f, 0.15f, 0.45f);
    private static readonly Color FlashFill = new Color(1f, 0.92f, 0.8f, 0.7f);
    private static readonly Color TriggerColor = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color SpriteTint = new Color(1f, 1f, 1f, 0.85f);

    private static readonly InspectorSections.Section[] Sections =
    {
        new("", "General", "tier", "maxHealth", "xpReward", "scoreReward", "moveSpeed"),
        new("", "Attack", "attackShape", "attackRange", "attackAngle", "attackWidth", "attackOrigin", "damage", "attackCooldown", "attackHitDelay",
            "attackAnimationDuration", "attackMoveSpeedMultiplier", "hitRangeTolerance", "isRanged", "projectilePrefab", "projectileSpeed", "projectileMaxDistance", "projectileArcHeight"),
        new("", "Targeting", "retargetInterval", "steerInterval", "directChaseRange", "lineOfSightInterval", "maxDistanceFromPlayers", "leashCheckInterval"),
        new("", "Visual", "enemyPrefab", "idleStateName", "runStateName", "attackStateName"),
    };

    private readonly PreviewPanel preview = new PreviewPanel();
    private ObjectReferenceKeyframe[] attackFrames;
    private ObjectReferenceKeyframe[] idleFrames;
    private float attackStateSpeed = 1f;
    private bool attackLoops;
    private Object cachedPrefab;
    private string cachedAttackState;

    private EnemyKit Kit => (EnemyKit)target;

    protected override bool AdvancePreviews(float delta) => preview.Tick(delta, TimelineLength(Kit));

    // One attack cycle: the longer of the animation and the hit (or the projectile flight), plus a short tail before looping.
    private static float TimelineLength(EnemyKit kit)
    {
        float hitTime = kit.IsRanged ? (kit.ProjectileSpeed > 0f ? kit.ProjectileMaxDistance / kit.ProjectileSpeed : 0f) : kit.AttackHitDelay + FlashSeconds;
        return Mathf.Max(kit.AttackAnimationDuration, hitTime, 0.1f) + TailSeconds;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        bool isRanged = serializedObject.FindProperty("isRanged").boolValue;
        var shape = (EnemyAttackShape)serializedObject.FindProperty("attackShape").enumValueIndex;

        InspectorSections.Draw(serializedObject, "EnemyKitEditor.Section.", Sections, name =>
            (!isRanged && System.Array.IndexOf(RangedOnlyFields, name) >= 0)
            || (name == "attackAngle" && (isRanged || shape != EnemyAttackShape.Cone))
            || (name == "attackWidth" && (isRanged || shape != EnemyAttackShape.Lane))
            || (isRanged && (name == "attackShape" || name == "hitRangeTolerance" || name == "attackOrigin")),
            section =>
            {
                if (section == "Attack")
                    DrawPreview(Kit, isRanged);
            });

        serializedObject.ApplyModifiedProperties();
    }

    // The body center sits at world zero; the view fits the largest of the zone, the trigger range and the projectile path.
    private void DrawPreview(EnemyKit kit, bool isRanged)
    {
        EditorGUILayout.Space(4f);
        CacheAnimation(kit);
        float hitTime = kit.IsRanged ? 0f : kit.AttackHitDelay;
        preview.DrawTimeline(TimelineLength(kit), new[] { (hitTime, PreviewColors.HitEdge), (kit.AttackAnimationDuration, TriggerColor) });
        Rect rect = PreviewPanel.Reserve(PreviewHeight, isRanged
            ? "Preview: enemy body center, attack trigger range (white), projectile path (red), player (blue). Scroll to zoom, drag the background to pan."
            : "Preview: attack trigger range from the body center (white), hit zone (red) from the attack origin (yellow, drag it, right-click resets), player at trigger range (blue). 1 square = 1 unit. Scroll to zoom, drag the background to pan.");

        float reach = isRanged ? kit.ProjectileMaxDistance : kit.HitReach;
        Vector2 attackOrigin = isRanged ? Vector2.zero : kit.AttackOrigin;
        float extent = Mathf.Max(reach + attackOrigin.magnitude, kit.AttackRange, 1f) * 1.15f;
        bool centered = isRanged || kit.AttackShape == EnemyAttackShape.Circle;
        Vector2 anchor = centered ? rect.center : new Vector2(rect.x + rect.width * 0.25f, rect.center.y);
        float fitScale = centered ? PreviewPanel.FitCentered(rect, extent) : Mathf.Min(rect.width * 0.72f, rect.height * 0.5f) / extent;

        if (!isRanged)
            preview.DragPoint(rect, anchor, fitScale, serializedObject.FindProperty("attackOrigin"), false, Repaint);
        if (!preview.Begin(rect, anchor, fitScale, Repaint, out PreviewCanvas canvas))
            return;

        PreviewShapes.PrefabBody(kit.EnemyPrefab, 0.4f, out Vector2 bodyOffset, out _);
        canvas.PrefabSprite(kit.EnemyPrefab, CurrentSprite(kit), -bodyOffset, false, SpriteTint);
        canvas.WireDisc(Vector2.zero, kit.AttackRange, TriggerColor);

        if (isRanged)
        {
            canvas.Line(Vector2.zero, Vector2.right * kit.ProjectileMaxDistance, PreviewColors.HitEdge, 3f);
            float travelled = kit.ProjectileSpeed * preview.Time;
            if (travelled <= kit.ProjectileMaxDistance)
            {
                Handles.color = FlashFill;
                Handles.DrawSolidDisc(canvas.ToGui(Vector2.right * travelled), Vector3.forward, Mathf.Max(3f, 0.15f * canvas.Scale));
            }
        }
        else
        {
            float hitDelay = Mathf.Max(kit.AttackHitDelay, 0.0001f);
            DrawZone(kit, canvas, attackOrigin, 1f, ZoneFill, true);
            if (preview.Time < kit.AttackHitDelay)
                DrawZone(kit, canvas, attackOrigin, preview.Time / hitDelay, ProgressFill, false);
            else if (preview.Time < kit.AttackHitDelay + FlashSeconds)
                DrawZone(kit, canvas, attackOrigin, 1f, FlashFill, false);

            canvas.Line(Vector2.zero, attackOrigin, TriggerColor, 1.5f);
            canvas.Handle(attackOrigin);
        }

        canvas.Disc(Vector2.right * kit.AttackRange, PlayerRadius, PreviewColors.Player);
        PreviewPanel.End();
    }

    // Attack and Idle sprite keys from the prefab's controller, re-read only when the prefab or attack state changes.
    private void CacheAnimation(EnemyKit kit)
    {
        if (cachedPrefab == kit.EnemyPrefab && cachedAttackState == kit.AttackStateName)
            return;

        cachedPrefab = kit.EnemyPrefab;
        cachedAttackState = kit.AttackStateName;
        AnimationClip attackClip = PreviewAnimation.StateClip(kit.EnemyPrefab, kit.AttackStateName, Vector2.right);
        AnimatorState attackState = PreviewAnimation.State(kit.EnemyPrefab, kit.AttackStateName);
        attackFrames = PreviewAnimation.SpriteKeys(attackClip);
        attackStateSpeed = attackState != null ? attackState.speed : 1f;
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

    // Same shapes as EnemyKit.HitZoneContains, attacking towards +x; fraction < 1 draws the telegraph fill so far.
    private static void DrawZone(EnemyKit kit, PreviewCanvas canvas, Vector2 attackOrigin, float fraction, Color fill, bool outline)
    {
        float reach = kit.HitReach * canvas.Scale * Mathf.Clamp01(fraction);
        if (reach <= 0f)
            return;
        Vector2 origin = canvas.ToGui(attackOrigin);
        switch (kit.AttackShape)
        {
            case EnemyAttackShape.Lane:
            {
                float half = kit.AttackWidth * 0.5f * canvas.Scale;
                Vector3[] corners = { origin + new Vector2(0f, -half), origin + new Vector2(reach, -half), origin + new Vector2(reach, half), origin + new Vector2(0f, half) };
                Handles.color = fill;
                Handles.DrawAAConvexPolygon(corners);
                if (!outline)
                    break;
                Handles.color = PreviewColors.HitEdge;
                Handles.DrawAAPolyLine(2f, corners[0], corners[1], corners[2], corners[3], corners[0]);
                break;
            }
            case EnemyAttackShape.Circle:
                Handles.color = fill;
                Handles.DrawSolidDisc(origin, Vector3.forward, reach);
                if (!outline)
                    break;
                Handles.color = PreviewColors.HitEdge;
                Handles.DrawWireDisc(origin, Vector3.forward, reach, 2f);
                break;
            default:
            {
                float angle = kit.AttackAngle;
                Vector3 from = Quaternion.Euler(0f, 0f, -angle * 0.5f) * Vector3.right;
                Handles.color = fill;
                Handles.DrawSolidArc(origin, Vector3.forward, from, angle, reach);
                if (!outline)
                    break;
                Handles.color = PreviewColors.HitEdge;
                Handles.DrawWireArc(origin, Vector3.forward, from, angle, reach, 2f);
                if (angle < 360f)
                {
                    Vector3 to = Quaternion.Euler(0f, 0f, angle * 0.5f) * Vector3.right;
                    Handles.DrawAAPolyLine(2f, origin, (Vector3)origin + from * reach);
                    Handles.DrawAAPolyLine(2f, origin, (Vector3)origin + to * reach);
                }
                break;
            }
        }
    }
}
