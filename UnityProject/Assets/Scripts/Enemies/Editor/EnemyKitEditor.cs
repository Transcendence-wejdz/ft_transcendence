using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Foldout sections, only the fields relevant to the kit (ranged or melee, attack shape) and a to-scale, playable preview of the attack timing.
[CustomEditor(typeof(EnemyKit))]
public class EnemyKitEditor : Editor
{
    private static readonly string[] RangedOnlyFields = { "projectilePrefab", "projectileSpeed", "projectileMaxDistance", "projectileArcHeight" };
    private const float PreviewHeight = 220f;
    private const float PlayerRadius = 0.3f;
    private static readonly Color ZoneFill = new Color(1f, 0.25f, 0.15f, 0.28f);
    private static readonly Color ZoneEdge = new Color(1f, 0.3f, 0.2f, 1f);
    private static readonly Color TriggerColor = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color PlayerColor = new Color(0.35f, 0.75f, 1f, 0.9f);
    private static readonly Color OriginColor = new Color(1f, 0.85f, 0.2f, 1f);
    private const float OriginHandleRadius = 5f;
    private static readonly Color ProgressFill = new Color(1f, 0.25f, 0.15f, 0.45f);
    private static readonly Color FlashFill = new Color(1f, 0.92f, 0.8f, 0.7f);
    private const float FlashSeconds = 0.15f;
    private const float TailSeconds = 0.4f;

    private readonly AttackPreviewState preview = new AttackPreviewState();
    private double lastTick;
    private ObjectReferenceKeyframe[] attackFrames;
    private ObjectReferenceKeyframe[] idleFrames;
    private float attackStateSpeed = 1f;
    private bool attackLoops;
    private Object cachedPrefab;
    private string cachedAttackState;

    void OnEnable()
    {
        lastTick = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    void OnDisable()
    {
        EditorApplication.update -= Tick;
    }

    private void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        float delta = (float)(now - lastTick);
        lastTick = now;
        if (AttackPreviewGUI.Tick(preview, delta, TimelineLength((EnemyKit)target)))
            Repaint();
    }

    // One attack cycle: the longer of the animation and the hit (or the projectile flight), plus a short tail before looping.
    private static float TimelineLength(EnemyKit kit)
    {
        float hitTime = kit.IsRanged ? (kit.ProjectileSpeed > 0f ? kit.ProjectileMaxDistance / kit.ProjectileSpeed : 0f) : kit.AttackHitDelay + FlashSeconds;
        return Mathf.Max(kit.AttackAnimationDuration, hitTime, 0.1f) + TailSeconds;
    }

