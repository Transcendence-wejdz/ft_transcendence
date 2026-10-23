using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Foldout sections (General, then one per action), hides unused Projectile/Shield fields and adds a to-scale, playable preview at the end of each action.
[CustomEditor(typeof(ClassKit))]
public class ClassKitEditor : Editor
{
    private static readonly string[] ProjectileOnlyFields = { "projectileSpeed", "projectileMaxDistance", "pierceCount" };
    private static readonly string[] ShieldOnlyFields = { "shieldHoldDuration", "shieldHealth", "shieldKnockbackForce", "shieldKnockbackDuration", "shieldPushInterval", "shieldTurnSpeed", "shieldHoldAnimation" };
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    private const string ControllerFolder = "Assets/Animations/Playable/";
    private const float PreviewHeight = 240f;
    private const float TailSeconds = 0.3f;
    private static readonly Color HitFill = new Color(1f, 0.25f, 0.15f, 0.3f);
    private static readonly Color HitEdge = new Color(1f, 0.3f, 0.2f, 1f);
    private static readonly Color IdleEdge = new Color(1f, 0.3f, 0.2f, 0.25f);
    private static readonly Color AnimColor = new Color(1f, 1f, 1f, 0.7f);
    private static readonly Color LockColor = new Color(0.35f, 0.75f, 1f, 0.9f);
    private static readonly Color CooldownColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);
    private static readonly Color PushFill = new Color(0.35f, 0.75f, 1f, 0.12f);
    private static readonly Color PushFlash = new Color(0.35f, 0.75f, 1f, 0.4f);
    private static readonly Color PushEdge = new Color(0.35f, 0.75f, 1f, 0.8f);
    private const float PushFlashSeconds = 0.12f;
    private const float ShieldPushQueryPadding = 0.8f;

    private enum Action { Attack, Special, Ultimate }

    private readonly AttackPreviewState[] previews = { new AttackPreviewState(), new AttackPreviewState(), new AttackPreviewState() };
    private double lastTick;
    private float aimAngle;
    private RuntimeAnimatorController controllerOverride;
    private float shieldLowerAt = -1f;

    // Everything the preview needs about one action, read from the kit and its FX prefab.
    private struct ActionData
    {
        public GameObject FxPrefab;
        public float Distance;
        public float Size;
        public bool IgnoreRotation;
        public float AnimSpeed;
        public float MoveLock;
        public float Cooldown;
        public string StateName;
    }

    private class FxLayer
    {
        public SpriteRenderer Renderer;
        public Sprite[] Frames;
        public bool Loop;
        public bool Behind;
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
        ClassKit kit = (ClassKit)target;
        for (int i = 0; i < previews.Length; i++)
            repaint |= AttackPreviewGUI.Tick(previews[i], delta, TimelineLength(kit, (Action)i));
        if (repaint)
            Repaint();
    }

    private static readonly InspectorSections.Section[] Sections =
    {
        new("GENERAL", "Health", "maxHealth", "healthRegenPerSecond"),
        new("GENERAL", "Mana", "maxMana", "manaRegenPerSecond"),
        new("GENERAL", "Dodge", "dodgeCharges", "dodgeRechargeSeconds", "dodgeDistance", "dodgeDuration"),
        new("ACTIONS", "Attack", "attackCooldown", "attackFxPrefab", "attackFxDistance", "attackDamage", "attackMoveLockDuration", "attackMoveSpeedMultiplier",
            "attackAnimSpeed", "attackSize", "attackIgnoreAimRotation", "attackFollowCaster", "hasProjectile", "projectileSpeed", "projectileMaxDistance", "pierceCount", "hasFXOnHit", "onHitFX"),
        new("ACTIONS", "Special", "specialCooldown", "specialManaCost", "specialFxPrefab", "specialFxDistance", "specialDamage", "specialMoveLockDuration",
            "specialMoveSpeedMultiplier", "specialAnimSpeed", "specialSize", "specialIgnoreAimRotation", "specialFollowCaster"),
        new("ACTIONS", "Ultimate", "ultimateCooldown", "ultimateManaCost", "ultimateFxPrefab", "ultimateFxDistance", "ultimateDamage", "ultimateMoveLockDuration",
            "ultimateMoveSpeedMultiplier", "ultimateAnimSpeed", "ultimateSize", "ultimateIgnoreAimRotation", "ultimateFollowCaster", "hasShield", "shieldHoldDuration",
            "shieldHealth", "shieldKnockbackForce", "shieldKnockbackDuration", "shieldPushInterval", "shieldTurnSpeed", "shieldHoldAnimation"),
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        bool hasProjectile = serializedObject.FindProperty("hasProjectile").boolValue;
        bool hasShield = serializedObject.FindProperty("hasShield").boolValue;
        ClassKit kit = (ClassKit)target;

        InspectorSections.Draw(serializedObject, "ClassKitEditor.Section.", Sections, name =>
            (!hasProjectile && System.Array.IndexOf(ProjectileOnlyFields, name) >= 0)
            || (!hasShield && System.Array.IndexOf(ShieldOnlyFields, name) >= 0),
            section =>
            {
                if (System.Enum.TryParse(section, out Action action))
                    DrawPreview(kit, action);
            });

        serializedObject.ApplyModifiedProperties();
    }

    private static ActionData Data(ClassKit kit, Action action) => action switch
    {
        Action.Special => new ActionData { FxPrefab = kit.SpecialFxPrefab, Distance = kit.SpecialFxDistance, Size = kit.SpecialSize, IgnoreRotation = kit.SpecialIgnoreAimRotation, AnimSpeed = kit.SpecialAnimSpeed, MoveLock = kit.SpecialMoveLockDuration, Cooldown = kit.SpecialCooldown, StateName = "HeavyAttack_Tree" },
        Action.Ultimate => new ActionData { FxPrefab = kit.UltimateFxPrefab, Distance = kit.UltimateFxDistance, Size = kit.UltimateSize, IgnoreRotation = kit.UltimateIgnoreAimRotation, AnimSpeed = kit.UltimateAnimSpeed, MoveLock = kit.UltimateMoveLockDuration, Cooldown = kit.UltimateCooldown, StateName = "Ultimate_Tree" },
        _ => new ActionData { FxPrefab = kit.AttackFxPrefab, Distance = kit.AttackFxDistance, Size = kit.AttackSize, IgnoreRotation = kit.AttackIgnoreAimRotation, AnimSpeed = kit.AttackAnimSpeed, MoveLock = kit.AttackMoveLockDuration, Cooldown = kit.AttackCooldown, StateName = "LightAttack_Tree" },
    };

    private float TimelineLength(ClassKit kit, Action action)
    {
        if (action == Action.Ultimate && kit.HasShield)
            return ShieldLowerAt(kit) + ShieldFrames(kit.UltimateFxPrefab, "endFrames", out float rate).Length / Mathf.Max(rate, 1f) + TailSeconds;

        ActionData data = Data(kit, action);
        return Mathf.Max(AnimLength(kit, data), FxLifetime(kit, data.FxPrefab), data.MoveLock, 0.2f) + TailSeconds;
    }

    // Projectiles live until their max distance; flipbook FX until their last frame, the whole time with a live hitbox.
    private static float FxLifetime(ClassKit kit, GameObject fxPrefab)
    {
        if (fxPrefab == null)
            return 0f;
        if (fxPrefab.GetComponent<ArrowProjectile>() != null)
            return kit.ProjectileSpeed > 0f ? kit.ProjectileMaxDistance / kit.ProjectileSpeed : 0f;

        float frameRate = 24f;
        int frameCount = 0;
        MonoBehaviour fx = FxComponent(fxPrefab);
        if (fx != null)
        {
            var so = new SerializedObject(fx);
            SerializedProperty rate = so.FindProperty("frameRate");
            if (rate != null)
                frameRate = rate.floatValue;
            foreach (string field in new[] { "frames", "frontFrames", "backFrames" })
            {
                SerializedProperty frames = so.FindProperty(field);
                if (frames != null && frames.isArray)
                    frameCount = Mathf.Max(frameCount, frames.arraySize);
            }
        }
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

    // Sprite layers of the FX: SlashAttackFX/ArrowProjectile frames on their renderer, LayeredAttackFX front/back pairs.
    private static List<FxLayer> FxLayers(GameObject fxPrefab)
    {
        var layers = new List<FxLayer>();
        MonoBehaviour fx = fxPrefab != null ? FxComponent(fxPrefab) : null;
        if (fx == null)
            return layers;

        var so = new SerializedObject(fx);
        Sprite[] Read(string field)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null || !property.isArray)
                return null;
            var sprites = new Sprite[property.arraySize];
            for (int i = 0; i < sprites.Length; i++)
                sprites[i] = property.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
            return sprites;
        }
        SpriteRenderer Renderer(string field) => so.FindProperty(field)?.objectReferenceValue as SpriteRenderer;

        if (fx is LayeredAttackFX)
        {
            layers.Add(new FxLayer { Renderer = Renderer("backRenderer"), Frames = Read("backFrames"), Behind = true });
            layers.Add(new FxLayer { Renderer = Renderer("frontRenderer"), Frames = Read("frontFrames") });
        }
        else if (fx is ArrowProjectile)
        {
            SpriteRenderer baseRenderer = fxPrefab.GetComponent<SpriteRenderer>();
            SpriteRenderer animated = Renderer("fxSpriteRenderer");
            if (animated != null && baseRenderer != null)
                layers.Add(new FxLayer { Renderer = baseRenderer, Frames = new[] { baseRenderer.sprite }, Loop = true });
            layers.Add(new FxLayer { Renderer = animated != null ? animated : baseRenderer, Frames = Read("frames"), Loop = true });
        }
        else
        {
            layers.Add(new FxLayer { Renderer = fxPrefab.GetComponent<SpriteRenderer>(), Frames = Read("frames") });
        }
        layers.RemoveAll(layer => layer.Renderer == null);
        return layers;
    }

    private RuntimeAnimatorController Controller(ClassKit kit)
    {
        if (controllerOverride != null)
            return controllerOverride;
        string className = kit.name.EndsWith("_Kit") ? kit.name.Substring(0, kit.name.Length - 4) : kit.name;
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerFolder + className + ".controller");
    }

    private float AnimLength(ClassKit kit, ActionData data)
    {
        AnimationClip clip = ActionClip(kit, data.StateName);
        return clip != null ? AttackPreviewGUI.FramesLength(AttackPreviewGUI.SpriteKeys(clip)) / Mathf.Max(data.AnimSpeed, 0.01f) : 0f;
    }

    private AnimationClip ActionClip(ClassKit kit, string stateName)
    {
        RuntimeAnimatorController runtime = Controller(kit);
        if (runtime == null)
            return null;
        AnimatorController controller = AttackPreviewGUI.ResolveController(runtime, out AnimatorOverrideController overrides);
        return AttackPreviewGUI.ClipFor(AttackPreviewGUI.FindState(controller, stateName), overrides, AimDirection);
    }

    private Vector2 AimDirection => new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));

    private void DrawPreview(ClassKit kit, Action action)
    {
        string key = "ClassKitEditor.Preview." + action;
        bool open = EditorPrefs.GetBool(key, true);
        bool newOpen = EditorGUILayout.Foldout(open, action + " Preview", true);
        if (newOpen != open)
            EditorPrefs.SetBool(key, newOpen);
        if (!newOpen)
            return;

        if (action == Action.Ultimate && kit.HasShield)
        {
            DrawShieldPreview(kit, previews[(int)action]);
            return;
        }

        ActionData data = Data(kit, action);
        AttackPreviewState preview = previews[(int)action];
        float lifetime = FxLifetime(kit, data.FxPrefab);
        float animLength = AnimLength(kit, data);
        float length = TimelineLength(kit, action);

        using (new EditorGUILayout.HorizontalScope())
        {
            aimAngle = EditorGUILayout.Slider("Aim Angle", aimAngle, 0f, 360f);
            controllerOverride = (RuntimeAnimatorController)EditorGUILayout.ObjectField(Controller(kit), typeof(RuntimeAnimatorController), false, GUILayout.Width(140f));
        }
        AttackPreviewGUI.DrawTimeline(preview, length,
            new[] { (animLength, AnimColor), (data.MoveLock, LockColor), (data.Cooldown, CooldownColor) },
            new[] { (0f, lifetime, HitEdge) });

        Rect rect = GUILayoutUtility.GetRect(10f, PreviewHeight, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("Red: hitbox (live while the bar's red span runs). Markers: animation end (white), move lock end (blue), cooldown (grey). 1 square = 1 unit. Scroll to zoom, drag to pan.", EditorStyles.wordWrappedMiniLabel);

        Vector2 direction = AimDirection;
        bool projectile = data.FxPrefab != null && data.FxPrefab.GetComponent<ArrowProjectile>() != null;
        float extent = Mathf.Max(FitRadius(kit, data, direction, projectile), 1.5f) * 1.15f;
        Vector2 anchor = rect.center;
        float fitScale = Mathf.Min(rect.width, rect.height) * 0.5f / extent;
        float scale = AttackPreviewGUI.Scale(preview, fitScale);
        AttackPreviewGUI.HandleZoomPan(preview, rect, anchor, fitScale, Repaint);
        if (Event.current.type != EventType.Repaint)
            return;

        EditorGUI.DrawRect(rect, AttackPreviewGUI.Background);
        GUI.BeginClip(rect);
        Vector2 origin = anchor + preview.Pan - rect.position;
        AttackPreviewGUI.DrawGrid(rect.size, origin, scale);

        float time = preview.Time;
        Matrix4x4 fxMatrix = FxMatrix(kit, data, direction, projectile, time);
        List<FxLayer> layers = FxLayers(data.FxPrefab);
        bool fxAlive = data.FxPrefab != null && time <= lifetime;
        if (fxAlive)
            DrawFxLayers(layers, data.FxPrefab, fxMatrix, origin, scale, time, behind: true);
        DrawCharacter(kit, data, direction, origin, scale, time, animLength, animLength);
        if (fxAlive)
            DrawFxLayers(layers, data.FxPrefab, fxMatrix, origin, scale, time, behind: false);

        if (data.FxPrefab != null)
        {
            Matrix4x4 hitMatrix = fxAlive ? fxMatrix : FxMatrix(kit, data, direction, projectile, 0f);
            foreach ((Vector2[] points, bool closed) in AttackPreviewGUI.ColliderOutlines(data.FxPrefab, hitMatrix))
                AttackPreviewGUI.DrawOutline(points, closed, origin, scale, fxAlive ? HitFill : Color.clear, fxAlive ? HitEdge : IdleEdge);
        }

        if (projectile)
        {
            Handles.color = IdleEdge;
            Handles.DrawAAPolyLine(2f, origin, AttackPreviewGUI.ToGui(origin, direction * (data.Distance + kit.ProjectileMaxDistance), scale));
        }
        GUI.EndClip();
    }

    // The second-click time being simulated, clamped to the hold duration (the shield drops on its own then).
    private float ShieldLowerAt(ClassKit kit)
    {
        float hold = Mathf.Max(kit.ShieldHoldDuration, 0.05f);
        if (shieldLowerAt < 0f || shieldLowerAt > hold)
            shieldLowerAt = hold;
        return shieldLowerAt;
    }

    private static Sprite[] ShieldFrames(GameObject fxPrefab, string field, out float frameRate)
    {
        frameRate = 24f;
        ShieldHoldFX fx = fxPrefab != null ? fxPrefab.GetComponent<ShieldHoldFX>() : null;
        if (fx == null)
            return new Sprite[0];
        var so = new SerializedObject(fx);
        SerializedProperty rate = so.FindProperty("frameRate");
        if (rate != null)
            frameRate = rate.floatValue;
        SerializedProperty property = so.FindProperty(field);
        var sprites = new Sprite[property != null && property.isArray ? property.arraySize : 0];
        for (int i = 0; i < sprites.Length; i++)
            sprites[i] = property.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
        return sprites;
    }

    // ShieldHoldFX stages: start once, loop while raised, end once after it is lowered (second click, timer or broken).
    private static Sprite ShieldSprite(GameObject fxPrefab, float time, float lowerAt)
    {
        Sprite[] start = ShieldFrames(fxPrefab, "startFrames", out float rate);
        if (time >= lowerAt)
            return AttackPreviewGUI.SampleSprites(ShieldFrames(fxPrefab, "endFrames", out _), rate, time - lowerAt, false);

        float startLength = start.Length / Mathf.Max(rate, 1f);
        if (time < startLength)
            return AttackPreviewGUI.SampleSprites(start, rate, time, false);
        return AttackPreviewGUI.SampleSprites(ShieldFrames(fxPrefab, "loopFrames", out _), rate, time - startLength, true);
    }

    // Wall collider (red, blocks hits while raised) and push query (blue, Player.ApplyPush: the FX capsule scaled like the FX + padding at FxDistance, flashing on each push).
    private void DrawShieldPreview(ClassKit kit, AttackPreviewState preview)
    {
        ActionData data = Data(kit, Action.Ultimate);
        float hold = Mathf.Max(kit.ShieldHoldDuration, 0.05f);
        shieldLowerAt = EditorGUILayout.Slider(new GUIContent("Lowered At", "Simulates the second click that drops the shield early; at the max it drops on its own when Shield Hold Duration ends."), ShieldLowerAt(kit), 0.05f, hold);
        float lowerAt = shieldLowerAt;
        float length = TimelineLength(kit, Action.Ultimate);

        var markers = new List<(float, Color)> { (lowerAt, AnimColor), (lowerAt + kit.UltimateCooldown, CooldownColor) };
        if (kit.ShieldPushInterval > 0f)
        {
            for (float push = 0f; push < lowerAt && markers.Count < 200; push += kit.ShieldPushInterval)
                markers.Add((push, LockColor));
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            aimAngle = EditorGUILayout.Slider("Aim Angle", aimAngle, 0f, 360f);
            controllerOverride = (RuntimeAnimatorController)EditorGUILayout.ObjectField(Controller(kit), typeof(RuntimeAnimatorController), false, GUILayout.Width(140f));
        }
        AttackPreviewGUI.DrawTimeline(preview, length, markers, new[] { (0f, lowerAt, HitEdge) });

        Rect rect = GUILayoutUtility.GetRect(10f, PreviewHeight, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("Red: shield wall (blocks hits while raised, red span). Blue: enemy push zone, flashing on each push (blue markers). Markers: lowered (white), cooldown end (grey). Scroll to zoom, drag to pan.", EditorStyles.wordWrappedMiniLabel);

        Vector2 direction = AimDirection;
        CapsuleCollider2D template = data.FxPrefab != null ? data.FxPrefab.GetComponent<CapsuleCollider2D>() : null;
        Vector2 prefabScale = template != null ? (Vector2)template.transform.lossyScale : Vector2.one;
        Vector2 pushSize = template != null ? new Vector2(template.size.x * Mathf.Abs(prefabScale.x), template.size.y * Mathf.Abs(prefabScale.y)) * kit.UltimateSize + new Vector2(ShieldPushQueryPadding, ShieldPushQueryPadding) : Vector2.zero;
        float extent = Mathf.Max(FitRadius(kit, data, direction, false), data.Distance + Mathf.Max(pushSize.x, pushSize.y) * 0.5f, 1.5f) * 1.15f;
        Vector2 anchor = rect.center;
        float fitScale = Mathf.Min(rect.width, rect.height) * 0.5f / extent;
        float scale = AttackPreviewGUI.Scale(preview, fitScale);
        AttackPreviewGUI.HandleZoomPan(preview, rect, anchor, fitScale, Repaint);
        if (Event.current.type != EventType.Repaint)
            return;

        EditorGUI.DrawRect(rect, AttackPreviewGUI.Background);
        GUI.BeginClip(rect);
        Vector2 origin = anchor + preview.Pan - rect.position;
        AttackPreviewGUI.DrawGrid(rect.size, origin, scale);

        float time = preview.Time;
        bool raised = time < lowerAt;
        float animLength = AnimLength(kit, data);
        DrawCharacter(kit, data, direction, origin, scale, time, kit.ShieldHoldAnimation ? Mathf.Max(animLength, lowerAt) : animLength, animLength);

        if (data.FxPrefab != null)
        {
            Matrix4x4 fxMatrix = FxMatrix(kit, data, direction, false, 0f);
            SpriteRenderer shieldRenderer = new SerializedObject(data.FxPrefab.GetComponent<ShieldHoldFX>()).FindProperty("spriteRenderer").objectReferenceValue as SpriteRenderer;
            if (shieldRenderer == null)
                shieldRenderer = data.FxPrefab.GetComponent<SpriteRenderer>();
            Sprite sprite = ShieldSprite(data.FxPrefab, time, lowerAt);
            if (shieldRenderer != null && sprite != null)
            {
                Matrix4x4 m = fxMatrix * data.FxPrefab.transform.worldToLocalMatrix * shieldRenderer.transform.localToWorldMatrix;
                Vector2 x = m.GetColumn(0);
                Vector2 y = m.GetColumn(1);
                AttackPreviewGUI.DrawSprite(sprite, origin, scale, m.MultiplyPoint3x4(Vector3.zero), new Vector2(x.magnitude, y.magnitude), Mathf.Atan2(x.y, x.x) * Mathf.Rad2Deg, shieldRenderer.flipX, shieldRenderer.color);
            }

            if (template != null)
            {
                float facing = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                bool flash = raised && kit.ShieldPushInterval > 0f && Mathf.Repeat(time, kit.ShieldPushInterval) < PushFlashSeconds;
                Vector2[] push = AttackPreviewGUI.CapsuleOutline(direction * data.Distance, pushSize, template.direction, facing);
                AttackPreviewGUI.DrawOutline(push, true, origin, scale, raised ? (flash ? PushFlash : PushFill) : Color.clear, raised ? PushEdge : IdleEdge);
            }

            foreach ((Vector2[] points, bool closed) in AttackPreviewGUI.ColliderOutlines(data.FxPrefab, fxMatrix))
                AttackPreviewGUI.DrawOutline(points, closed, origin, scale, raised ? HitFill : Color.clear, raised ? HitEdge : IdleEdge);
        }
        GUI.EndClip();
    }

    // FX root placement exactly as Player.SpawnLocalFx: player pivot + aim * distance, aim rotation unless ignored, prefab scale x Size; projectiles then fly along the aim.
    private static Matrix4x4 FxMatrix(ClassKit kit, ActionData data, Vector2 direction, bool projectile, float time)
    {
        Vector2 position = direction * data.Distance;
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
        if (projectile)
            return data.Distance + kit.ProjectileMaxDistance;

        float radius = data.Distance;
        foreach ((Vector2[] points, bool _) in AttackPreviewGUI.ColliderOutlines(data.FxPrefab, FxMatrix(kit, data, direction, false, 0f)))
        {
            foreach (Vector2 point in points)
                radius = Mathf.Max(radius, point.magnitude);
        }
        return radius;
    }

    private static void DrawFxLayers(List<FxLayer> layers, GameObject fxPrefab, Matrix4x4 fxMatrix, Vector2 origin, float scale, float time, bool behind)
    {
        foreach (FxLayer layer in layers)
        {
            if (layer.Behind != behind)
                continue;
            Sprite sprite = layer.Frames != null && layer.Frames.Length > 0
                ? AttackPreviewGUI.SampleSprites(layer.Frames, FrameRate(fxPrefab), time, layer.Loop)
                : layer.Renderer.sprite;
            if (sprite == null)
                continue;

            Matrix4x4 m = fxMatrix * fxPrefab.transform.worldToLocalMatrix * layer.Renderer.transform.localToWorldMatrix;
            Vector2 x = m.GetColumn(0);
            Vector2 y = m.GetColumn(1);
            float determinant = x.x * y.y - x.y * y.x;
            Vector2 worldScale = new Vector2(x.magnitude * Mathf.Sign(determinant), y.magnitude);
            float rotation = Mathf.Atan2(x.y, x.x) * Mathf.Rad2Deg;
            if (determinant < 0f)
                rotation += 180f;
            AttackPreviewGUI.DrawSprite(sprite, origin, scale, m.MultiplyPoint3x4(Vector3.zero), worldScale, rotation, layer.Renderer.flipX, layer.Renderer.color);
        }
    }

    private static float FrameRate(GameObject fxPrefab)
    {
        MonoBehaviour fx = FxComponent(fxPrefab);
        SerializedProperty rate = fx != null ? new SerializedObject(fx).FindProperty("frameRate") : null;
        return rate != null ? rate.floatValue : 24f;
    }

    // Action clip (scaled by its anim speed) until holdUntil (its last frame held past animLength, like ShieldHeld), then Idle; mirrored when aiming left like the in-game flipX.
    private void DrawCharacter(ClassKit kit, ActionData data, Vector2 direction, Vector2 origin, float scale, float time, float holdUntil, float animLength)
    {
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        SpriteRenderer spriteRenderer = player != null ? player.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (spriteRenderer == null)
            return;

        Sprite sprite;
        if (time < holdUntil)
            sprite = AttackPreviewGUI.SampleFrames(AttackPreviewGUI.SpriteKeys(ActionClip(kit, data.StateName)), Mathf.Min(time, animLength) * data.AnimSpeed, false);
        else
            sprite = AttackPreviewGUI.SampleFrames(AttackPreviewGUI.SpriteKeys(ActionClip(kit, "Idle_Tree")), time - holdUntil, true);
        if (sprite == null)
            sprite = spriteRenderer.sprite;

        Vector2 offset = (Vector2)spriteRenderer.transform.position - (Vector2)player.transform.position;
        AttackPreviewGUI.DrawSprite(sprite, origin, scale, offset, spriteRenderer.transform.lossyScale, 0f, direction.x < -0.01f, Color.white);
    }
}
