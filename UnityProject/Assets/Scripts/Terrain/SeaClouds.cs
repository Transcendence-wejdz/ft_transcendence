using UnityEngine;

// Cosmetic clouds living in a fixed world area over the map, drifting with the wind and drawn above everything on the Sky sorting layer.
public class SeaClouds : MonoBehaviour
{
    [SerializeField] private Sprite[] cloudSprites;
    [Range(0, 300)]
    [SerializeField] private int cloudCount = 10;
    [Tooltip("Wind direction, normalized at runtime.")]
    [SerializeField] private Vector2 windDirection = new(1f, 0.15f);
    [SerializeField] private Vector2 speedRange = new(0.2f, 0.6f);
    [SerializeField] private Vector2 scaleRange = new(0.7f, 1.3f);
    [Range(0f, 1f)]
    [SerializeField] private float opacity = 0.9f;
    [Tooltip("World-space size of the cloud area, centered on this object. Clouds wrap inside it, so keep it a bit wider than the map.")]
    [SerializeField] private Vector2 areaSize = new(60f, 60f);
    [Range(0f, 30f)]
    [Tooltip("Clouds fade out over this distance before reaching the area edge, so the wrap to the other side is invisible.")]
    [SerializeField] private float edgeFade = 8f;
    [SerializeField] private string sortingLayer = "Sky";
    [SerializeField] private int sortingOrder;
    [Tooltip("Leave empty for the default sprite material.")]
    [SerializeField] private Material cloudMaterial;

    private Transform[] clouds;
    private SpriteRenderer[] renderers;
    private float[] speeds;

    void Start()
    {
        if (cloudSprites == null || cloudSprites.Length == 0)
        {
            enabled = false;
            return;
        }
        Spawn();
    }

    private void Spawn()
    {
        clouds = new Transform[cloudCount];
        renderers = new SpriteRenderer[cloudCount];
        speeds = new float[cloudCount];
        Rect area = GetArea();

        for (int i = 0; i < cloudCount; i++)
        {
            GameObject cloud = new("Cloud");
            cloud.transform.SetParent(transform, false);
            clouds[i] = cloud.transform;
            renderers[i] = cloud.AddComponent<SpriteRenderer>();
            renderers[i].sortingLayerName = sortingLayer;
            renderers[i].sortingOrder = sortingOrder;
            if (cloudMaterial != null)
                renderers[i].sharedMaterial = cloudMaterial;
            Restyle(i);
            clouds[i].position = new Vector3(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax), 0f);
        }
    }

    void Update()
    {
        Rect area = GetArea();
        Vector3 wind = windDirection.normalized;

        for (int i = 0; i < cloudCount; i++)
        {
            Vector3 pos = clouds[i].position + wind * (speeds[i] * Time.deltaTime);

            if (!area.Contains(pos))
            {
                pos.x = area.xMin + Mathf.Repeat(pos.x - area.xMin, area.width);
                pos.y = area.yMin + Mathf.Repeat(pos.y - area.yMin, area.height);
                Restyle(i);
            }

            clouds[i].position = new Vector3(pos.x, pos.y, 0f);

            float edgeDistance = Mathf.Min(pos.x - area.xMin, area.xMax - pos.x, pos.y - area.yMin, area.yMax - pos.y);
            float alpha = opacity * (edgeFade > 0f ? Mathf.Clamp01(edgeDistance / edgeFade) : 1f);
            renderers[i].color = new Color(1f, 1f, 1f, alpha);
        }
    }

    private Rect GetArea()
    {
        Vector3 center = transform.position;
        return new Rect(center.x - areaSize.x * 0.5f, center.y - areaSize.y * 0.5f, areaSize.x, areaSize.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0f));
    }

    // New look each time a cloud re-enters, so the wrap is not noticeable.
    private void Restyle(int i)
    {
        renderers[i].sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
        renderers[i].color = new Color(1f, 1f, 1f, opacity);
        renderers[i].flipX = Random.value < 0.5f;
        clouds[i].localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
        speeds[i] = Random.Range(speedRange.x, speedRange.y);
    }
}
