using System.Collections.Generic;
using UnityEngine;

// Ability hitbox where it would land, drawn while the player holds the aim as a faint filled zone plus its outline; shape read from the FX prefab's Collider2D, tinted to the player's color.
[RequireComponent(typeof(LineRenderer))]
public class AbilityAimPreview : MonoBehaviour
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

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = lineWidth;
        line.enabled = false;
        player = GetComponentInParent<Player>();

        if (fill != null)
        {
            fillMesh = new Mesh { name = "AbilityAimPreviewFill" };
            fillMesh.MarkDynamic();
            fill.sharedMesh = fillMesh;
            fillRenderer = fill.GetComponent<MeshRenderer>();
            fillRenderer.enabled = false;
        }
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

    private void ApplyColor(int classIndex, int colorIndex)
    {
        Color color = CharacterCatalog.Instance != null ? CharacterCatalog.Instance.GetFxLightColor(colorIndex) : Color.white;
        color.a = alpha;
        line.startColor = color;
        line.endColor = color;
        fillColor = color;
        fillColor.a = fillAlpha;
    }

    // Same pose the FX gets when spawned: position, rotation, and the prefab's own scale times the ability's size multiplier.
    public void Show(GameObject fxPrefab, Vector3 position, Quaternion rotation, float scaleMultiplier)
    {
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

        Matrix4x4 pose = Matrix4x4.TRS(position, rotation, fxPrefab.transform.localScale * scaleMultiplier);
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
        if (fillRenderer != null)
            fillRenderer.enabled = false;
    }

    public void Hide()
    {
        if (line != null)
            line.enabled = false;
        if (fillRenderer != null)
            fillRenderer.enabled = false;
    }

    // Fan of triangles from the shape's center, rewritten in the fill object's local space each frame since the player moves under it.
    private void ShowFill(Matrix4x4 pose)
    {
        if (fillMesh == null)
            return;

        Transform space = fill.transform;
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

    // Vertex and triangle layout for the current shape; positions are filled per frame by ShowFill.
    private void RebuildFillMesh()
    {
        if (fillMesh == null)
            return;

        shapeCenter = Vector3.zero;
        foreach (Vector3 point in shape)
            shapeCenter += point;
        if (shape.Count > 0)
            shapeCenter /= shape.Count;

        fillMesh.Clear();
        fillVertices = new Vector3[shape.Count + 1];
        fillColors = new Color[shape.Count + 1];
        int[] triangles = new int[shape.Count * 3];
        for (int i = 0; i < shape.Count; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % shape.Count + 1;
        }
        fillMesh.vertices = fillVertices;
        fillMesh.triangles = triangles;
    }

    // Hitbox outline in the FX root's local space, from its first Collider2D (circle, capsule, box or polygon).
    private void BuildShape(GameObject fx)
    {
        shape.Clear();
        Collider2D hitbox = fx != null ? fx.GetComponentInChildren<Collider2D>(true) : null;
        if (hitbox == null)
            return;

        switch (hitbox)
        {
            case CircleCollider2D circle:
                AddArc(circle.offset, circle.radius, 0f, 360f, closed: true);
                break;
            case CapsuleCollider2D capsule:
                AddCapsule(capsule);
                break;
            case BoxCollider2D box:
                Vector2 half = box.size * 0.5f;
                shape.Add(box.offset + new Vector2(-half.x, -half.y));
                shape.Add(box.offset + new Vector2(half.x, -half.y));
                shape.Add(box.offset + new Vector2(half.x, half.y));
                shape.Add(box.offset + new Vector2(-half.x, half.y));
                break;
            case PolygonCollider2D polygon:
                foreach (Vector2 point in polygon.GetPath(0))
                    shape.Add(polygon.offset + point);
                break;
        }

        Matrix4x4 toRoot = fx.transform.worldToLocalMatrix * hitbox.transform.localToWorldMatrix;
        for (int i = 0; i < shape.Count; i++)
            shape[i] = toRoot.MultiplyPoint3x4(shape[i]);
        RebuildFillMesh();
    }

    // Two half circles joined by straight sides, along the capsule's long axis.
    private void AddCapsule(CapsuleCollider2D capsule)
    {
        bool vertical = capsule.direction == CapsuleDirection2D.Vertical;
        float radius = (vertical ? capsule.size.x : capsule.size.y) * 0.5f;
        float straight = Mathf.Max(0f, (vertical ? capsule.size.y : capsule.size.x) * 0.5f - radius);
        Vector2 axis = vertical ? Vector2.up : Vector2.right;
        float start = vertical ? 0f : -90f;

        AddArc(capsule.offset + axis * straight, radius, start, start + 180f, closed: false);
        AddArc(capsule.offset - axis * straight, radius, start + 180f, start + 360f, closed: false);
    }

    private void AddArc(Vector2 center, float radius, float fromDeg, float toDeg, bool closed)
    {
        int steps = Mathf.Max(2, Mathf.RoundToInt(arcSegments * (toDeg - fromDeg) / 360f));
        int count = closed ? steps : steps + 1;
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Lerp(fromDeg, toDeg, (float)i / steps) * Mathf.Deg2Rad;
            shape.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }
}
