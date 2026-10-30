using UnityEditor;
using UnityEngine;

// Foldout sections (General, then one per action), hides unused Projectile/Shield fields and adds a to-scale, playable preview at the end of each action.
[CustomEditor(typeof(ClassKit))]
public partial class ClassKitEditor : KitPreviewEditor
{
    private static readonly string[] ProjectileOnlyFields = { "projectileSpeed", "projectileMaxDistance", "pierceCount" };
    private static readonly string[] ShieldOnlyFields = { "shieldHoldDuration", "shieldHealth", "shieldKnockbackForce", "shieldKnockbackDuration", "shieldPushInterval", "shieldTurnSpeed", "shieldHoldAnimation" };
    private const string ControllerFolder = "Assets/Animations/Playable/";

    private static readonly InspectorSections.Section[] Sections =
    {
        new("GENERAL", "Health", "maxHealth", "healthRegenPerSecond"),
        new("GENERAL", "Mana", "maxMana", "manaRegenPerSecond"),
        new("GENERAL", "Dodge", "dodgeCharges", "dodgeRechargeSeconds", "dodgeDistance", "dodgeDuration"),
        new("ACTIONS", "Attack", "attackCooldown", "attackFxPrefab", "attackFxDistance", "attackFxOrigin", "attackFxFromAimIndicator", "attackAimMode", "attackMaxRange", "attackAimAssist", "attackAimAssistAngle", "attackDamage", "attackMoveLockDuration", "attackMoveSpeedMultiplier",
            "attackAnimSpeed", "attackSize", "attackIgnoreAimRotation", "attackFollowCaster", "attackGlowIntensity", "attackGlowSize", "attackGlowDuration", "hasProjectile", "projectileSpeed", "projectileMaxDistance", "pierceCount"),
        new("ACTIONS", "Special", "specialCooldown", "specialManaCost", "specialFxPrefab", "specialFxDistance", "specialFxOrigin", "specialFxFromAimIndicator", "specialAimMode", "specialMaxRange", "specialDamage", "specialMoveLockDuration",
            "specialMoveSpeedMultiplier", "specialAnimSpeed", "specialSize", "specialIgnoreAimRotation", "specialFollowCaster", "specialGlowIntensity", "specialGlowSize", "specialGlowDuration"),
        new("ACTIONS", "Ultimate", "ultimateCooldown", "ultimateManaCost", "ultimateFxPrefab", "ultimateFxDistance", "ultimateFxOrigin", "ultimateFxFromAimIndicator", "ultimateAimMode", "ultimateMaxRange", "ultimateDamage", "ultimateMoveLockDuration",
            "ultimateMoveSpeedMultiplier", "ultimateAnimSpeed", "ultimateSize", "ultimateIgnoreAimRotation", "ultimateFollowCaster", "ultimateGlowIntensity", "ultimateGlowSize", "ultimateGlowDuration", "hasShield", "shieldHoldDuration",
            "shieldHealth", "shieldKnockbackForce", "shieldKnockbackDuration", "shieldPushInterval", "shieldTurnSpeed", "shieldHoldAnimation"),
    };

    private enum Action { Attack, Special, Ultimate }

    // Everything the preview needs about one action, read from the kit.
    private struct ActionData
    {
        public AbilityType Ability;
        public bool FromAimIndicator;
        public GameObject FxPrefab;
        public float Distance;
        public float Size;
        public bool IgnoreRotation;
        public float AnimSpeed;
        public float MoveLock;
        public float Cooldown;
        public string StateName;
    }

    private readonly PreviewPanel[] previews = { new PreviewPanel(), new PreviewPanel(), new PreviewPanel() };
    private float aimAngle;
    private RuntimeAnimatorController controllerOverride;

    private ClassKit Kit => (ClassKit)target;

