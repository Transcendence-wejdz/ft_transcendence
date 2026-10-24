using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Keeps editor previews out of saved scenes and out of Play mode.
[InitializeOnLoad]
static class ProceduralMapGeneratorPreviewHooks
{
    static ProceduralMapGeneratorPreviewHooks()
    {
        EditorSceneManager.sceneSaving += (scene, _) => ForEachPreview(scene, g => g.ClearPreview());
        EditorSceneManager.sceneSaved += scene => ForEachPreview(scene, g => g.GeneratePreview());
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode && state != PlayModeStateChange.EnteredEditMode)
            return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                ForEachPreview(SceneManager.GetSceneAt(i), g => g.ClearPreview());
            else
                ForEachPreview(SceneManager.GetSceneAt(i), g => g.GeneratePreview());
        }
    }

    private static void ForEachPreview(Scene scene, Action<ProceduralMapGenerator> action)
    {
        if (!scene.isLoaded)
            return;

        foreach (UnityEngine.GameObject root in scene.GetRootGameObjects())
        {
            foreach (ProceduralMapGenerator generator in root.GetComponentsInChildren<ProceduralMapGenerator>(true))
            {
                if (generator.PreviewInEditor)
                    action(generator);
            }
        }
    }
}
