using UnityEditor;
using UnityEngine;

// Foldout sections for the map generator, plus a Regenerate button while the editor preview is on.
[CustomEditor(typeof(ProceduralMapGenerator))]
public class ProceduralMapGeneratorEditor : Editor
{
    private const string PrefsPrefix = "ProceduralMapGeneratorEditor.Section.";

    private static readonly InspectorSections.Section[] Sections =
    {
        new("", "References", "landTilemap", "waterTilemap", "waterBackgroundTilemap", "coastFoamTilemap", "player"),
        new("", "Generation", "seed", "seedSource"),
        new("", "Chunk Streaming", "chunkSize", "viewDistanceInChunks", "maxChunkGenerationsPerFrame", "loadWholeIsland"),
        new("", "Biomes", "groundNoiseScale", "groundNoiseOctaves", "groundNoisePersistence", "groundNoiseContrast", "biomes"),
        new("", "Editor Preview", "previewInEditor", "previewRadiusInChunks"),
    };

    public override void OnInspectorGUI()
    {
        bool previewOn;

        serializedObject.Update();
        InspectorSections.Draw(serializedObject, PrefsPrefix, Sections, null);
        serializedObject.ApplyModifiedProperties();
        previewOn = serializedObject.FindProperty("previewInEditor").boolValue;
        if (previewOn && GUILayout.Button("Regenerate Preview"))
            ((ProceduralMapGenerator)target).GeneratePreview();
    }
}
