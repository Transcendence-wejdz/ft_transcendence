using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Sprite layers of the FX and their drawing: SlashAttackFX/ArrowProjectile frames on their renderer, LayeredAttackFX front/back pairs, ZoneAttackFX phases.
public partial class AbilityKitEditor
{
    private class FxLayer
    {
        public SpriteRenderer Renderer;
        public Sprite[] Frames;
        public bool Loop;
        public bool Behind;
        public ZonePhases Zone;
    }

    private static SpriteRenderer RendererField(SerializedObject serialized, string field)
    {
        SerializedProperty property = serialized.FindProperty(field);

        if (property == null)
            return null;
        return property.objectReferenceValue as SpriteRenderer;
    }

    // The FX's own spriteRenderer field, else the renderer on its root.
    private static SpriteRenderer MainRenderer(GameObject fxPrefab)
    {
        MonoBehaviour fx = FxComponent(fxPrefab);
        SpriteRenderer main = null;

        if (fx != null)
            main = RendererField(new SerializedObject(fx), "spriteRenderer");
        if (main == null && fxPrefab != null)
            main = fxPrefab.GetComponent<SpriteRenderer>();
        return main;
    }

    // Layers without a renderer are skipped.
    private static void AddLayer(List<FxLayer> layers, FxLayer layer)
    {
        if (layer.Renderer != null)
            layers.Add(layer);
    }

    private static void AddLayeredLayers(List<FxLayer> layers, SerializedObject serialized)
    {
        AddLayer(layers, new FxLayer { Renderer = RendererField(serialized, "backRenderer"), Frames = PreviewAnimation.SpriteArray(serialized, "backFrames"), Behind = true });
        AddLayer(layers, new FxLayer { Renderer = RendererField(serialized, "frontRenderer"), Frames = PreviewAnimation.SpriteArray(serialized, "frontFrames") });
    }

    private static void AddZoneLayers(List<FxLayer> layers, SerializedObject serialized, GameObject fxPrefab)
    {
        SpriteRenderer overlayRenderer = RendererField(serialized, "overlayRenderer");

        AddLayer(layers, new FxLayer { Renderer = MainRenderer(fxPrefab), Zone = ReadZone(fxPrefab), Behind = true });
        if (overlayRenderer != null)
            AddLayer(layers, new FxLayer { Renderer = overlayRenderer, Zone = ReadZone(fxPrefab, true) });
    }

    // The static base sprite under an animated child renderer, then the animated frames.
    private static void AddProjectileLayers(List<FxLayer> layers, SerializedObject serialized, GameObject fxPrefab)
    {
        SpriteRenderer baseRenderer = fxPrefab.GetComponent<SpriteRenderer>();
        SpriteRenderer animated = RendererField(serialized, "fxSpriteRenderer");

        if (animated != null && baseRenderer != null)
            AddLayer(layers, new FxLayer { Renderer = baseRenderer, Frames = new[] { baseRenderer.sprite }, Loop = true });
        if (animated == null)
            animated = baseRenderer;
        AddLayer(layers, new FxLayer { Renderer = animated, Frames = PreviewAnimation.SpriteArray(serialized, "frames"), Loop = true });
    }

    private static List<FxLayer> FxLayers(GameObject fxPrefab)
    {
        List<FxLayer> layers = new List<FxLayer>();
        MonoBehaviour fx = FxComponent(fxPrefab);
        SerializedObject serialized;

        if (fx == null)
            return layers;
        serialized = new SerializedObject(fx);
        if (fx is LayeredAttackFX)
            AddLayeredLayers(layers, serialized);
        else if (fx is ZoneAttackFX)
            AddZoneLayers(layers, serialized, fxPrefab);
        else if (fx is ArrowProjectile)
            AddProjectileLayers(layers, serialized, fxPrefab);
        else
            AddLayer(layers, new FxLayer { Renderer = fxPrefab.GetComponent<SpriteRenderer>(), Frames = PreviewAnimation.SpriteArray(serialized, "frames") });
        return layers;
    }

    private static Sprite LayerSprite(FxLayer layer, float frameRate, float time)
    {
        if (layer.Zone != null)
            return layer.Zone.Sample(time);
        if (layer.Frames != null && layer.Frames.Length > 0)
            return PreviewAnimation.SampleSprites(layer.Frames, frameRate, time, layer.Loop);
        return layer.Renderer.sprite;
    }

    // A renderer's sprite placed as it sits under the FX root at fxMatrix.
    private static void DrawLayerSprite(PreviewCanvas canvas, GameObject fxPrefab, Matrix4x4 fxMatrix, SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null || sprite == null)
            return;
        canvas.Sprite(sprite, fxMatrix * fxPrefab.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix, renderer.flipX, renderer.color);
    }

    private static void DrawFxLayers(PreviewCanvas canvas, List<FxLayer> layers, GameObject fxPrefab, Matrix4x4 fxMatrix, float time, bool behind)
    {
        float frameRate = FrameRate(fxPrefab);

        foreach (FxLayer layer in layers)
        {
            if (layer.Behind == behind)
                DrawLayerSprite(canvas, fxPrefab, fxMatrix, layer.Renderer, LayerSprite(layer, frameRate, time));
        }
    }
}
