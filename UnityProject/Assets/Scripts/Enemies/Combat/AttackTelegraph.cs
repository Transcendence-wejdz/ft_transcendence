using System.Collections;
using UnityEngine;

// Ground warning shown during an attack windup: outlines the exact hit zone and sweeps a fill out to its edge, reaching it when the hit lands.
public class AttackTelegraph : MonoBehaviour
{
    private const int ArcSegments = 32;
    private const int CircleSegments = 64;
    private const string GroundSortingLayer = "Ground";
    private const float FadeInSeconds = 0.08f;
    private const float FlashSeconds = 0.15f;
    private const float FullCircleDegrees = 359f;
    private const float MinDirectionSqr = 0.0001f;
    private const float ConeShape = 0f;
    private const float LaneShape = 1f;
    private const float CircleShape = 2f;

    private static readonly int ShapeId = Shader.PropertyToID("_Shape");
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int FlashId = Shader.PropertyToID("_Flash");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

    private static Material cachedMaterial;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock block;

    // Parented to the attacker (if given) so it tracks any drift during the windup, instead of staying frozen in world space.
    public static void Show(Transform anchor, Vector3 position, Vector2 direction, float coneAngleDegrees, float radius, float duration)
    {
        float halfAngle = coneAngleDegrees * 0.5f * Mathf.Deg2Rad;

        if (radius <= 0f || duration <= 0f || direction.sqrMagnitude < MinDirectionSqr)
            return;
        if (coneAngleDegrees >= FullCircleDegrees)
        {
            Spawn(anchor, position, Vector2.right, BuildFanMesh(radius, 360f, CircleSegments), new Vector4(CircleShape, radius, 0f, 0f), duration);
            return;
        }
        Spawn(anchor, position, direction, BuildFanMesh(radius, coneAngleDegrees, ArcSegments), new Vector4(ConeShape, radius, halfAngle, 0f), duration);
    }

    // Straight lane from position along direction, e.g. a boss charge path.
    public static void ShowLane(Transform anchor, Vector3 position, Vector2 direction, float length, float width, float duration)
    {
        if (length <= 0f || width <= 0f || duration <= 0f || direction.sqrMagnitude < MinDirectionSqr)
            return;
        Spawn(anchor, position, direction, BuildLaneMesh(length, width), new Vector4(LaneShape, length, width * 0.5f, 0f), duration);
    }

    // The mesh is built pointing along +x and the object is rotated, so the shader reads the shape straight from local space.
    private static void Spawn(Transform anchor, Vector3 position, Vector2 direction, Mesh mesh, Vector4 shape, float duration)
    {
        Material material = GetMaterial();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        GameObject go;
        AttackTelegraph telegraph;

        if (material == null)
            return;
        go = new GameObject("AttackTelegraph", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
        if (anchor != null)
            go.transform.SetParent(anchor, worldPositionStays: true);
        telegraph = go.AddComponent<AttackTelegraph>();
        telegraph.Init(mesh, material, shape, duration);
    }

    private void Init(Mesh mesh, Material material, Vector4 shape, float duration)
    {
        GetComponent<MeshFilter>().mesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.sortingLayerName = GroundSortingLayer;
        block = new MaterialPropertyBlock();
        block.SetVector(ShapeId, shape);
        Apply(0f, 0f, 0f);
        StartCoroutine(Animate(duration));
    }

    // Fill sweeps over the windup, then a short flash marks the moment the hit lands.
    private IEnumerator Animate(float duration)
    {
        yield return Sweep(duration);
        yield return Flash();
        Destroy(GetComponent<MeshFilter>().mesh);
        Destroy(gameObject);
    }

    private IEnumerator Sweep(float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            Apply(Mathf.Clamp01(t / duration), 0f, Mathf.Clamp01(t / FadeInSeconds));
            yield return null;
        }
    }

    private IEnumerator Flash()
    {
        float flash = 0f;
        float k;

        while (flash < FlashSeconds)
        {
            flash += Time.deltaTime;
            k = Mathf.Clamp01(flash / FlashSeconds);
            Apply(1f, 1f - k * 0.5f, 1f - k);
            yield return null;
        }
    }

    private void Apply(float progress, float flash, float alpha)
    {
        block.SetFloat(ProgressId, progress);
        block.SetFloat(FlashId, flash);
        block.SetFloat(AlphaId, alpha);
        meshRenderer.SetPropertyBlock(block);
    }

    // Fan from the origin out to an arc of the given angle, centered on +x.
    private static Mesh BuildFanMesh(float radius, float angleDegrees, int segments)
    {
        float halfAngle = angleDegrees * 0.5f * Mathf.Deg2Rad;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];
        Mesh mesh = new Mesh { name = "AttackTelegraphFan" };
        float angle;

        vertices[0] = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            angle = -halfAngle + (float)i / segments * 2f * halfAngle;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        }
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh BuildLaneMesh(float length, float width)
    {
        float halfWidth = width * 0.5f;
        Mesh mesh = new Mesh { name = "AttackTelegraphLane" };

        mesh.SetVertices(new[] { new Vector3(0f, -halfWidth, 0f), new Vector3(0f, halfWidth, 0f), new Vector3(length, halfWidth, 0f), new Vector3(length, -halfWidth, 0f) });
        mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // Lives in Resources so the shader ships in builds.
    private static Material GetMaterial()
    {
        if (cachedMaterial == null)
            cachedMaterial = Resources.Load<Material>("Materials/AttackTelegraph");
        return cachedMaterial;
    }
}
