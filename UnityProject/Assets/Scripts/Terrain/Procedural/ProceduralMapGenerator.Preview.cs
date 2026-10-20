#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class ProceduralMapGenerator
{
    public bool PreviewInEditor => previewInEditor;

    // Deferred because OnValidate is not allowed to create or destroy objects.
    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        EditorApplication.delayCall -= RefreshPreview;
        EditorApplication.delayCall += RefreshPreview;
    }

    // Scene-only: a preview inside a prefab stage or on the asset would get saved into the prefab.
    private bool CanPreview()
    {
        return this != null && !Application.isPlaying && gameObject.scene.IsValid()
            && !EditorUtility.IsPersistent(this) && PrefabStageUtility.GetPrefabStage(gameObject) == null
            && landTilemap != null && waterTilemap != null && waterBackgroundTilemap != null && coastFoamTilemap != null
            && biomes != null && biomes.Length > 0;
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
        if (!CanPreview())
            return;

        ClearPreview();
        Initialize();

        int radius = islandRadius > 0f
            ? Mathf.CeilToInt((islandRadius * (1f + islandEdgeVariation) + Mathf.Max(0f, decorCoastOffset)) / chunkSize) + 1
            : previewRadiusInChunks;
        Vector2Int center = WorldToChunk(transform.position);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
                GenerateChunk(center + new Vector2Int(x, y));
        }
    }

    public void ClearPreview()
    {
        foreach (Tilemap map in new[] { landTilemap, waterTilemap, waterBackgroundTilemap, coastFoamTilemap })
        {
            if (map == null || map.GetUsedTilesCount() == 0)
                continue;

            map.ClearAllTiles();
            RebuildColliders(map);
        }

        Transform root = transform.Find(DecorRootName);
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
        if (map.TryGetComponent(out TilemapCollider2D tilemapCollider))
            tilemapCollider.ProcessTilemapChanges();

        if (map.TryGetComponent(out CompositeCollider2D composite))
            composite.GenerateGeometry();
    }
}
#endif
