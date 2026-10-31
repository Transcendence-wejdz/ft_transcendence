using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Reads Animator states and sprite clips, so previews show the same frames the game plays.
public static class PreviewAnimation
{
    private const string SpriteProperty = "m_Sprite";
    private const float CenterSqrEpsilon = 0.0001f;
    private const float CenterScore = -2f;

    private static RuntimeAnimatorController Controller(GameObject prefab)
    {
        Animator animator = null;

        if (prefab != null)
            animator = prefab.GetComponentInChildren<Animator>(true);
        if (animator == null)
            return null;
        return animator.runtimeAnimatorController;
    }

    // How well a blend tree child points along direction; the center child scores lowest.
    private static float BlendScore(ChildMotion child, Vector2 direction)
    {
        if (child.position.sqrMagnitude < CenterSqrEpsilon)
            return CenterScore;
        return Vector2.Dot(child.position.normalized, direction.normalized);
    }

    private static AnimationClip ClosestBlendClip(BlendTree tree, Vector2 direction)
    {
        AnimationClip clip = null;
        AnimationClip candidate;
        float best = float.MinValue;
        float score;

        foreach (ChildMotion child in tree.children)
        {
            candidate = child.motion as AnimationClip;
            if (candidate == null)
                continue;
            score = BlendScore(child, direction);
            if (score > best)
            {
                best = score;
                clip = candidate;
            }
        }
        return clip;
    }

    // Base controller plus its override, so overridden clips are the ones read.
    public static AnimatorController ResolveController(RuntimeAnimatorController runtime, out AnimatorOverrideController overrides)
    {
        overrides = runtime as AnimatorOverrideController;
        if (overrides != null)
            return overrides.runtimeAnimatorController as AnimatorController;
        return runtime as AnimatorController;
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
        AnimationClip clip;

        if (state == null)
            return null;
        clip = state.motion as AnimationClip;
        if (clip == null && state.motion is BlendTree tree)
            clip = ClosestBlendClip(tree, direction);
        if (clip != null && overrides != null && overrides[clip] != null)
            clip = overrides[clip];
        return clip;
    }

    // Clip a controller plays for a state, override included.
    public static AnimationClip StateClip(RuntimeAnimatorController runtime, string stateName, Vector2 direction)
    {
        AnimatorController controller;
        AnimatorOverrideController overrides;

        if (runtime == null)
            return null;
        controller = ResolveController(runtime, out overrides);
        return ClipFor(FindState(controller, stateName), overrides, direction);
    }

    // Same, read from the first Animator under a prefab.
    public static AnimationClip StateClip(GameObject prefab, string stateName, Vector2 direction)
    {
        return StateClip(Controller(prefab), stateName, direction);
    }

    // A prefab's state, e.g. to read its playback speed.
    public static AnimatorState State(GameObject prefab, string stateName)
    {
        return FindState(ResolveController(Controller(prefab), out _), stateName);
    }

    public static ObjectReferenceKeyframe[] SpriteKeys(AnimationClip clip)
    {
        if (clip == null)
            return null;
        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            if (binding.type == typeof(SpriteRenderer) && binding.propertyName == SpriteProperty)
                return AnimationUtility.GetObjectReferenceCurve(clip, binding);
        }
        return null;
    }

    // Length of a sprite-key clip including its last frame's display time.
    public static float FramesLength(ObjectReferenceKeyframe[] frames)
    {
        float last;

        if (frames == null || frames.Length == 0)
            return 0f;
        last = frames[frames.Length - 1].time;
        if (frames.Length == 1)
            return last;
        return last + last / (frames.Length - 1);
    }

    public static Sprite SampleFrames(ObjectReferenceKeyframe[] frames, float t, bool loop)
    {
        float clipLength;
        Sprite sprite;

        if (frames == null || frames.Length == 0)
            return null;
        clipLength = frames[frames.Length - 1].time;
        if (clipLength > 0f && loop)
            t = Mathf.Repeat(t, FramesLength(frames));
        else if (clipLength > 0f)
            t = Mathf.Min(t, clipLength);
        sprite = frames[0].value as Sprite;
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
        if (duration <= 0f)
            return SampleFrames(frames, 0f, false);
        return SampleFrames(frames, t / duration * FramesLength(frames), false);
    }

    // Flipbook frame at time t for a sprite array played at frameRate; null once a non-looping one has finished.
    public static Sprite SampleSprites(Sprite[] frames, float frameRate, float t, bool loop)
    {
        int index;

        if (frames == null || frames.Length == 0 || frameRate <= 0f)
            return null;
        index = Mathf.FloorToInt(t * frameRate);
        if (loop)
            return frames[((index % frames.Length) + frames.Length) % frames.Length];
        if (index < 0 || index >= frames.Length)
            return null;
        return frames[index];
    }

    // Sprite array from a serialized field of a component (e.g. an FX's frames), empty when missing.
    public static Sprite[] SpriteArray(SerializedObject serialized, string field)
    {
        SerializedProperty property = serialized.FindProperty(field);
        Sprite[] sprites;

        if (property == null || !property.isArray)
            return new Sprite[0];
        sprites = new Sprite[property.arraySize];
        for (int i = 0; i < sprites.Length; i++)
            sprites[i] = property.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
        return sprites;
    }
}