    private Vector2 AimDirection => new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));

    protected override bool AdvancePreviews(float delta)
    {
        bool repaint = false;
        for (int i = 0; i < previews.Length; i++)
            repaint |= previews[i].Tick(delta, TimelineLength(Kit, (Action)i));
        return repaint;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        bool hasProjectile = serializedObject.FindProperty("hasProjectile").boolValue;
        bool hasShield = serializedObject.FindProperty("hasShield").boolValue;

        InspectorSections.Draw(serializedObject, "ClassKitEditor.Section.", Sections, name =>
            (!hasProjectile && System.Array.IndexOf(ProjectileOnlyFields, name) >= 0)
            || (!hasShield && System.Array.IndexOf(ShieldOnlyFields, name) >= 0)
            || (hasShield && (name == "ultimateAimMode" || name == "ultimateMaxRange"))
            || (name == "attackAimAssistAngle" && !serializedObject.FindProperty("attackAimAssist").boolValue)
            || (name.EndsWith("MaxRange") && serializedObject.FindProperty(name.Replace("MaxRange", "AimMode")).enumValueIndex != (int)AbilityAimMode.HoldFree)
            || (name.EndsWith("FxOrigin") && serializedObject.FindProperty(name.Replace("FxOrigin", "FxFromAimIndicator")).boolValue),
            section =>
            {
                if (System.Enum.TryParse(section, out Action action))
                    DrawPreview(Kit, action);
            });

        serializedObject.ApplyModifiedProperties();
    }

    private static ActionData Data(ClassKit kit, Action action) => action switch
    {
        Action.Special => new ActionData { Ability = AbilityType.Special, FromAimIndicator = kit.SpecialFxFromAimIndicator, FxPrefab = kit.SpecialFxPrefab, Distance = kit.SpecialFxDistance, Size = kit.SpecialSize, IgnoreRotation = kit.SpecialIgnoreAimRotation, AnimSpeed = kit.SpecialAnimSpeed, MoveLock = kit.SpecialMoveLockDuration, Cooldown = kit.SpecialCooldown, StateName = "HeavyAttack_Tree" },
        Action.Ultimate => new ActionData { Ability = AbilityType.Ultimate, FromAimIndicator = kit.UltimateFxFromAimIndicator, FxPrefab = kit.UltimateFxPrefab, Distance = kit.UltimateFxDistance, Size = kit.UltimateSize, IgnoreRotation = kit.UltimateIgnoreAimRotation, AnimSpeed = kit.UltimateAnimSpeed, MoveLock = kit.UltimateMoveLockDuration, Cooldown = kit.UltimateCooldown, StateName = "Ultimate_Tree" },
        _ => new ActionData { Ability = AbilityType.Attack, FromAimIndicator = kit.AttackFxFromAimIndicator, FxPrefab = kit.AttackFxPrefab, Distance = kit.AttackFxDistance, Size = kit.AttackSize, IgnoreRotation = kit.AttackIgnoreAimRotation, AnimSpeed = kit.AttackAnimSpeed, MoveLock = kit.AttackMoveLockDuration, Cooldown = kit.AttackCooldown, StateName = "LightAttack_Tree" },
    };

    // The class's controller (Assets/Animations/Playable/<Class>.controller), unless one is picked in the preview.
    private RuntimeAnimatorController Controller(ClassKit kit)
    {
        if (controllerOverride != null)
            return controllerOverride;
        string className = kit.name.EndsWith("_Kit") ? kit.name.Substring(0, kit.name.Length - 4) : kit.name;
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerFolder + className + ".controller");
    }

    private AnimationClip ActionClip(ClassKit kit, string stateName) => PreviewAnimation.StateClip(Controller(kit), stateName, AimDirection);

    private float AnimLength(ClassKit kit, ActionData data)
    {
        AnimationClip clip = ActionClip(kit, data.StateName);
        return clip != null ? PreviewAnimation.FramesLength(PreviewAnimation.SpriteKeys(clip)) / Mathf.Max(data.AnimSpeed, 0.01f) : 0f;
    }

    private float TimelineLength(ClassKit kit, Action action)
    {
        if (action == Action.Ultimate && kit.HasShield)
            return ShieldTimelineLength(kit);

        ActionData data = Data(kit, action);
        return Mathf.Max(AnimLength(kit, data), FxLifetime(kit, data.FxPrefab), data.MoveLock, 0.2f) + TailSeconds;
    }

    // FX placement as Player.FxOffset, with the aim indicator center read from the Player prefab.
    private static Vector2 FxOffset(ClassKit kit, ActionData data, Vector2 direction) => kit.FxOffset(data.Ability, direction, AimIndicatorCenter());

    // Where the FX distance starts: the aim indicator center, or FX Origin mirrored when aiming left.
    private static Vector2 FxOrigin(ClassKit kit, ActionData data, Vector2 direction) => FxOffset(kit, data, direction) - direction * data.Distance;

    // The action's FxOrigin property, edited by dragging its dot in the preview.
    private SerializedProperty OriginProperty(ActionData data) => serializedObject.FindProperty(data.Ability.ToString().ToLowerInvariant() + "FxOrigin");

    // Aim angle slider and controller picker shared by every action preview, one row each so the slider keeps its bar.
    private void DrawPreviewControls(ClassKit kit)
    {
        aimAngle = EditorGUILayout.Slider("Aim Angle", aimAngle, 0f, 360f);
        controllerOverride = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Controller", Controller(kit), typeof(RuntimeAnimatorController), false);
    }
}
