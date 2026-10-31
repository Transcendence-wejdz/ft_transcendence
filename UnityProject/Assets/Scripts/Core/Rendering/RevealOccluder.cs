using System.Collections.Generic;
using UnityEngine;

// Decor that swaps to the reveal material while it hides the local player, so a see-through circle opens around them.
public partial class RevealOccluder : MonoBehaviour
{
    public static readonly List<RevealOccluder> Active = new List<RevealOccluder>();
    private static readonly List<RevealOccluder> Animating = new List<RevealOccluder>();

    private static readonly int RevealOpenId = Shader.PropertyToID("_RevealOpen");
    private static readonly Rect NoArea = new Rect(float.MaxValue, float.MaxValue, 0f, 0f);
    private static Material litRevealMaterial;
    private static Material unlitRevealMaterial;
    private static bool revealMaterialsLoaded;

    [Tooltip("Ignore sorting and reveal whenever it covers the player, for sprites drawn on a layer above everything (e.g. Sky clouds).")]
    [SerializeField] private bool alwaysInFront;
    [Tooltip("Recompute the covered area every frame, for occluders that move (e.g. drifting clouds); static decor computes it once.")]
    [SerializeField] private bool moves;

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

    private bool IsAreaCached => areaFrame >= 0 && (!moves || areaFrame == Time.frameCount);

    private static void LoadRevealMaterials()
    {
        if (revealMaterialsLoaded)
            return;
        litRevealMaterial = Resources.Load<Material>("Materials/SpriteLitReveal");
        unlitRevealMaterial = Resources.Load<Material>("Materials/SpriteUnlitReveal");
        revealMaterialsLoaded = true;
    }

    // Unlit sprites (e.g. clouds) get the unlit variant, since the lit one would go dark outside 2D lights.
    private static Material GetRevealMaterial(Material original)
    {
        LoadRevealMaterials();
        if (original != null && original.shader != null && original.shader.name.Contains("Unlit"))
            return unlitRevealMaterial;
        return litRevealMaterial;
    }

    // Sprites under a nested occluder (e.g. houses inside HouseBunch) belong to that one, so no sprite is driven twice.
    private SpriteRenderer[] CollectOwnedRenderers()
    {
        List<SpriteRenderer> owned = new List<SpriteRenderer>();

        foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (spriteRenderer.GetComponentInParent<RevealOccluder>(true) == this)
                owned.Add(spriteRenderer);
        }
        return owned.ToArray();
    }

    private void SetOpen(SpriteRenderer renderer, float value)
    {
        renderer.GetPropertyBlock(block);
        block.SetFloat(RevealOpenId, value);
        renderer.SetPropertyBlock(block);
    }

    private void SwapToReveal(int i)
    {
        Material material = GetRevealMaterial(originalMaterials[i]);

        if (material != null)
            renderers[i].sharedMaterial = material;
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
            SetOpen(renderers[i], 0f);
        }
        swapped = false;
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
                SwapToReveal(i);
            SetOpen(renderers[i], open);
        }
        swapped = true;
        return open != targetOpen;
    }

    private void StopAnimating()
    {
        if (!animating)
            return;
        Animating.Remove(this);
        animating = false;
    }

    // Called every frame by the local player; only occluders whose circle has to move get queued for TickAll.
    public void SetTarget(bool covering, float openSpeed)
    {
        targetOpen = 0f;
        if (covering)
            targetOpen = 1f;
        speed = openSpeed;
        if (animating || open == targetOpen)
            return;
        animating = true;
        Animating.Add(this);
    }

    // Eases every queued occluder towards its target, dropping it from the queue once settled.
    public static void TickAll(float deltaTime)
    {
        RevealOccluder occluder;

        for (int i = Animating.Count - 1; i >= 0; i--)
        {
            occluder = Animating[i];
            if (occluder != null && occluder.Step(deltaTime))
                continue;
            if (occluder != null)
                occluder.animating = false;
            Animating.RemoveAt(i);
        }
    }

    void Awake()
    {
        renderers = CollectOwnedRenderers();
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
        StopAnimating();
        targetOpen = 0f;
        open = 0f;
        Restore();
    }
}
