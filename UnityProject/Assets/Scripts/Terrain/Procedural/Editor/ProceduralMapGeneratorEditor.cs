using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralMapGenerator))]
public class ProceduralMapGeneratorEditor : Editor
{
    private struct Section
    {
        public string Key;
        public string Label;
        public string[] Properties;

        public Section(string key, string label, params string[] properties)
        {
            Key = key;
            Label = label;
            Properties = properties;
        }
    }

    private static readonly Section[] Sections =
    {
        new("References", "References",
            "landTilemap", "waterTilemap", "waterBackgroundTilemap", "coastFoamTilemap", "player"),
        new("Generation", "Generation", "seed", "seedSource"),
        new("ChunkStreaming", "Chunk Streaming",
            "chunkSize", "viewDistanceInChunks", "maxChunkGenerationsPerFrame", "loadWholeIsland"),
        new("Biomes", "Biomes",
            "groundNoiseScale", "groundNoiseOctaves", "groundNoisePersistence", "groundNoiseContrast", "biomes"),
        new("EditorPreview", "Editor Preview", "previewInEditor", "previewRadiusInChunks"),
    };

    private const string FoldoutPrefPrefix = "ProceduralMapGeneratorEditor.Foldout.";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        foreach (Section section in Sections)
        {
            string prefKey = FoldoutPrefPrefix + section.Key;
            bool expanded = EditorPrefs.GetBool(prefKey, true);

            EditorGUILayout.Space(2);
            bool newExpanded = EditorGUILayout.Foldout(expanded, section.Label, true, EditorStyles.foldoutHeader);
            if (newExpanded != expanded)
                EditorPrefs.SetBool(prefKey, newExpanded);

            if (newExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (string propertyName in section.Properties)
                {
                    SerializedProperty property = serializedObject.FindProperty(propertyName);
                    if (property != null)
                        EditorGUILayout.PropertyField(property, true);
                }
                EditorGUI.indentLevel--;
            }
        }

        serializedObject.ApplyModifiedProperties();

        if (serializedObject.FindProperty("previewInEditor").boolValue && GUILayout.Button("Regenerate Preview"))
            ((ProceduralMapGenerator)target).GeneratePreview();
    }
}
