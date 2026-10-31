using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Foldout sections for custom inspectors (open state kept in EditorPrefs), with optional group titles and a hook to draw extras at the end of a section; a section whose fields are all hidden is skipped.
public static class InspectorSections
{
    public readonly struct Section
    {
        public readonly string Group;
        public readonly string Title;
        public readonly string[] Properties;

        public Section(string group, string title, params string[] properties)
        {
            Group = group;
            Title = title;
            Properties = properties;
        }
    }

    private const string ScriptProperty = "m_Script";
    private const string OtherTitle = "Other";
    private const float GroupSpacing = 8f;
    private const float SectionSpacing = 2f;

    private static bool IsHidden(System.Func<string, bool> isHidden, string name)
    {
        return isHidden != null && isHidden(name);
    }

    private static bool AllHidden(System.Func<string, bool> isHidden, string[] properties)
    {
        if (isHidden == null)
            return false;
        foreach (string name in properties)
        {
            if (!isHidden(name))
                return false;
        }
        return true;
    }

    // Visible top-level properties that no section lists.
    private static List<string> UnlistedProperties(SerializedObject serializedObject, IList<Section> sections)
    {
        HashSet<string> listed = new HashSet<string> { ScriptProperty };
        List<string> others = new List<string>();
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        foreach (Section section in sections)
            listed.UnionWith(section.Properties);
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (!listed.Contains(iterator.name))
                others.Add(iterator.name);
        }
        return others;
    }

    private static void DrawProperties(SerializedObject serializedObject, string[] properties, System.Func<string, bool> isHidden)
    {
        SerializedProperty property;

        EditorGUI.indentLevel++;
        foreach (string name in properties)
        {
            if (IsHidden(isHidden, name))
                continue;
            property = serializedObject.FindProperty(name);
            if (property != null)
                EditorGUILayout.PropertyField(property, true);
        }
        EditorGUI.indentLevel--;
    }

    private static void DrawSection(SerializedObject serializedObject, string prefsPrefix, string title, string[] properties, System.Func<string, bool> isHidden, System.Action<string> afterSection)
    {
        if (AllHidden(isHidden, properties))
            return;
        EditorGUILayout.Space(SectionSpacing);
        if (!Foldout(prefsPrefix + title, title, EditorStyles.foldoutHeader))
            return;
        DrawProperties(serializedObject, properties, isHidden);
        afterSection?.Invoke(title);
    }

    private static void DrawGroupTitle(string group)
    {
        EditorGUILayout.Space(GroupSpacing);
        EditorGUILayout.LabelField(group, EditorStyles.boldLabel);
    }

    // Properties not listed in any section still show up in a trailing "Other" section, so new fields are never hidden.
    public static void Draw(SerializedObject serializedObject, string prefsPrefix, IList<Section> sections, System.Func<string, bool> isHidden, System.Action<string> afterSection = null)
    {
        List<string> others = UnlistedProperties(serializedObject, sections);
        string currentGroup = null;

        foreach (Section section in sections)
        {
            if (!string.IsNullOrEmpty(section.Group) && section.Group != currentGroup)
            {
                currentGroup = section.Group;
                DrawGroupTitle(currentGroup);
            }
            DrawSection(serializedObject, prefsPrefix, section.Title, section.Properties, isHidden, afterSection);
        }
        if (others.Count > 0)
            DrawSection(serializedObject, prefsPrefix, OtherTitle, others.ToArray(), isHidden, afterSection);
    }

    // Foldout whose open state is kept in EditorPrefs under key, open by default.
    public static bool Foldout(string key, string label, GUIStyle style = null)
    {
        bool expanded = EditorPrefs.GetBool(key, true);
        bool newExpanded = EditorGUILayout.Foldout(expanded, label, true, style ?? EditorStyles.foldout);

        if (newExpanded != expanded)
            EditorPrefs.SetBool(key, newExpanded);
        return newExpanded;
    }
}
