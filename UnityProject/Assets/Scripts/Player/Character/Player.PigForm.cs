using UnityEngine;

// Dead players turn into a pig that can still walk (and be ridden to revive them) but not fight.
public partial class Player
{
    private const int StartupGraceFrames = 2;
    private const float PigFacingDeadZone = 0.01f;
    private const float TransformFxLifetime = 1f;

    [Header("Pig Form")]
    [SerializeField] private GameObject pigPrefab;
    [SerializeField] private float pigScale = 1.5f;
    [SerializeField] private GameObject transformFxPrefab;
    [SerializeField] private Vector2 transformFxOffset = new Vector2(0f, -1.1f);

    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int ExplosionSpellHash = Animator.StringToHash("Explosion Spell");

    private int enemyMask;

    private GameObject pig;
    private SpriteRenderer pigRenderer;
    private Animator pigAnimator;
    private bool isPig;
    private bool hasCheckedState;

    private int currentAnimationHash;
    private int startFrame;

    private void AwakePigForm()
    {
        enemyMask = LayerMask.GetMask("Enemy");
    }

    private void StartPigForm()
    {
        startFrame = Time.frameCount;
    }

    // Health reads 0 until stats initialise it a frame after spawn, so the first frames are ignored.
    private void UpdatePigForm()
    {
        bool dead;

        if (pigPrefab == null || Time.frameCount < startFrame + StartupGraceFrames)
            return;
        dead = IsDead;
        if (dead != isPig)
        {
            SetPig(dead);
            if (dead && hasCheckedState)
                PlayTransformFx();
        }
        hasCheckedState = true;
        if (isPig)
            UpdatePigVisuals();
    }

    private Vector3 BodyCenter()
    {
        if (bodyCollider == null)
            return transform.position;
        return bodyCollider.bounds.center;
    }

    // Smoke burst over the body; skipped for players already dead when first seen.
    private void PlayTransformFx()
    {
        GameObject fx;
        SpriteRenderer fxRenderer;

        if (transformFxPrefab == null)
            return;
        fx = Instantiate(transformFxPrefab, BodyCenter() + (Vector3)transformFxOffset, Quaternion.identity);
        fxRenderer = fx.GetComponent<SpriteRenderer>();
        fxRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        fxRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        fx.GetComponent<Animator>().Play(ExplosionSpellHash, 0, 0f);
        Destroy(fx, TransformFxLifetime);
    }

    // Dead players walk through enemies instead of blocking or being shoved by them.
    private void SetIgnoreEnemies(bool ignore)
    {
        if (rb == null)
            return;
        if (ignore)
            rb.excludeLayers = rb.excludeLayers | enemyMask;
        else
            rb.excludeLayers = rb.excludeLayers & ~enemyMask;
    }

    private void SetPig(bool active)
    {
        isPig = active;
        if (active && pig == null)
            CreatePig();
        if (spriteRenderer != null)
            spriteRenderer.enabled = !active;
        if (pig != null)
            pig.SetActive(active);
        SetIgnoreEnemies(active);
        if (mountInteractRoot != null)
            mountInteractRoot.SetActive(active);
        currentAnimationHash = 0;
    }

    private void CreatePig()
    {
        pig = Instantiate(pigPrefab, transform);
        pig.transform.localPosition = Vector3.zero;
        pig.transform.localScale = Vector3.one * pigScale;
        pigRenderer = pig.GetComponent<SpriteRenderer>();
        pigAnimator = pig.GetComponent<Animator>();
        pig.transform.position += BodyCenter() - pigRenderer.bounds.center;
    }

    // Reuses the body's own (NetworkAnimator-synced) params instead of a remote Transform, which can read as moving at rest.
    private void UpdatePigFacing()
    {
        float lastInputX;

        if (animator == null)
            return;
        lastInputX = animator.GetFloat(LastInputXHash);
        if (Mathf.Abs(lastInputX) > PigFacingDeadZone)
            pigRenderer.flipX = lastInputX < 0f;
    }

    private void UpdatePigAnimation()
    {
        int desired = IdleHash;

        if (animator != null && animator.GetBool(IsWalkingHash))
            desired = RunHash;
        if (desired == currentAnimationHash)
            return;
        currentAnimationHash = desired;
        pigAnimator.Play(desired, 0, 0f);
    }

    private void SortPig()
    {
        pigRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        if (sortingLayerAuto != null)
            pigRenderer.sortingOrder = sortingLayerAuto.CurrentSortingOrder;
        else
            pigRenderer.sortingOrder = spriteRenderer.sortingOrder;
    }

    private void UpdatePigVisuals()
    {
        UpdatePigFacing();
        UpdatePigAnimation();
        SortPig();
    }
}
