using UnityEditor;
using UnityEngine;

// Foldout sections for the BossKit plus playable previews of each pattern and of the combos, replaying Boss's timing, aim and hit shapes.
[CustomEditor(typeof(BossKit))]
public partial class BossKitEditor : KitPreviewEditor
{
    private const float ContactMargin = 0.5f;

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
        InspectorSections.Draw(serializedObject, "BossKitEditor.Section.", Sections, null, section =>
        {
            if (section == "Patterns")
                DrawPreview(Preview.Combo);
            else if (System.Enum.TryParse(section, out Preview preview))
                DrawPreview(preview);
        });
        serializedObject.ApplyModifiedProperties();
    }

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

    // Same lane width as Boss.ChargeHitWidth: never narrower than the body plus a contact margin.
    private float ChargeHitWidth
    {
        get
        {
            PreviewShapes.PrefabBody(BossPrefab(), 0.5f, out _, out float radius);
            return Mathf.Max(Kit.ChargeWidth, 2f * (radius + ContactMargin));
        }
    }
}
