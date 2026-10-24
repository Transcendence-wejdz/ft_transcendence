using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Foldout sections for the BossKit plus playable previews of each pattern and of the combos, replaying BossAI's timing, aim and hit shapes.
[CustomEditor(typeof(BossKit))]
public class BossKitEditor : Editor
{
    private const float PreviewHeight = 260f;
    private const float TailSeconds = 0.4f;
    private const float FlashSeconds = 0.15f;
    private const float PlayerRadius = 0.3f;
    private const float ContactMargin = 0.5f;
    private const float ChainWindupFraction = 0.6f;
    private const float ChargeOvershoot = 3f;
    private const float MinChargeLength = 4f;
    private static readonly Color ZoneFill = new Color(1f, 0.25f, 0.15f, 0.15f);
    private static readonly Color ZoneEdge = new Color(1f, 0.3f, 0.2f, 1f);
    private static readonly Color ProgressFill = new Color(1f, 0.25f, 0.15f, 0.4f);
    private static readonly Color FlashFill = new Color(1f, 0.92f, 0.8f, 0.6f);
    private static readonly Color RangeColor = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color MeleeColor = new Color(0.35f, 0.75f, 1f, 0.6f);
    private static readonly Color PlayerColor = new Color(0.35f, 0.75f, 1f, 0.9f);
    private static readonly Color SummonColor = new Color(0.75f, 0.45f, 1f, 0.9f);
    private static readonly Color PhaseColor = new Color(1f, 1f, 1f, 0.6f);

    private static readonly InspectorSections.Section[] Sections =
    {
        new("", "General", "displayName", "maxHealth", "healthPerExtraPlayer"),
        new("", "Movement", "moveSpeed", "retargetInterval", "steerInterval"),
        new("", "Patterns", "patternCooldown", "recoveryDuration", "enrageHealthFraction", "enrageTempo"),
        new("", "Charge", "chargeMinRange", "chargeCooldown", "chargeLeadSeconds", "chargeChainCount", "chargeWindup", "chargeSpeed", "chargeDistance", "chargeWidth", "chargeDamage"),
        new("", "Slam", "slamMaxRange", "slamCooldown", "slamWindup", "slamRadius", "slamDamage", "slamActiveDuration"),
        new("", "Summon", "summonKits", "summonCount", "summonInterval", "summonWindup", "summonRadius", "summonActiveDuration", "summonFxPrefabs", "summonFxOffset"),
        new("", "Animator States", "idleState", "walkState", "windupState", "attackState", "recoveryState", "chargeAnimationSpeed"),
    };

    private enum Preview { Charge, Slam, Summon, Combo }
    private enum Combo { Opening, Melee, Ranged }
    private static readonly string[] ComboLabels = { "Opening (summon)", "Melee (charge + slam)", "Ranged (chain + summon)" };

    private readonly AttackPreviewState[] previews = { new AttackPreviewState(), new AttackPreviewState(), new AttackPreviewState(), new AttackPreviewState() };
    private double lastTick;
    private bool enraged;
    private Combo combo = Combo.Ranged;
    private GameObject bossPrefab;
    private BossKit cachedKit;

    private struct Step
    {
        public BossPattern Pattern;
        public float Start;
        public float Windup;
        public float Active;
        public float Recovery;
        public Vector2 From;
        public Vector2 Direction;
        public float Length;
        public float End => Start + Windup + Active + Recovery;
    }

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

