using UnityEditor;
using UnityEngine;

// Foldout sections, only the fields relevant to the kit (ranged or melee, attack shape) and a to-scale, playable preview of the attack timing.
[CustomEditor(typeof(EnemyKit))]
public partial class EnemyKitEditor : KitPreviewEditor
{
    private const string PrefsPrefix = "EnemyKitEditor.Section.";
    private const string AttackSection = "Attack";
    private const string EncirclePrefix = "encircle";
    private const float MinCycle = 0.1f;

    private static readonly string[] RangedOnlyFields = { "projectilePrefab", "projectileSpeed", "projectileMaxDistance", "projectileArcHeight" };
    private static readonly string[] MeleeOnlyFields = { "attackShape", "hitRangeTolerance", "attackOrigin" };
    private static readonly string[] RangedBehaviourFields = { "keepAwayDistance", "retreatSpeedMultiplier", "rangedSpacing" };

    private static readonly InspectorSections.Section[] Sections =
    {
        new("", "General", "tier", "maxHealth", "xpReward", "scoreReward", "moveSpeed"),
        new("", "Attack", "attackShape", "attackRange", "attackAngle", "attackWidth", "attackOrigin", "damage", "attackCooldown", "attackHitDelay",
            "attackAnimationDuration", "attackMoveSpeedMultiplier", "hitRangeTolerance", "isRanged", "projectilePrefab", "projectileSpeed", "projectileMaxDistance", "projectileArcHeight"),
        new("", "Behaviour", "behaviour", "keepAwayDistance", "retreatSpeedMultiplier", "rangedSpacing", "encircleRadius", "encircleJoinDistance", "encircleSlotTolerance", "encircleMaxFlankTime", "encircleRegroupChance"),
        new("", "Targeting", "retargetInterval", "steerInterval", "directChaseRange", "lineOfSightInterval", "maxDistanceFromPlayers", "leashCheckInterval"),
        new("", "Visual", "enemyPrefab", "idleStateName", "runStateName", "attackStateName"),
    };

    private bool isRanged;
    private EnemyAttackShape shape;
    private EnemyBehaviour behaviour;

    private EnemyKit Kit => (EnemyKit)target;

    private static bool Contains(string[] fields, string name)
    {
        return System.Array.IndexOf(fields, name) >= 0;
    }

    private bool IsHiddenByAttack(string name)
    {
        if (isRanged)
            return Contains(MeleeOnlyFields, name) || name == "attackAngle" || name == "attackWidth";
        if (Contains(RangedOnlyFields, name))
            return true;
        if (name == "attackAngle")
            return shape != EnemyAttackShape.Cone;
        if (name == "attackWidth")
            return shape != EnemyAttackShape.Lane;
        return false;
    }

    private bool IsHiddenByBehaviour(string name)
    {
        if (behaviour != EnemyBehaviour.Ranged && Contains(RangedBehaviourFields, name))
            return true;
        return behaviour != EnemyBehaviour.Encircle && name.StartsWith(EncirclePrefix);
    }

    private bool IsHidden(string name)
    {
        return IsHiddenByAttack(name) || IsHiddenByBehaviour(name);
    }

    private void DrawAfterSection(string section)
    {
        if (section == AttackSection)
            DrawPreview(Kit);
    }

    // Projectile flight time for ranged kits, else the hit delay plus its flash.
    private static float HitTime(EnemyKit kit)
    {
        if (!kit.IsRanged)
            return kit.AttackHitDelay + FlashSeconds;
        if (kit.ProjectileSpeed <= 0f)
            return 0f;
        return kit.ProjectileMaxDistance / kit.ProjectileSpeed;
    }

    // One attack cycle: the longer of the animation and the hit (or the projectile flight), plus a short tail before looping.
    private static float TimelineLength(EnemyKit kit)
    {
        return Mathf.Max(kit.AttackAnimationDuration, HitTime(kit), MinCycle) + TailSeconds;
    }

    protected override bool AdvancePreviews(float delta)
    {
        return preview.Tick(delta, TimelineLength(Kit));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        isRanged = serializedObject.FindProperty("isRanged").boolValue;
        shape = (EnemyAttackShape)serializedObject.FindProperty("attackShape").enumValueIndex;
        behaviour = (EnemyBehaviour)serializedObject.FindProperty("behaviour").enumValueIndex;
        InspectorSections.Draw(serializedObject, PrefsPrefix, Sections, IsHidden, DrawAfterSection);
        serializedObject.ApplyModifiedProperties();
    }
}
