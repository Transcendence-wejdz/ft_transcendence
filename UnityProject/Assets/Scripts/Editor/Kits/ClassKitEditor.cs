using UnityEditor;
using UnityEngine;

// General foldouts, then one section per action slot: its AbilityKit reference with that ability's inspector and preview drawn inline.
[CustomEditor(typeof(ClassKit))]
public class ClassKitEditor : Editor
{
    private const string PrefsPrefix = "ClassKitEditor.Section.";
    private const int SlotCount = 3;

    private static readonly InspectorSections.Section[] Sections =
    {
        new("GENERAL", "Health", "maxHealth", "healthRegenPerSecond"),
        new("GENERAL", "Mana", "maxMana", "manaRegenPerSecond"),
        new("GENERAL", "Dodge", "dodgeCharges", "dodgeRechargeSeconds", "dodgeDistance", "dodgeDuration"),
        new("ACTIONS", "Attack", "attack"),
        new("ACTIONS", "Special", "specials"),
        new("ACTIONS", "Ultimate", "ultimates"),
    };

    private readonly AbilityKitEditor[] abilityEditors = new AbilityKitEditor[SlotCount];

    private ClassKit Kit => (ClassKit)target;

    // Recreates the slot's nested editor when the ability asset changed.
    private AbilityKitEditor EditorFor(AbilityType slot, AbilityKit ability)
    {
        AbilityKitEditor editor = abilityEditors[(int)slot];

        if (editor != null && editor.target == ability)
            return editor;
        if (editor != null)
            DestroyImmediate(editor);
        editor = (AbilityKitEditor)CreateEditor(ability, typeof(AbilityKitEditor));
        abilityEditors[(int)slot] = editor;
        return editor;
    }

    private static string AbilityTitle(AbilityKit ability)
    {
        return ability.name + " (" + ObjectNames.NicifyVariableName(ability.GetType().Name) + ")";
    }

    // The slot's ability (the first entry for Special/Ultimate) in a box, edited in place on its own asset.
    private void DrawAbility(AbilityType slot)
    {
        AbilityKit ability = Kit.Ability(slot);
        AbilityKitEditor editor;

        if (ability == null)
        {
            EditorGUILayout.HelpBox("No ability in this slot: the action does nothing.", MessageType.Info);
            return;
        }
        editor = EditorFor(slot, ability);
        editor.OwnerKit = Kit;
        editor.Slot = slot;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(AbilityTitle(ability), EditorStyles.miniBoldLabel);
        editor.OnInspectorGUI();
        EditorGUILayout.EndVertical();
    }

    private void DrawAfterSection(string section)
    {
        AbilityType slot;

        if (System.Enum.TryParse(section, out slot))
            DrawAbility(slot);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        InspectorSections.Draw(serializedObject, PrefsPrefix, Sections, null, DrawAfterSection);
        serializedObject.ApplyModifiedProperties();
    }

    private void OnDisable()
    {
        foreach (AbilityKitEditor editor in abilityEditors)
        {
            if (editor != null)
                DestroyImmediate(editor);
        }
    }
}
