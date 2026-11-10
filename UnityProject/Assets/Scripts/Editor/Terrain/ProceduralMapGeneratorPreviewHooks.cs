using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps editor previews out of saved scenes and out of Play mode.
[InitializeOnLoad]
static class ProceduralMapGeneratorPreviewHooks
{
    private static void SetPreviews(Scene scene, bool generate)
    {
        if (!scene.isLoaded)
            return;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (ProceduralMapGenerator generator in root.GetComponentsInChildren<ProceduralMapGenerator>(true))
            {
                if (!generator.PreviewInEditor)
                    continue;
                if (generate)
                    generator.GeneratePreview();
                else
                    generator.ClearPreview();
            }
        }
    }

    private static void HandleSceneSaving(Scene scene, string path)
    {
        SetPreviews(scene, false);
    }

    private static void HandleSceneSaved(Scene scene)
    {
        SetPreviews(scene, true);
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode && state != PlayModeStateChange.EnteredEditMode)
            return;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            SetPreviews(SceneManager.GetSceneAt(i), state == PlayModeStateChange.EnteredEditMode);
    }

    static ProceduralMapGeneratorPreviewHooks()
    {
        EditorSceneManager.sceneSaving += HandleSceneSaving;
        EditorSceneManager.sceneSaved += HandleSceneSaved;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }
}
