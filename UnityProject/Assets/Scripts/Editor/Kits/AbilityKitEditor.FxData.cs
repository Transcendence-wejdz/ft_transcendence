using UnityEditor;
using UnityEngine;

// What the preview reads from an FX prefab: its component, frame rate, zone phases, lifetime and hit window.
public partial class AbilityKitEditor
{
    private const float DefaultFrameRate = 24f;
    private const float MinFrameRate = 1f;

    private static readonly string[] FlipbookFields = { "frames", "frontFrames", "backFrames" };
    private static readonly string[] ZoneFields = { "startFrames", "loopFrames", "endFrames" };
    private static readonly string[] OverlayFields = { "overlayStartFrames", "overlayLoopFrames", "overlayEndFrames" };

    // ZoneAttackFX timing: start once, loop for its duration (damage ticks), end once.
    private class ZonePhases
    {
        public Sprite[] Start;
        public Sprite[] Loop;
        public Sprite[] End;
        public float FrameRate;
        public float Duration;

        public float StartLength => FlipbookLength(Start, FrameRate);
        public float LoopEnd => StartLength + Duration;
        public float Lifetime => LoopEnd + FlipbookLength(End, FrameRate);

        public Sprite Sample(float time)
        {
            if (time < StartLength)
                return PreviewAnimation.SampleSprites(Start, FrameRate, time, false);
            if (time < LoopEnd)
                return PreviewAnimation.SampleSprites(Loop, FrameRate, time - StartLength, true);
            return PreviewAnimation.SampleSprites(End, FrameRate, time - LoopEnd, false);
        }
    }

    private static MonoBehaviour FxComponent(GameObject fxPrefab)
    {
        if (fxPrefab == null)
            return null;
        foreach (MonoBehaviour behaviour in fxPrefab.GetComponents<MonoBehaviour>())
        {
            if (behaviour is IAttackFX)
                return behaviour;
        }
        return null;
    }

    private static float FlipbookLength(Sprite[] frames, float frameRate)
    {
        return frames.Length / Mathf.Max(frameRate, MinFrameRate);
    }

    // A sprite array field of the FX component, empty without one.
    private static Sprite[] FxSprites(GameObject fxPrefab, string field)
    {
        MonoBehaviour fx = FxComponent(fxPrefab);

        if (fx == null)
            return new Sprite[0];
        return PreviewAnimation.SpriteArray(new SerializedObject(fx), field);
    }

    private static float FrameRate(GameObject fxPrefab)
    {
        MonoBehaviour fx = FxComponent(fxPrefab);
        SerializedProperty rate;

        if (fx == null)
            return DefaultFrameRate;
        rate = new SerializedObject(fx).FindProperty("frameRate");
        if (rate == null)
            return DefaultFrameRate;
        return rate.floatValue;
    }

    // The zone's own frames, or with overlay its "overlay..." layer (same timing).
    private static ZonePhases ReadZone(GameObject fxPrefab, bool overlay = false)
    {
        ZoneAttackFX zone = null;
        SerializedObject serialized;
        string[] fields = ZoneFields;
        ZonePhases phases = new ZonePhases();

        if (fxPrefab != null)
            zone = fxPrefab.GetComponent<ZoneAttackFX>();
        if (zone == null)
            return null;
        serialized = new SerializedObject(zone);
        if (overlay)
            fields = OverlayFields;
        phases.Start = PreviewAnimation.SpriteArray(serialized, fields[0]);
        phases.Loop = PreviewAnimation.SpriteArray(serialized, fields[1]);
        phases.End = PreviewAnimation.SpriteArray(serialized, fields[2]);
        phases.FrameRate = serialized.FindProperty("frameRate").floatValue;
        phases.Duration = serialized.FindProperty("duration").floatValue;
        return phases;
    }

    private static float ProjectileLifetime(AbilityKit kit)
    {
        if (kit.Kind != AbilityKind.Projectile || kit.ProjectileSpeed <= 0f)
            return 0f;
        return kit.ProjectileMaxDistance / kit.ProjectileSpeed;
    }

    // Longest of the FX's flipbooks at its frame rate.
    private static float FlipbookLifetime(GameObject fxPrefab)
    {
        int frameCount = 0;
        float frameRate;

        foreach (string field in FlipbookFields)
            frameCount = Mathf.Max(frameCount, FxSprites(fxPrefab, field).Length);
        frameRate = FrameRate(fxPrefab);
        if (frameRate <= 0f)
            return 0f;
        return frameCount / frameRate;
    }

    // Projectiles live until their max distance, zones until their end phase, flipbook FX until their last frame.
    private static float FxLifetime(AbilityKit kit, GameObject fxPrefab)
    {
        ZonePhases zone;

        if (fxPrefab == null)
            return 0f;
        zone = ReadZone(fxPrefab);
        if (zone != null)
            return zone.Lifetime;
        if (fxPrefab.GetComponent<ArrowProjectile>() != null)
            return ProjectileLifetime(kit);
        return FlipbookLifetime(fxPrefab);
    }

    // When the hitbox deals damage: a zone only during its loop, any other FX for its whole lifetime.
    private static (float from, float to) HitWindow(AbilityKit kit, GameObject fxPrefab)
    {
        ZonePhases zone = ReadZone(fxPrefab);

        if (zone != null)
            return (zone.StartLength, zone.LoopEnd);
        return (0f, FxLifetime(kit, fxPrefab));
    }
}
