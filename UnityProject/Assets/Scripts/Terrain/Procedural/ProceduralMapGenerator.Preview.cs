#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Editor-only Scene view preview of the map, refreshed on every inspector change.
public partial class ProceduralMapGenerator
{
    private const string EmptyEntryLabel = "(empty)";

    public bool PreviewInEditor => previewInEditor;

    private static void LabelGrounds(GroundEntry[] grounds)
    {
        if (grounds == null)
            return;
        foreach (GroundEntry ground in grounds)
        {
            if (ground == null)
                continue;
            if (ground.tile != null)
                ground.label = ground.tile.name;
            else
                ground.label = EmptyEntryLabel;
        }
    }

    private static void LabelDecor(DecorEntry[] decor)
    {
        if (decor == null)
            return;
        foreach (DecorEntry entry in decor)
        {
            if (entry == null)
                continue;
            if (entry.prefab != null)
                entry.label = $"{entry.prefab.name} ({entry.surface})";
            else
                entry.label = EmptyEntryLabel;
        }
    }

    // Array elements show their tile or prefab name instead of "Element N".
    private void RefreshEntryLabels()
    {
        if (biomes == null)
            return;
        foreach (Biome biome in biomes)
        {
            if (biome == null)
                continue;
            LabelGrounds(biome.grounds);
            LabelDecor(biome.decor);
        }
    }

    // Scene-only: a preview inside a prefab stage or on the asset would get saved into the prefab.
    private bool CanPreview()
    {
        if (this == null || Application.isPlaying || !gameObject.scene.IsValid())
            return false;
        if (EditorUtility.IsPersistent(this) || PrefabStageUtility.GetPrefabStage(gameObject) != null)
            return false;
        if (landTilemap == null || waterTilemap == null || waterBackgroundTilemap == null || coastFoamTilemap == null)
            return false;
        return biomes != null && biomes.Length > 0;
    }

    private int PreviewRadius()
    {
        if (Island.radius > 0f)
            return IslandChunkRadius();
        return previewRadiusInChunks;
    }

    private void RefreshPreview()
    {
        if (!CanPreview())
            return;
        if (previewInEditor)
            GeneratePreview();
        else
            ClearPreview();
    }

    // Covers the whole island plus a ring of ocean, or a fixed square when the map is endless.
    public void GeneratePreview()
    {
        int radius;
        Vector2Int center;

        if (!CanPreview())
            return;
        ClearPreview();
        Initialize();
        radius = PreviewRadius();
        center = WorldToChunk(transform.position);
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
                GenerateChunk(center + new Vector2Int(x, y));
        }
    }

    private static void ClearTilemap(Tilemap map)
    {
        if (map == null || map.GetUsedTilesCount() == 0)
            return;
        map.ClearAllTiles();
        RebuildColliders(map);
    }

    public void ClearPreview()
    {
        Transform root;

        foreach (Tilemap map in new Tilemap[] { landTilemap, waterTilemap, waterBackgroundTilemap, coastFoamTilemap })
            ClearTilemap(map);
        root = transform.Find(DecorRootName);
        if (root != null)
            DestroyImmediate(root.gameObject);
        decorParent = null;
        decorObjects.Clear();
        decorPool.Clear();
        decorCategoryParents.Clear();
    }

    // Colliders rebuild lazily, so without this the preview's geometry gets saved into the scene.
    private static void RebuildColliders(Tilemap map)
    {
        TilemapCollider2D tilemapCollider;
        CompositeCollider2D composite;

        if (map.TryGetComponent(out tilemapCollider))
            tilemapCollider.ProcessTilemapChanges();
        if (map.TryGetComponent(out composite))
            composite.GenerateGeometry();
    }

    // Deferred because OnValidate is not allowed to create or destroy objects.
    void OnValidate()
    {
        RefreshEntryLabels();
        if (Application.isPlaying)
            return;
        EditorApplication.delayCall -= RefreshPreview;
        EditorApplication.delayCall += RefreshPreview;
    }
}
#endif
