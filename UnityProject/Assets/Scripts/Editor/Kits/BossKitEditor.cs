using UnityEditor;
using UnityEngine;

// Foldout sections for the BossKit plus playable previews of each pattern and of the combos, replaying Boss's timing, aim and hit shapes.
[CustomEditor(typeof(BossKit))]
public partial class BossKitEditor : KitPreviewEditor
{
    private const string PrefsPrefix = "BossKitEditor.Section.";
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string PatternsSection = "Patterns";
    private const float ContactMargin = 0.5f;
    private const float FallbackBodyRadius = 0.5f;

    private static readonly InspectorSections.Section[] Sections =
    {
        new("", "General", "displayName", "maxHealth", "healthPerExtraPlayer", "scoreReward"),
        new("", "Movement", "moveSpeed", "retargetInterval", "steerInterval"),
        new("", "Patterns", "patternCooldown", "recoveryDuration", "enrageHealthFraction", "enrageTempo"),
        new("", "Charge", "chargeMinRange", "chargeCooldown", "chargeLeadSeconds", "chargeChainCount", "chargeWindup", "chargeSpeed", "chargeDistance", "chargeWidth", "chargeDamage"),
        new("", "Slam", "slamMaxRange", "slamCooldown", "slamWindup", "slamRadius", "slamDamage", "slamActiveDuration"),
        new("", "Summon", "summonKits", "summonCount", "summonInterval", "summonWindup", "summonRadius", "summonActiveDuration", "summonFxPrefabs", "summonFxOffset"),
        new("", "Animator States", "idleState", "walkState", "windupState", "attackState", "recoveryState", "chargeAnimationSpeed"),
    };

    private enum Preview { Charge, Slam, Summon, Combo }

    private readonly PreviewPanel[] previews = { new PreviewPanel(), new PreviewPanel(), new PreviewPanel(), new PreviewPanel() };
    private GameObject bossPrefab;
    private BossKit cachedKit;

    private BossKit Kit => (BossKit)target;

    // Same lane width as Boss.ChargeHitWidth: never narrower than the body plus a contact margin.
    private float ChargeHitWidth
    {
        get
        {
            PreviewShapes.PrefabBody(BossPrefab(), FallbackBodyRadius, out _, out float radius);
            return Mathf.Max(Kit.ChargeWidth, 2f * (radius + ContactMargin));
        }
    }

    private bool UsesThisKit(GameObject prefab)
    {
        Boss boss;

        if (prefab == null)
            return false;
        boss = prefab.GetComponent<Boss>();
        return boss != null && boss.Kit == Kit;
    }

    private GameObject FindBossPrefab()
    {
        GameObject prefab;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsFolder }))
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (UsesThisKit(prefab))
                return prefab;
        }
        return null;
    }

    // The prefab whose Boss component uses this kit, for the sprite, Animator and body collider.
    private GameObject BossPrefab()
    {
        if (cachedKit == Kit && bossPrefab != null)
            return bossPrefab;
        cachedKit = Kit;
        bossPrefab = FindBossPrefab();
        return bossPrefab;
    }

    private void DrawAfterSection(string section)
    {
        Preview preview;

        if (section == PatternsSection)
            DrawPreview(Preview.Combo);
        else if (System.Enum.TryParse(section, out preview))
            DrawPreview(preview);
    }

    protected override bool AdvancePreviews(float delta)
    {
        bool repaint = false;

        for (int i = 0; i < previews.Length; i++)
            repaint |= previews[i].Tick(delta, TimelineLength(BuildSteps((Preview)i)));
        return repaint;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        InspectorSections.Draw(serializedObject, PrefsPrefix, Sections, null, DrawAfterSection);
        serializedObject.ApplyModifiedProperties();
    }
}
