using System.Collections.Generic;
using UnityEngine;

// Decor that swaps to the reveal material while it hides the local player, so a see-through circle opens around them.
public class RevealOccluder : MonoBehaviour
{
    public static readonly List<RevealOccluder> Active = new List<RevealOccluder>();
    private static readonly List<RevealOccluder> Animating = new List<RevealOccluder>();

    private static readonly int RevealOpenId = Shader.PropertyToID("_RevealOpen");
    private static Material litRevealMaterial;
    private static Material unlitRevealMaterial;
    private static bool revealMaterialsLoaded;

    [Tooltip("Ignore sorting and reveal whenever it covers the player, for sprites drawn on a layer above everything (e.g. Sky clouds).")]
    [SerializeField] private bool alwaysInFront;
    [Tooltip("Recompute the covered area every frame, for occluders that move (e.g. drifting clouds); static decor computes it once.")]
    [SerializeField] private bool moves;

    public bool AlwaysInFront
    {
        get => alwaysInFront;
        set => alwaysInFront = value;
    }

    public bool Moves
    {
        get => moves;
        set => moves = value;
    }

    private SpriteRenderer[] renderers;
    private Material[] originalMaterials;
    private MaterialPropertyBlock block;
    private bool swapped;
    private bool animating;
    private float open;
    private float targetOpen;
    private float speed = 8f;
    private Rect area;
    private int areaFrame = -1;

    // Sprites under a nested occluder (e.g. houses inside HouseBunch) belong to that one, so no sprite is driven twice.
    void Awake()
    {
        List<SpriteRenderer> owned = new List<SpriteRenderer>();
        foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (spriteRenderer.GetComponentInParent<RevealOccluder>(true) == this)
                owned.Add(spriteRenderer);
        }
        renderers = owned.ToArray();
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].sharedMaterial;
        block = new MaterialPropertyBlock();
    }

    // Area is recomputed on the first query after (re)enabling, once the spawner has placed it for this frame.
    void OnEnable()
    {
        Active.Add(this);
        areaFrame = -1;
    }

    // Pooled decor comes back with its own material.
    void OnDisable()
    {
        Active.Remove(this);
        if (animating)
        {
            Animating.Remove(this);
            animating = false;
        }
        targetOpen = 0f;
        open = 0f;
        Restore();
    }

    // Called every frame by the local player; only occluders whose circle has to move get queued for TickAll.
    public void SetTarget(bool covering, float openSpeed)
    {
        targetOpen = covering ? 1f : 0f;
        speed = openSpeed;
        if (!animating && open != targetOpen)
        {
            animating = true;
            Animating.Add(this);
        }
    }

    // Eases every queued occluder towards its target, dropping it from the queue once settled.
    public static void TickAll(float deltaTime)
    {
        for (int i = Animating.Count - 1; i >= 0; i--)
        {
            RevealOccluder occluder = Animating[i];
            if (occluder != null && occluder.Step(deltaTime))
                continue;
            if (occluder != null)
                occluder.animating = false;
            Animating.RemoveAt(i);
        }
    }

    // Returns false once the circle is settled (fully closed and restored, or fully open).
    private bool Step(float deltaTime)
    {
        open = Mathf.MoveTowards(open, targetOpen, speed * deltaTime);
        if (open <= 0f)
        {
            Restore();
            return false;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            if (!swapped)
            {
                Material material = GetRevealMaterial(originalMaterials[i]);
                if (material != null)
                    renderers[i].sharedMaterial = material;
            }
            renderers[i].GetPropertyBlock(block);
            block.SetFloat(RevealOpenId, open);
            renderers[i].SetPropertyBlock(block);
        }
        swapped = true;
        return open != targetOpen;
    }

    private void Restore()
    {
        if (!swapped)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            renderers[i].sharedMaterial = originalMaterials[i];
            renderers[i].GetPropertyBlock(block);
            block.SetFloat(RevealOpenId, 0f);
            renderers[i].SetPropertyBlock(block);
        }
        swapped = false;
    }

    // Unlit sprites (e.g. clouds) get the unlit variant, since the lit one would go dark outside 2D lights.
    private static Material GetRevealMaterial(Material original)
    {
        if (!revealMaterialsLoaded)
        {
            litRevealMaterial = Resources.Load<Material>("Materials/SpriteLitReveal");
            unlitRevealMaterial = Resources.Load<Material>("Materials/SpriteUnlitReveal");
            revealMaterialsLoaded = true;
        }
        bool unlit = original != null && original.shader != null && original.shader.name.Contains("Unlit");
        return unlit ? unlitRevealMaterial : litRevealMaterial;
    }

    // Union of the owned sprites' bounds, cached for static decor so the per-frame test stays in managed code.
    private Rect Area()
    {
        if (areaFrame >= 0 && (!moves || areaFrame == Time.frameCount))
            return area;

        areaFrame = Time.frameCount;
        bool any = false;
        Bounds union = default;
        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer == null)
                continue;
            if (!any)
                union = spriteRenderer.bounds;
            else
                union.Encapsulate(spriteRenderer.bounds);
            any = true;
        }
        area = any ? new Rect(union.min.x, union.min.y, union.size.x, union.size.y) : new Rect(float.MaxValue, float.MaxValue, 0f, 0f);
        return area;
    }

    // Cheap cached-area rejection first; only occluders actually around the point pay for the per-sprite sorting checks.
    public bool Covers(Vector3 point, int sortingLayerId, int sortingOrder)
    {
        if (!Area().Contains(point))
            return false;

        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer == null || !spriteRenderer.enabled)
                continue;
            if (!alwaysInFront && (spriteRenderer.sortingLayerID != sortingLayerId || spriteRenderer.sortingOrder <= sortingOrder))
                continue;

            Bounds bounds = spriteRenderer.bounds;
            if (point.x >= bounds.min.x && point.x <= bounds.max.x && point.y >= bounds.min.y && point.y <= bounds.max.y)
                return true;
        }
        return false;
    }
}
