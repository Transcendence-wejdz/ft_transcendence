using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Applies the Graphics settings (render scale, MSAA, HDR) to a runtime copy of the URP asset, so the project asset is never edited.
public static class GraphicsQualityApplier
{
    private static readonly float[] RenderScales = { 0.5f, 0.75f, 1f, 1f };
    private static readonly int[] MsaaSamples = { 1, 1, 1, 2 };

    private static RenderPipelineAsset originalAsset;
    private static UniversalRenderPipelineAsset runtimeAsset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameSettings.OnChanged -= Apply;
        GameSettings.OnChanged += Apply;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= RestoreOnExitPlay;
        UnityEditor.EditorApplication.playModeStateChanged += RestoreOnExitPlay;
#endif
        Apply();
    }

    public static void Apply()
    {
        if (runtimeAsset == null)
        {
            originalAsset = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
            if (originalAsset is not UniversalRenderPipelineAsset source)
                return;
            runtimeAsset = Object.Instantiate(source);
            runtimeAsset.name = source.name + " (Runtime)";
            QualitySettings.renderPipeline = runtimeAsset;
        }

        int quality = GameSettings.GraphicsQuality;
        runtimeAsset.renderScale = RenderScales[quality];
        runtimeAsset.msaaSampleCount = MsaaSamples[quality];
        runtimeAsset.supportsHDR = GameSettings.HdrEnabled;
    }

#if UNITY_EDITOR
    // Puts the project's own asset back on the quality level, so leaving Play mode never saves the runtime copy into QualitySettings.
    private static void RestoreOnExitPlay(UnityEditor.PlayModeStateChange change)
    {
        if (change != UnityEditor.PlayModeStateChange.ExitingPlayMode || runtimeAsset == null)
            return;
        QualitySettings.renderPipeline = originalAsset;
        Object.Destroy(runtimeAsset);
        runtimeAsset = null;
    }
#endif
}
