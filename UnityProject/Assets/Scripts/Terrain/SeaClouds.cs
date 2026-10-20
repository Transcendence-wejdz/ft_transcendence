using UnityEngine;

// Cosmetic clouds drifting over the map, drawn above everything on the Sky sorting layer.
public class SeaClouds : MonoBehaviour
{
    [SerializeField] private Sprite[] cloudSprites;
    [SerializeField] private int cloudCount = 10;
    [Tooltip("Wind direction, normalized at runtime.")]
    [SerializeField] private Vector2 windDirection = new(1f, 0.15f);
    [SerializeField] private Vector2 speedRange = new(0.2f, 0.6f);
    [SerializeField] private Vector2 scaleRange = new(0.7f, 1.3f);
    [Range(0f, 1f)]
    [SerializeField] private float opacity = 0.9f;
    [Tooltip("World units added around the camera view; clouds wrap around this area, so keep it wider than a cloud.")]
    [SerializeField] private float margin = 12f;
    [SerializeField] private string sortingLayer = "Sky";
    [SerializeField] private int sortingOrder;
    [Tooltip("Leave empty for the default sprite material.")]
    [SerializeField] private Material cloudMaterial;

    private Transform[] clouds;
    private SpriteRenderer[] renderers;
    private float[] speeds;
    private Camera cam;

    void Start()
    {
        if (cloudSprites == null || cloudSprites.Length == 0)
            enabled = false;
    }

    // The camera lives on the spawned player, so clouds are created once one exists.
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
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
                return;
        }

        if (clouds == null)
            Spawn();

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
        }
    }

    // Camera view plus margin, so wrapping always happens off-screen.
    private Rect GetArea()
    {
        float halfHeight = cam.orthographicSize + margin;
        float halfWidth = cam.orthographicSize * cam.aspect + margin;
        Vector3 center = cam.transform.position;
        return new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
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
