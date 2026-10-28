using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Reads Animator states and sprite clips, so previews show the same frames the game plays.
public static class PreviewAnimation
{
    // Base controller plus its override, so overridden clips are the ones read.
    public static AnimatorController ResolveController(RuntimeAnimatorController runtime, out AnimatorOverrideController overrides)
    {
        overrides = runtime as AnimatorOverrideController;
        return (overrides != null ? overrides.runtimeAnimatorController : runtime) as AnimatorController;
    }

    public static AnimatorState FindState(AnimatorController controller, string stateName)
    {
        if (controller == null || controller.layers.Length == 0)
            return null;
        foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
        {
            if (child.state.name == stateName)
                return child.state;
        }
        return null;
    }

    // A state's clip, or for a 2D blend tree the child whose position points closest to direction.
    public static AnimationClip ClipFor(AnimatorState state, AnimatorOverrideController overrides, Vector2 direction)
    {
        if (state == null)
            return null;

        AnimationClip clip = state.motion as AnimationClip;
        if (clip == null && state.motion is BlendTree tree)
        {
            float best = float.MinValue;
            foreach (ChildMotion child in tree.children)
            {
                AnimationClip candidate = child.motion as AnimationClip;
                if (candidate == null)
                    continue;
                float score = child.position.sqrMagnitude < 0.0001f ? -2f : Vector2.Dot(child.position.normalized, direction.normalized);
                if (score > best)
                {
                    best = score;
                    clip = candidate;
                }
            }
        }
        if (clip != null && overrides != null && overrides[clip] != null)
            clip = overrides[clip];
        return clip;
    }

    // Clip a controller plays for a state, override included.
    public static AnimationClip StateClip(RuntimeAnimatorController runtime, string stateName, Vector2 direction)
    {
        if (runtime == null)
            return null;
        AnimatorController controller = ResolveController(runtime, out AnimatorOverrideController overrides);
        return ClipFor(FindState(controller, stateName), overrides, direction);
    }

    // Same, read from the first Animator under a prefab.
    public static AnimationClip StateClip(GameObject prefab, string stateName, Vector2 direction) => StateClip(Controller(prefab), stateName, direction);

    // A prefab's state, e.g. to read its playback speed.
    public static AnimatorState State(GameObject prefab, string stateName) => FindState(ResolveController(Controller(prefab), out _), stateName);

    private static RuntimeAnimatorController Controller(GameObject prefab)
    {
        Animator animator = prefab != null ? prefab.GetComponentInChildren<Animator>(true) : null;
        return animator != null ? animator.runtimeAnimatorController : null;
    }

    public static ObjectReferenceKeyframe[] SpriteKeys(AnimationClip clip)
    {
        if (clip == null)
            return null;
        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite")
                return AnimationUtility.GetObjectReferenceCurve(clip, binding);
        }
        return null;
    }

    // Length of a sprite-key clip including its last frame's display time.
    public static float FramesLength(ObjectReferenceKeyframe[] frames)
    {
        if (frames == null || frames.Length == 0)
            return 0f;
        float last = frames[frames.Length - 1].time;
        float step = frames.Length > 1 ? last / (frames.Length - 1) : 0f;
        return last + step;
    }

    public static Sprite SampleFrames(ObjectReferenceKeyframe[] frames, float t, bool loop)
    {
        if (frames == null || frames.Length == 0)
            return null;

        float clipLength = frames[frames.Length - 1].time;
        if (clipLength > 0f)
            t = loop ? Mathf.Repeat(t, FramesLength(frames)) : Mathf.Min(t, clipLength);

        Sprite sprite = frames[0].value as Sprite;
        foreach (ObjectReferenceKeyframe frame in frames)
        {
            if (frame.time > t)
                break;
            sprite = frame.value as Sprite;
        }
        return sprite;
    }

    // Frame at time t when the clip is stretched to play exactly once over duration.
    public static Sprite SampleFitted(ObjectReferenceKeyframe[] frames, float t, float duration)
    {
        float clipLength = FramesLength(frames);
        return SampleFrames(frames, duration > 0f ? t / duration * clipLength : 0f, false);
    }

    // Flipbook frame at time t for a sprite array played at frameRate; null once a non-looping one has finished.
    public static Sprite SampleSprites(Sprite[] frames, float frameRate, float t, bool loop)
    {
        if (frames == null || frames.Length == 0 || frameRate <= 0f)
            return null;
        int index = Mathf.FloorToInt(t * frameRate);
        if (loop)
            return frames[((index % frames.Length) + frames.Length) % frames.Length];
        return index >= 0 && index < frames.Length ? frames[index] : null;
    }

    // Sprite array from a serialized field of a component (e.g. an FX's frames), empty when missing.
    public static Sprite[] SpriteArray(SerializedObject serialized, string field)
    {
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null || !property.isArray)
            return new Sprite[0];
        var sprites = new Sprite[property.arraySize];
        for (int i = 0; i < sprites.Length; i++)
            sprites[i] = property.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
        return sprites;
    }
}
