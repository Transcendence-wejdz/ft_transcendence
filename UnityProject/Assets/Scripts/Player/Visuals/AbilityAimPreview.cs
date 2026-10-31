using System.Collections.Generic;
using UnityEngine;

// Ability hitbox where it would land, drawn while the player holds the aim as a faint filled zone plus its outline; shape read from the FX prefab's Collider2D, tinted to the player's color.
[RequireComponent(typeof(LineRenderer))]
public partial class AbilityAimPreview : MonoBehaviour
{
    [Range(8, 128)]
    [SerializeField] private int arcSegments = 48;
    [Range(0.01f, 0.3f)]
    [SerializeField] private float lineWidth = 0.06f;
    [Range(0f, 1f)]
    [SerializeField] private float alpha = 0.85f;
    [SerializeField] private MeshFilter fill;
    [Range(0f, 1f)]
    [SerializeField] private float fillAlpha = 0.12f;

    private LineRenderer line;
    private MeshRenderer fillRenderer;
    private Mesh fillMesh;
    private Vector3[] fillVertices = new Vector3[0];
    private Color[] fillColors = new Color[0];
    private Color fillColor;
    private Vector3 shapeCenter;
    private Player player;
    private GameObject shapeSource;
    private readonly List<Vector3> shape = new List<Vector3>();

    private void InitLine()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = lineWidth;
        line.enabled = false;
    }

    private void InitFill()
    {
        if (fill == null)
            return;
        fillMesh = new Mesh { name = "AbilityAimPreviewFill" };
        fillMesh.MarkDynamic();
        fill.sharedMesh = fillMesh;
        fillRenderer = fill.GetComponent<MeshRenderer>();
        fillRenderer.enabled = false;
    }

    private void ApplyColor(int classIndex, int colorIndex)
    {
        Color color = Color.white;

        if (CharacterCatalog.Instance != null)
            color = CharacterCatalog.Instance.GetFxLightColor(colorIndex);
        color.a = alpha;
        line.startColor = color;
        line.endColor = color;
        fillColor = color;
        fillColor.a = fillAlpha;
    }

    private void HideFill()
    {
        if (fillRenderer != null)
            fillRenderer.enabled = false;
    }

    // Fan of triangles from the shape's center, rewritten in the fill object's local space each frame since the player moves under it.
    private void ShowFill(Matrix4x4 pose)
    {
        Transform space;

        if (fillMesh == null)
            return;
        space = fill.transform;
        fillVertices[0] = space.InverseTransformPoint(pose.MultiplyPoint3x4(shapeCenter));
        for (int i = 0; i < shape.Count; i++)
            fillVertices[i + 1] = space.InverseTransformPoint(pose.MultiplyPoint3x4(shape[i]));
        for (int i = 0; i < fillColors.Length; i++)
            fillColors[i] = fillColor;
        fillMesh.vertices = fillVertices;
        fillMesh.colors = fillColors;
        fillMesh.RecalculateBounds();
        fillRenderer.enabled = true;
    }

    // Same pose the FX gets when spawned: position, rotation, and the prefab's own scale times the ability's size multiplier.
    public void Show(GameObject fxPrefab, Vector3 position, Quaternion rotation, float scaleMultiplier)
    {
        Matrix4x4 pose;

        if (fxPrefab != shapeSource)
        {
            shapeSource = fxPrefab;
            BuildShape(fxPrefab);
        }
        if (shape.Count < 2)
        {
            Hide();
            return;
        }
        pose = Matrix4x4.TRS(position, rotation, fxPrefab.transform.localScale * scaleMultiplier);
        line.loop = true;
        line.positionCount = shape.Count;
        for (int i = 0; i < shape.Count; i++)
            line.SetPosition(i, pose.MultiplyPoint3x4(shape[i]));
        line.enabled = true;
        ShowFill(pose);
    }

    // Straight path of a projectile, from its spawn point to where it despawns; no fill.
    public void ShowLine(Vector3 from, Vector3 to)
    {
        line.loop = false;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.enabled = true;
        HideFill();
    }

    public void Hide()
    {
        if (line != null)
            line.enabled = false;
        HideFill();
    }

    void Awake()
    {
        InitLine();
        player = GetComponentInParent<Player>();
        InitFill();
    }

    void OnEnable()
    {
        if (player == null)
            return;
        player.OnClassOrColorChanged += ApplyColor;
        ApplyColor(player.ClassIndex, player.ColorIndex);
    }

    void OnDisable()
    {
        if (player != null)
            player.OnClassOrColorChanged -= ApplyColor;
        Hide();
    }
}
