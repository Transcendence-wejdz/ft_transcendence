using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Frame cap, VSync and the bloom volume, applied alongside the URP quality settings.
public static partial class GraphicsQualityApplier
{
    private const string BloomProfilePath = "Rendering/BloomProfile";
    private const float BloomVolumePriority = 100f;

    private static Volume bloomVolume;
    private static int originalVSyncCount = -1;

    // VSync only on desktop; while it is on, desktop players ignore the frame cap.
    private static void ApplyDisplay()
    {
        Application.targetFrameRate = GameSettings.FpsLimitValues[GameSettings.FpsLimit];
        if (!GameSettings.IsDesktop)
            return;
        if (originalVSyncCount < 0)
            originalVSyncCount = QualitySettings.vSyncCount;
        if (GameSettings.VSync)
            QualitySettings.vSyncCount = 1;
        else
            QualitySettings.vSyncCount = 0;
    }

    // Editor only: puts the quality level's VSync back so Play mode never saves it into QualitySettings.
    private static void RestoreDisplay()
    {
        if (originalVSyncCount < 0)
            return;
        QualitySettings.vSyncCount = originalVSyncCount;
        originalVSyncCount = -1;
    }

    // One global volume for the whole session, created on first use.
    private static Volume BloomVolume()
    {
        VolumeProfile profile;
        GameObject go;

        if (bloomVolume != null)
            return bloomVolume;
        profile = Resources.Load<VolumeProfile>(BloomProfilePath);
        if (profile == null)
            return null;
        go = new GameObject("BloomVolume");
        Object.DontDestroyOnLoad(go);
        bloomVolume = go.AddComponent<Volume>();
        bloomVolume.isGlobal = true;
        bloomVolume.priority = BloomVolumePriority;
        bloomVolume.sharedProfile = profile;
        return bloomVolume;
    }

    private static void ApplyBloom()
    {
        Volume volume = BloomVolume();

        if (volume != null)
            volume.enabled = GameSettings.Bloom;
    }

    // Post-processing runs on game cameras only while bloom is on, so it costs nothing when off; render-texture previews keep their own setup.
    private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        UniversalAdditionalCameraData data;

        if (camera.cameraType != CameraType.Game || camera.targetTexture != null)
            return;
        if (!camera.TryGetComponent(out data))
            return;
        data.renderPostProcessing = GameSettings.Bloom;
    }
}