    private static readonly InspectorSections.Section[] Sections =
    {
        new("", "General", "tier", "maxHealth", "xpReward", "moveSpeed"),
        new("", "Attack", "attackShape", "attackRange", "attackAngle", "attackWidth", "attackOrigin", "damage", "attackCooldown", "attackHitDelay",
            "attackAnimationDuration", "attackMoveSpeedMultiplier", "hitRangeTolerance", "isRanged", "projectilePrefab", "projectileSpeed", "projectileMaxDistance", "projectileArcHeight"),
        new("", "Targeting", "retargetInterval", "steerInterval", "directChaseRange", "lineOfSightInterval", "maxDistanceFromPlayers", "leashCheckInterval"),
        new("", "Visual", "enemyPrefab", "idleStateName", "runStateName", "attackStateName"),
    };

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
                    DrawPreview((EnemyKit)target, isRanged);
            });

        serializedObject.ApplyModifiedProperties();
    }

    // World units map to the preview at a scale that fits the largest of the zone, the trigger range and the projectile path.
    private void DrawPreview(EnemyKit kit, bool isRanged)
    {
        EditorGUILayout.Space(4f);
        CacheAnimation(kit);
        float hitTime = kit.IsRanged ? 0f : kit.AttackHitDelay;
        AttackPreviewGUI.DrawTimeline(preview, TimelineLength(kit), new[] { (hitTime, ZoneEdge), (kit.AttackAnimationDuration, TriggerColor) });
        Rect rect = GUILayoutUtility.GetRect(10f, PreviewHeight, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField(isRanged
            ? "Preview: enemy body center, attack trigger range (white), projectile path (red), player (blue). Scroll to zoom, drag the background to pan."
            : "Preview: attack trigger range from the body center (white), hit zone (red) from the attack origin (yellow, drag it, right-click resets), player at trigger range (blue). 1 square = 1 unit. Scroll to zoom, drag the background to pan.",
            EditorStyles.wordWrappedMiniLabel);
        float reach = isRanged ? kit.ProjectileMaxDistance : kit.HitReach;
        Vector2 attackOrigin = isRanged ? Vector2.zero : kit.AttackOrigin;
        float extent = Mathf.Max(reach + attackOrigin.magnitude, kit.AttackRange, 1f) * 1.15f;
        bool centered = isRanged || kit.AttackShape == EnemyAttackShape.Circle;
        Vector2 anchor = centered ? rect.center : new Vector2(rect.x + rect.width * 0.25f, rect.center.y);
        float available = centered ? Mathf.Min(rect.width, rect.height) * 0.5f : Mathf.Min(rect.width * 0.72f, rect.height * 0.5f);
        float fitScale = available / extent;
        float scale = AttackPreviewGUI.Scale(preview, fitScale);
        Vector2 body = anchor + preview.Pan;
        Vector2 origin = body + new Vector2(attackOrigin.x, -attackOrigin.y) * scale;

        if (!isRanged)
            HandleOriginDrag(rect, body, origin, scale);
        AttackPreviewGUI.HandleZoomPan(preview, rect, anchor, fitScale, Repaint);
        if (Event.current.type != EventType.Repaint)
            return;

        EditorGUI.DrawRect(rect, AttackPreviewGUI.Background);
        GUI.BeginClip(rect);
        body -= rect.position;
        origin -= rect.position;
        AttackPreviewGUI.DrawGrid(rect.size, body, scale);
        DrawEnemySprite(kit, CurrentSprite(kit), body, scale);

        Handles.color = TriggerColor;
        Handles.DrawWireDisc(body, Vector3.forward, kit.AttackRange * scale);

        if (isRanged)
        {
            Handles.color = ZoneEdge;
            Handles.DrawAAPolyLine(3f, body, body + Vector2.right * kit.ProjectileMaxDistance * scale);
            float travelled = kit.ProjectileSpeed * preview.Time;
            if (travelled <= kit.ProjectileMaxDistance)
            {
                Handles.color = FlashFill;
                Handles.DrawSolidDisc(body + Vector2.right * travelled * scale, Vector3.forward, Mathf.Max(3f, 0.15f * scale));
            }
        }
        else
        {
            float hitDelay = Mathf.Max(kit.AttackHitDelay, 0.0001f);
            bool flashing = preview.Time >= kit.AttackHitDelay && preview.Time < kit.AttackHitDelay + FlashSeconds;
            DrawZone(kit, origin, scale, 1f, ZoneFill, true);
            if (preview.Time < kit.AttackHitDelay)
                DrawZone(kit, origin, scale, preview.Time / hitDelay, ProgressFill, false);
            else if (flashing)
                DrawZone(kit, origin, scale, 1f, FlashFill, false);

            Handles.color = TriggerColor;
            Handles.DrawAAPolyLine(1.5f, body, origin);
            Handles.color = OriginColor;
            Handles.DrawSolidDisc(origin, Vector3.forward, OriginHandleRadius);
        }

        Handles.color = PlayerColor;
        Handles.DrawSolidDisc(body + Vector2.right * kit.AttackRange * scale, Vector3.forward, PlayerRadius * scale);
        GUI.EndClip();
    }

    // Drag the yellow dot to move attackOrigin (snapped to 0.05, scale frozen while dragging); right-click it to reset to the body center.
    private void HandleOriginDrag(Rect rect, Vector2 body, Vector2 origin, float scale)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive, rect);
        Event e = Event.current;
        bool overHandle = (e.mousePosition - origin).sqrMagnitude <= (OriginHandleRadius + 4f) * (OriginHandleRadius + 4f);
        SerializedProperty property = serializedObject.FindProperty("attackOrigin");

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (!overHandle || !rect.Contains(e.mousePosition))
                    break;
                if (e.button == 1)
                {
                    property.vector2Value = Vector2.zero;
                    e.Use();
                    break;
                }
                GUIUtility.hotControl = id;
                preview.LockedScale = scale;
                e.Use();
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl != id)
                    break;
                Vector2 local = (e.mousePosition - body) / scale;
                property.vector2Value = new Vector2(Snap(local.x), Snap(-local.y));
                e.Use();
                Repaint();
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    preview.LockedScale = -1f;
                    e.Use();
                }
                break;
            case EventType.Repaint:
                EditorGUIUtility.AddCursorRect(new Rect(origin.x - OriginHandleRadius, origin.y - OriginHandleRadius, OriginHandleRadius * 2f, OriginHandleRadius * 2f), MouseCursor.MoveArrow);
                break;
        }
    }

    private static float Snap(float value) => Mathf.Round(value / 0.05f) * 0.05f;

    // Attack state's sprite keys from the prefab's controller (override clips included), plus Idle for after the animation ends.
    private void CacheAnimation(EnemyKit kit)
    {
        if (cachedPrefab == kit.EnemyPrefab && cachedAttackState == kit.AttackStateName)
            return;

        cachedPrefab = kit.EnemyPrefab;
        cachedAttackState = kit.AttackStateName;
        attackFrames = null;
        idleFrames = null;
        attackStateSpeed = 1f;
        attackLoops = false;

        Animator animator = kit.EnemyPrefab != null ? kit.EnemyPrefab.GetComponentInChildren<Animator>(true) : null;
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        RuntimeAnimatorController runtime = animator.runtimeAnimatorController;
        AnimatorOverrideController overrides = runtime as AnimatorOverrideController;
        AnimatorController controller = (overrides != null ? overrides.runtimeAnimatorController : runtime) as AnimatorController;
        if (controller == null || controller.layers.Length == 0)
            return;

        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
        {
            AnimationClip clip = child.state.motion as AnimationClip;
            if (clip != null && overrides != null && overrides[clip] != null)
                clip = overrides[clip];
            if (clip == null)
                continue;

            if (child.state.name == kit.AttackStateName)
            {
                attackFrames = AttackPreviewGUI.SpriteKeys(clip);
                attackStateSpeed = child.state.speed;
                attackLoops = clip.isLooping;
            }
            else if (child.state.name == kit.IdleStateName)
            {
                idleFrames = AttackPreviewGUI.SpriteKeys(clip);
            }
        }
    }

    // Attack frames while the attack visual is held (EnemyAI.PlayAttackVisual), then Idle; falls back to the prefab's sprite.
    private Sprite CurrentSprite(EnemyKit kit)
    {
        float time = preview.Time;
        if (time < kit.AttackAnimationDuration && attackFrames != null && attackFrames.Length > 0)
            return AttackPreviewGUI.SampleFrames(attackFrames, time * attackStateSpeed, attackLoops);
        if (idleFrames != null && idleFrames.Length > 0)
            return AttackPreviewGUI.SampleFrames(idleFrames, time - kit.AttackAnimationDuration, true);

        SpriteRenderer spriteRenderer = kit.EnemyPrefab != null ? kit.EnemyPrefab.GetComponentInChildren<SpriteRenderer>(true) : null;
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    // Pivot sits below the body center by the collider offset, as in EnemyAI.BodyCenter.
    private static void DrawEnemySprite(EnemyKit kit, Sprite sprite, Vector2 origin, float scale)
    {
        if (kit.EnemyPrefab == null || sprite == null)
            return;
        SpriteRenderer spriteRenderer = kit.EnemyPrefab.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer == null)
            return;

        CircleCollider2D body = kit.EnemyPrefab.GetComponent<CircleCollider2D>();
        Vector2 bodyOffset = body != null ? Vector2.Scale(body.offset, kit.EnemyPrefab.transform.localScale) : Vector2.zero;
        Vector2 localOffset = (Vector2)spriteRenderer.transform.position - (Vector2)kit.EnemyPrefab.transform.position;
        AttackPreviewGUI.DrawSprite(sprite, origin, scale, localOffset - bodyOffset, spriteRenderer.transform.lossyScale, 0f, false, new Color(1f, 1f, 1f, 0.85f));
    }

    // Same shapes as EnemyKit.HitZoneContains, attacking towards +x (GUI y points down); fraction < 1 draws the telegraph fill so far.
    private static void DrawZone(EnemyKit kit, Vector2 origin, float scale, float fraction, Color fill, bool outline)
    {
        float reach = kit.HitReach * scale * Mathf.Clamp01(fraction);
        if (reach <= 0f)
            return;
        switch (kit.AttackShape)
        {
            case EnemyAttackShape.Lane:
            {
                float half = kit.AttackWidth * 0.5f * scale;
                Vector3[] corners = { origin + new Vector2(0f, -half), origin + new Vector2(reach, -half), origin + new Vector2(reach, half), origin + new Vector2(0f, half) };
                Handles.color = fill;
                Handles.DrawAAConvexPolygon(corners);
                if (!outline)
                    break;
                Handles.color = ZoneEdge;
                Handles.DrawAAPolyLine(2f, corners[0], corners[1], corners[2], corners[3], corners[0]);
                break;
            }
            case EnemyAttackShape.Circle:
                Handles.color = fill;
                Handles.DrawSolidDisc(origin, Vector3.forward, reach);
                if (!outline)
                    break;
                Handles.color = ZoneEdge;
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
                Handles.color = ZoneEdge;
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
