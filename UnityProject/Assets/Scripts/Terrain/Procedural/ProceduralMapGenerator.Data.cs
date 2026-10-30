using UnityEngine;
using UnityEngine.Tilemaps;

// Serialized settings types of the generator: biomes, water, island, ground and decor entries.
public partial class ProceduralMapGenerator
{
    private enum SeedSource { Fixed, Random, Previous }

    private enum DecorSurface { Land, Water }

    [System.Serializable]
    private class Biome
    {
        public string name;
        [Tooltip("Ground tiles mixed inside this biome, picked by the ground noise in list order (first = lowest noise).")]
        public GroundEntry[] grounds;
        [Range(0f, 30f)]
        [Tooltip("Radius in cells around the player spawn kept as dry land with no decor.")]
        public float spawnSafeRadius = 5f;
        [Tooltip("Decor spawned on this biome. Earlier entries win when several match a cell.")]
        public DecorEntry[] decor;
        public WaterSettings water = new WaterSettings();
        public IslandSettings island = new IslandSettings();
    }

    [System.Serializable]
    private class WaterSettings
    {
        [PowerRange(0.005f, 0.5f, 2f)]
        [Tooltip("Perlin scale of the inland lakes. Smaller = fewer, larger lakes.")]
        public float noiseScale = 0.1f;
        [Range(0f, 1f)]
        [Tooltip("Cells whose water noise is below this become water. 0 = no inland lakes.")]
        public float threshold = 0.35f;
        public TileBase tile;
        public TileBase backgroundTile;
        public TileBase coastFoamTile;
    }

    [System.Serializable]
    private class IslandSettings
    {
        [Range(0f, 300f)]
        [Tooltip("Radius in cells around the generator beyond which everything is water. 0 = endless land.")]
        public float radius;
        [Range(0f, 0.5f)]
        [Tooltip("How far the coastline wanders from a perfect circle, as a fraction of the radius.")]
        public float edgeVariation = 0.15f;
        [PowerRange(0.005f, 0.5f, 2f)]
        [Tooltip("Perlin scale of the coastline wobble. Smaller = fewer, wider bays and capes.")]
        public float edgeNoiseScale = 0.05f;
        [Range(-10f, 10f)]
        [Tooltip("Decor only spawns up to this many cells past the coastline (negative keeps it inland). Ignored without an island.")]
        public float decorCoastOffset = 3f;
    }

    [System.Serializable]
    private class GroundEntry
    {
        [HideInInspector] public string label;
        public TileBase tile;
        [Range(0f, 1f)]
        [Tooltip("Share of the noise range this tile gets, relative to the others.")]
        public float weight = 1f;
    }

    [System.Serializable]
    private class DecorEntry
    {
        [HideInInspector] public string label;
        public GameObject prefab;
        [Tooltip("Land: only on dry tiles. Water: only on water tiles.")]
        public DecorSurface surface = DecorSurface.Land;
        [PowerRange(0f, 1f, 3f)]
        [Tooltip("Chance applied to cells inside the noise range - main density knob. The slider is finer near 0.")]
        public float density = 0.05f;
        [PowerRange(0.01f, 1f, 2f)]
        [Tooltip("Perlin noise scale for clustering. Smaller = larger, smoother clusters.")]
        public float noiseScale = 0.2f;
        [MinMaxRange(0f, 1f)]
        [Tooltip("Cells spawn only where the clustering noise falls in this range. Narrow = rarer patches; a middle band = rings around the clusters.")]
        public Vector2 noiseRange = new Vector2(0.75f, 1f);
    }
}