        bool repaint = false;
        for (int i = 0; i < previews.Length; i++)
            repaint |= AttackPreviewGUI.Tick(previews[i], delta, TimelineLength(BuildSteps((Preview)i)));
        if (repaint)
            Repaint();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        InspectorSections.Draw(serializedObject, "BossKitEditor.Section.", Sections, null, section =>
        {
            if (section == "Patterns")
                DrawPreview(Preview.Combo);
            else if (System.Enum.TryParse(section, out Preview preview))
                DrawPreview(preview);
        });
        serializedObject.ApplyModifiedProperties();
    }

    private BossKit Kit => (BossKit)target;

    private float Tempo => enraged ? Mathf.Max(Kit.EnrageTempo, 0.01f) : 1f;

    // The prefab whose Boss component uses this kit, for the sprite, Animator and body collider.
    private GameObject BossPrefab()
    {
        if (cachedKit == Kit && bossPrefab != null)
            return bossPrefab;
        cachedKit = Kit;
        bossPrefab = null;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            Boss boss = prefab != null ? prefab.GetComponent<Boss>() : null;
            if (boss != null && boss.Kit == Kit)
            {
                bossPrefab = prefab;
                break;
            }
        }
        return bossPrefab;
    }

    private void Body(out Vector2 offset, out float radius)
    {
        GameObject prefab = BossPrefab();
        CircleCollider2D circle = prefab != null ? prefab.GetComponent<CircleCollider2D>() : null;
        Vector3 scale = prefab != null ? prefab.transform.localScale : Vector3.one;
        offset = circle != null ? Vector2.Scale(circle.offset, scale) : Vector2.zero;
        radius = circle != null ? circle.radius * Mathf.Max(scale.x, scale.y) : 0.5f;
    }

    private float ChargeHitWidth
    {
        get
        {
            Body(out _, out float radius);
            return Mathf.Max(Kit.ChargeWidth, 2f * (radius + ContactMargin));
        }
    }

    // Where the simulated target stands: inside slam range for melee, past the charge range otherwise.
    private Vector2 TargetFor(Preview preview) => preview == Preview.Slam || (preview == Preview.Combo && combo == Combo.Melee)
        ? Vector2.right * Mathf.Max(0.5f, Kit.SlamMaxRange * 0.6f)
        : Vector2.right * Mathf.Max(Kit.ChargeMinRange + 2f, 3f);

    private List<BossPattern> Patterns(Preview preview)
    {
        var patterns = new List<BossPattern>();
        switch (preview)
        {
            case Preview.Charge:
                patterns.Add(BossPattern.Charge);
                break;
            case Preview.Slam:
                patterns.Add(BossPattern.Slam);
                break;
            case Preview.Summon:
                patterns.Add(BossPattern.Summon);
                break;
            default:
                if (combo == Combo.Opening)
                    patterns.Add(BossPattern.Summon);
                else if (combo == Combo.Melee)
                {
                    patterns.Add(BossPattern.Charge);
                    patterns.Add(BossPattern.Slam);
                }
                else
                {
                    for (int i = 0; i < Mathf.Max(1, Kit.ChargeChainCount); i++)
                        patterns.Add(BossPattern.Charge);
                    patterns.Add(BossPattern.Summon);
                }
                break;
        }
        return patterns;
    }

    // Same sequencing as BossAI: every step re-aims at the target, chained windups are shortened, only the last step recovers, all scaled by the tempo.
    private List<Step> BuildSteps(Preview preview)
    {
        var steps = new List<Step>();
        List<BossPattern> patterns = Patterns(preview);
        Vector2 target = TargetFor(preview);
        Vector2 position = Vector2.zero;
        float time = 0f;
        for (int i = 0; i < patterns.Count; i++)
        {
            var step = new Step { Pattern = patterns[i], Start = time, From = position };
            Vector2 aim = target - position;
            step.Direction = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
            float windup = patterns[i] == BossPattern.Charge ? Kit.ChargeWindup : patterns[i] == BossPattern.Slam ? Kit.SlamWindup : Kit.SummonWindup;
            if (i > 0)
                windup *= ChainWindupFraction;
            step.Windup = windup / Tempo;

            if (patterns[i] == BossPattern.Charge)
            {
                step.Length = Mathf.Clamp(aim.magnitude + ChargeOvershoot, MinChargeLength, Mathf.Max(MinChargeLength, Kit.ChargeDistance));
                step.Active = step.Length / Mathf.Max(0.01f, Kit.ChargeSpeed);
                position += step.Direction * step.Length;
            }
            else
            {
                step.Active = (patterns[i] == BossPattern.Slam ? Kit.SlamActiveDuration : Kit.SummonActiveDuration) / Tempo;
            }
            step.Recovery = i == patterns.Count - 1 ? Kit.RecoveryDuration / Tempo : 0f;
            time = step.End;
            steps.Add(step);
        }
        return steps;
    }

    private static float TimelineLength(List<Step> steps) => (steps.Count > 0 ? steps[steps.Count - 1].End : 0f) + TailSeconds;

    private void DrawPreview(Preview preview)
    {
        string key = "BossKitEditor.Preview." + preview;
        bool open = EditorPrefs.GetBool(key, true);
        bool newOpen = EditorGUILayout.Foldout(open, (preview == Preview.Combo ? "Combo" : preview.ToString()) + " Preview", true);
        if (newOpen != open)
            EditorPrefs.SetBool(key, newOpen);
        if (!newOpen)
            return;

        AttackPreviewState state = previews[(int)preview];
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
            spans.Add((step.Start + step.Windup, step.Start + step.Windup + (step.Pattern == BossPattern.Charge ? step.Active : FlashSeconds), step.Pattern == BossPattern.Summon ? SummonColor : ZoneEdge));
        }
        float length = TimelineLength(steps);
        AttackPreviewGUI.DrawTimeline(state, length, markers, spans);

        Rect rect = GUILayoutUtility.GetRect(10f, PreviewHeight, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("Rings: charge min range (white), slam max range (blue), around the boss start. Blue dot: target. Red: telegraph then hit; purple: summon area and spawns. Timeline: step starts (white), hits (red/purple spans). Scroll to zoom, drag to pan.", EditorStyles.wordWrappedMiniLabel);

        Vector2 target = TargetFor(preview);
        float extent = Mathf.Max(Kit.SlamRadius, Kit.SummonRadius, Kit.ChargeMinRange, target.magnitude, 2f);
        foreach (Step step in steps)
            extent = Mathf.Max(extent, (step.From + step.Direction * step.Length).magnitude + ChargeHitWidth * 0.5f);
        extent *= 1.1f;

        Vector2 anchor = rect.center;
        float fitScale = Mathf.Min(rect.width, rect.height) * 0.5f / extent;
        float scale = AttackPreviewGUI.Scale(state, fitScale);
        AttackPreviewGUI.HandleZoomPan(state, rect, anchor, fitScale, Repaint);
        if (Event.current.type != EventType.Repaint)
            return;

        EditorGUI.DrawRect(rect, AttackPreviewGUI.Background);
        GUI.BeginClip(rect);
        Vector2 origin = anchor + state.Pan - rect.position;
        AttackPreviewGUI.DrawGrid(rect.size, origin, scale);

        Handles.color = RangeColor;
        Handles.DrawWireDisc(origin, Vector3.forward, Kit.ChargeMinRange * scale);
        Handles.color = MeleeColor;
        Handles.DrawWireDisc(origin, Vector3.forward, Kit.SlamMaxRange * scale);

        float time = state.Time;
        Step current = steps.Count > 0 ? steps[steps.Count - 1] : default;
        foreach (Step step in steps)
        {
            if (time < step.End)
            {
                current = step;
                break;
            }
        }
        Vector2 body = BodyAt(steps, time);
        DrawStep(current, time, body, origin, scale);
        DrawBoss(current, time, body, origin, scale, steps.Count > 0 && time < steps[steps.Count - 1].End);

        Handles.color = PlayerColor;
        Handles.DrawSolidDisc(AttackPreviewGUI.ToGui(origin, target, scale), Vector3.forward, PlayerRadius * scale);
        GUI.EndClip();
    }

    // Body center at time t: still between steps, sliding along the lane during charge actives.
    private static Vector2 BodyAt(List<Step> steps, float time)
    {
        Vector2 position = Vector2.zero;
        foreach (Step step in steps)
        {
            float activeStart = step.Start + step.Windup;
            if (step.Pattern == BossPattern.Charge)
            {
                if (time <= activeStart)
                    return step.From;
                float k = step.Active > 0f ? Mathf.Clamp01((time - activeStart) / step.Active) : 1f;
                position = step.From + step.Direction * step.Length * k;
                if (k < 1f)
                    return position;
            }
            else if (time < step.End)
            {
                return step.From;
            }
        }
        return position;
    }

    private void DrawStep(Step step, float time, Vector2 body, Vector2 origin, float scale)
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
                Vector2[] lane = { center - side, center + side, center + side + step.Direction * step.Length, center - side + step.Direction * step.Length };
                AttackPreviewGUI.DrawOutline(lane, true, origin, scale, ZoneFill, ZoneEdge);
                if (windup)
                {
                    Vector2 reach = step.Direction * step.Length * progress;
                    AttackPreviewGUI.DrawOutline(new[] { center - side, center + side, center + side + reach, center - side + reach }, true, origin, scale, ProgressFill, Color.clear);
                }
                else if (local < step.Windup + step.Active)
                {
                    Handles.color = FlashFill;
                    Handles.DrawSolidDisc(AttackPreviewGUI.ToGui(origin, body, scale), Vector3.forward, width * 0.5f * scale);
                }
                break;
            }
            case BossPattern.Slam:
                DrawCircle(center, Kit.SlamRadius, windup, progress, local - step.Windup, origin, scale, ZoneEdge);
                break;
            case BossPattern.Summon:
                DrawCircle(center, Kit.SummonRadius, windup, progress, local - step.Windup, origin, scale, SummonColor);
                if (!windup)
                    DrawSummons(center, origin, scale);
                break;
        }
    }

    private static void DrawCircle(Vector2 center, float radius, bool windup, float progress, float sinceHit, Vector2 origin, float scale, Color edge)
    {
        Vector2 gui = AttackPreviewGUI.ToGui(origin, center, scale);
        Handles.color = new Color(edge.r, edge.g, edge.b, 0.15f);
        Handles.DrawSolidDisc(gui, Vector3.forward, radius * scale);
        Handles.color = edge;
        Handles.DrawWireDisc(gui, Vector3.forward, radius * scale, 2f);
        if (windup)
        {
            Handles.color = new Color(edge.r, edge.g, edge.b, 0.4f);
            Handles.DrawSolidDisc(gui, Vector3.forward, radius * progress * scale);
        }
        else if (sinceHit < FlashSeconds)
        {
            Handles.color = FlashFill;
            Handles.DrawSolidDisc(gui, Vector3.forward, radius * scale);
        }
    }

    // Spawn spots are random in game; a fixed golden-angle spread shows the count and area, with each summoned kit's sprite.
    private void DrawSummons(Vector2 center, Vector2 origin, float scale)
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
                AttackPreviewGUI.DrawSprite(spriteRenderer.sprite, origin, scale, spot, spriteRenderer.transform.lossyScale, 0f, false, new Color(1f, 1f, 1f, 0.85f));
            else
            {
                Handles.color = SummonColor;
                Handles.DrawSolidDisc(AttackPreviewGUI.ToGui(origin, spot, scale), Vector3.forward, 0.2f * scale);
            }
        }
    }

    // State per phase as BossAI.PlayPhaseVisual: windup and recovery clips fit their duration, the charge runs Walk at its own speed, slam/summon fit Attack; Idle otherwise.
    private void DrawBoss(Step step, float time, Vector2 body, Vector2 origin, float scale, bool inCombo)
    {
        GameObject prefab = BossPrefab();
        SpriteRenderer spriteRenderer = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (spriteRenderer == null)
            return;

        Sprite sprite = spriteRenderer.sprite;
        float local = time - step.Start;
        if (inCombo && local >= 0f)
        {
            if (local < step.Windup)
                sprite = FitSample(Kit.WindupState, local, step.Windup);
            else if (local < step.Windup + step.Active)
                sprite = step.Pattern == BossPattern.Charge
                    ? Sample(Kit.WalkState, (local - step.Windup) * Kit.ChargeAnimationSpeed, true)
                    : FitSample(Kit.AttackState, local - step.Windup, step.Active);
            else
                sprite = FitSample(Kit.RecoveryState, local - step.Windup - step.Active, step.Recovery);
        }
        else
        {
            sprite = Sample(Kit.IdleState, time, true);
        }
        if (sprite == null)
            sprite = spriteRenderer.sprite;

        Body(out Vector2 bodyOffset, out _);
        Vector2 rendererOffset = (Vector2)spriteRenderer.transform.position - (Vector2)prefab.transform.position;
        AttackPreviewGUI.DrawSprite(sprite, origin, scale, body - bodyOffset + rendererOffset, spriteRenderer.transform.lossyScale, 0f, step.Direction.x < -0.01f, Color.white);
    }

    private Sprite FitSample(string state, float time, float duration)
    {
        ObjectReferenceKeyframe[] frames = AttackPreviewGUI.SpriteKeys(Clip(state));
        float clipLength = AttackPreviewGUI.FramesLength(frames);
        return AttackPreviewGUI.SampleFrames(frames, duration > 0f ? time / duration * clipLength : 0f, false);
    }

    private Sprite Sample(string state, float time, bool loop) => AttackPreviewGUI.SampleFrames(AttackPreviewGUI.SpriteKeys(Clip(state)), time, loop);

    private AnimationClip Clip(string state)
    {
        GameObject prefab = BossPrefab();
        Animator animator = prefab != null ? prefab.GetComponentInChildren<Animator>(true) : null;
        if (animator == null || animator.runtimeAnimatorController == null)
            return null;
        AnimatorController controller = AttackPreviewGUI.ResolveController(animator.runtimeAnimatorController, out AnimatorOverrideController overrides);
        return AttackPreviewGUI.ClipFor(AttackPreviewGUI.FindState(controller, state), overrides, Vector2.right);
    }
}
