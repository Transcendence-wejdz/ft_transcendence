using UnityEditor;
using UnityEngine;

// Kind first (it picks which sections show), foldout sections for one action, then a to-scale, playable preview on the class that uses it (also drawn inline by ClassKitEditor).
[CustomEditor(typeof(AbilityKit), true)]
public partial class AbilityKitEditor : KitPreviewEditor
{
    private const string PrefsPrefix = "AbilityKitEditor.Section.";
    private const string ControllerFolder = "Assets/Animations/Playable/";
    private const string ControllerExtension = ".controller";
    private const string KitSuffix = "_Kit";
    private const float MinAnimSpeed = 0.01f;
    private const float MinCycle = 0.2f;

    private static readonly string[] ProjectileFields = { "projectileSpeed", "projectileMaxDistance", "pierceCount" };
    private static readonly string[] ShieldFields = { "shieldHoldDuration", "shieldHealth", "shieldKnockbackForce", "shieldKnockbackDuration", "shieldPushInterval", "shieldTurnSpeed", "shieldHoldAnimation" };

    private static readonly InspectorSections.Section[] BaseSections =
    {
        new("ABILITY", "Type", "kind"),
        new("ABILITY", "Use", "cooldown", "manaCost", "moveLockDuration", "moveSpeedMultiplier", "animSpeed"),
        new("ABILITY", "FX", "fxPrefab", "fxDistance", "fxOrigin", "fxFromAimIndicator", "size", "ignoreAimRotation", "followCaster"),
        new("ABILITY", "Aim", "aimMode", "maxRange", "aimAssist", "aimAssistAngle"),
        new("ABILITY", "Damage", "damage"),
        new("ABILITY", "Glow", "glowIntensity", "glowSize", "glowDuration"),
        new("ABILITY", "Projectile", ProjectileFields),
        new("ABILITY", "Shield", ShieldFields),
    };

    private readonly PreviewPanel preview = new PreviewPanel();
    private float aimAngle;
    private RuntimeAnimatorController controllerOverride;
    private bool contextResolved;
    private AbilityKind kind;

    // Class and slot the action is previewed on, set by ClassKitEditor or found among the ClassKits using it.
    public ClassKit OwnerKit { get; set; }
    public AbilityType Slot { get; set; }

    private AbilityKit Kit => (AbilityKit)target;

    private Vector2 AimDirection => new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));

    private bool BoolField(string name)
    {
        return serializedObject.FindProperty(name).boolValue;
    }

    private bool IsHidden(string name)
    {
        bool isShield = kind == AbilityKind.Shield;

        if (kind != AbilityKind.Projectile && System.Array.IndexOf(ProjectileFields, name) >= 0)
            return true;
        if (!isShield && System.Array.IndexOf(ShieldFields, name) >= 0)
            return true;
        if (isShield && (name == "aimMode" || name == "maxRange"))
            return true;
        if (name == "aimAssistAngle")
            return !BoolField("aimAssist");
        if (name == "maxRange")
            return serializedObject.FindProperty("aimMode").enumValueIndex != (int)AbilityAimMode.HoldFree;
        if (name == "fxOrigin")
            return BoolField("fxFromAimIndicator");
        return false;
    }

    private bool UsesThisKit(ClassKit kit, out AbilityType usedSlot)
    {
        usedSlot = default;
        if (kit == null)
            return false;
        foreach (AbilityType slot in System.Enum.GetValues(typeof(AbilityType)))
        {
            if (kit.Ability(slot) != Kit)
                continue;
            usedSlot = slot;
            return true;
        }
        return false;
    }

    // A selected ability asset previews on the first ClassKit referencing it, in that slot.
    private void ResolveContext()
    {
        ClassKit kit;
        AbilityType slot;

        if (contextResolved || OwnerKit != null)
            return;
        contextResolved = true;
        foreach (string guid in AssetDatabase.FindAssets("t:ClassKit"))
        {
            kit = AssetDatabase.LoadAssetAtPath<ClassKit>(AssetDatabase.GUIDToAssetPath(guid));
            if (!UsesThisKit(kit, out slot))
                continue;
            OwnerKit = kit;
            Slot = slot;
            return;
        }
    }

    private string SlotStateName()
    {
        if (Slot == AbilityType.Special)
            return "HeavyAttack_Tree";
        if (Slot == AbilityType.Ultimate)
            return "Ultimate_Tree";
        return "LightAttack_Tree";
    }

    // The owner class's controller (Assets/Animations/Playable/<Class>.controller), unless one is picked in the preview.
    private RuntimeAnimatorController Controller()
    {
        string className;

        if (controllerOverride != null || OwnerKit == null)
            return controllerOverride;
        className = OwnerKit.name;
        if (className.EndsWith(KitSuffix))
            className = className.Substring(0, className.Length - KitSuffix.Length);
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerFolder + className + ControllerExtension);
    }

    private AnimationClip ActionClip(string stateName)
    {
        return PreviewAnimation.StateClip(Controller(), stateName, AimDirection);
    }

    private float AnimLength(AbilityKit kit)
    {
        AnimationClip clip = ActionClip(SlotStateName());

        if (clip == null)
            return 0f;
        return PreviewAnimation.FramesLength(PreviewAnimation.SpriteKeys(clip)) / Mathf.Max(kit.AnimSpeed, MinAnimSpeed);
    }

    private float TimelineLength(AbilityKit kit)
    {

        if (kit.Kind == AbilityKind.Shield)
            return ShieldTimelineLength(kit);
        return Mathf.Max(AnimLength(kit), FxLifetime(kit, kit.FxPrefab), kit.MoveLockDuration, MinCycle) + TailSeconds;
    }

    protected override bool AdvancePreviews(float delta)
    {
        return preview.Tick(delta, TimelineLength(Kit));
    }

    public override void OnInspectorGUI()
    {
        ResolveContext();
        serializedObject.Update();
        kind = (AbilityKind)serializedObject.FindProperty("kind").enumValueIndex;
        InspectorSections.Draw(serializedObject, PrefsPrefix, BaseSections, IsHidden);
        serializedObject.ApplyModifiedProperties();
        DrawPreview(Kit);
    }
}
