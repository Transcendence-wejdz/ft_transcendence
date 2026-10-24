using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Foldout sections for custom inspectors (open state kept in EditorPrefs), with optional group titles and a hook to draw extras at the end of a section.
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

    // Properties not listed in any section still show up in a trailing "Other" section, so new fields are never hidden.
    public static void Draw(SerializedObject serializedObject, string prefsPrefix, IList<Section> sections, System.Func<string, bool> isHidden, System.Action<string> afterSection = null)
    {
        var listed = new HashSet<string> { "m_Script" };
        foreach (Section section in sections)
            listed.UnionWith(section.Properties);

        var others = new List<string>();
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (!listed.Contains(iterator.name))
                others.Add(iterator.name);
        }

        string currentGroup = null;
        foreach (Section section in sections)
        {
            if (!string.IsNullOrEmpty(section.Group) && section.Group != currentGroup)
            {
                currentGroup = section.Group;
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField(section.Group, EditorStyles.boldLabel);
            }
            DrawSection(serializedObject, prefsPrefix, section.Title, section.Properties, isHidden, afterSection);
        }

        if (others.Count > 0)
            DrawSection(serializedObject, prefsPrefix, "Other", others.ToArray(), isHidden, afterSection);
    }

    private static void DrawSection(SerializedObject serializedObject, string prefsPrefix, string title, string[] properties, System.Func<string, bool> isHidden, System.Action<string> afterSection)
    {
        string key = prefsPrefix + title;
        bool expanded = EditorPrefs.GetBool(key, true);
        EditorGUILayout.Space(2f);
        bool newExpanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
        if (newExpanded != expanded)
            EditorPrefs.SetBool(key, newExpanded);
        if (!newExpanded)
            return;

        EditorGUI.indentLevel++;
        foreach (string name in properties)
        {
            if (isHidden != null && isHidden(name))
                continue;
            SerializedProperty property = serializedObject.FindProperty(name);
            if (property != null)
                EditorGUILayout.PropertyField(property, true);
        }
        EditorGUI.indentLevel--;
        afterSection?.Invoke(title);
    }
}
