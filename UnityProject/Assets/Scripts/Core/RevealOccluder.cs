using System.Collections.Generic;
using UnityEngine;

// Decor that swaps to the reveal material while it hides the local player, so a see-through circle opens around them.
public class RevealOccluder : MonoBehaviour
{
    public static readonly List<RevealOccluder> Active = new List<RevealOccluder>();

    private static readonly int RevealOpenId = Shader.PropertyToID("_RevealOpen");
    private static Material litRevealMaterial;
    private static Material unlitRevealMaterial;
    private static bool revealMaterialsLoaded;

    [Tooltip("Ignore sorting and reveal whenever it covers the player, for sprites drawn on a layer above everything (e.g. Sky clouds).")]
    [SerializeField] private bool alwaysInFront;

    public bool AlwaysInFront
    {
        get => alwaysInFront;
        set => alwaysInFront = value;
    }

    private SpriteRenderer[] renderers;
    private Material[] originalMaterials;
    private MaterialPropertyBlock block;
    private bool swapped;
    private float open;
    private float targetOpen;
    private float speed = 8f;

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

    void OnEnable()
    {
        Active.Add(this);
    }

    // Pooled decor comes back with its own material.
    void OnDisable()
    {
        Active.Remove(this);
        targetOpen = 0f;
        open = 0f;
        Restore();
    }

    // Called every frame by the local player; the circle eases open or closed here.
    public void SetTarget(bool covering, float openSpeed)
    {
        targetOpen = covering ? 1f : 0f;
        speed = openSpeed;
    }

    void Update()
    {
        if (open == targetOpen && !swapped)
            return;

        open = Mathf.MoveTowards(open, targetOpen, speed * Time.deltaTime);
        if (open <= 0f)
        {
            Restore();
            return;
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

    // True when one of its visible sprites covers the point and sorts above the given order on that layer (or always, when alwaysInFront).
    public bool Covers(Vector3 point, int sortingLayerId, int sortingOrder)
    {
        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer == null || !spriteRenderer.enabled || !spriteRenderer.isVisible)
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
