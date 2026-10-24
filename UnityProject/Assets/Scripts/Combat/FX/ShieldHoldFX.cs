using UnityEngine;

// Three-stage flipbook (start/loop/end) for a held shield; polls the owner's IsShieldActive, ending only once it was seen active (the spawn RPC can beat that variable to remote peers).
public class ShieldHoldFX : MonoBehaviour, IAttackFX, IHealthStats
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] startFrames;
    [SerializeField] private Sprite[] loopFrames;
    [SerializeField] private Sprite[] endFrames;
    [SerializeField] private float frameRate = 24f;
    // Reparented onto the attacker directly in Init so it stays upright above their head instead of orbiting/rotating with the shield sprite.
    [SerializeField] private Transform healthBarRoot;

    private enum Stage { Start, Loop, End }

    private const float ActivationGraceSeconds = 1f;

    private Player owner;
    private Sprite[] currentFrames;
    private Stage stage = Stage.Start;
    private int frameIndex;
    private float frameTimer;
    private float lastReportedHealth = -1f;
    private bool tracksFacing = true;
    private float fxDistance;
    private bool seenActive;
    private float initTime;

    public Player Owner => owner;
    public float CurrentHealth => owner != null ? owner.CurrentShieldHealth : 0f;
    public float MaxHealth => owner != null ? owner.ShieldMaxHealth : 0f;
    public event System.Action<float, float> OnHealthChanged;

    // On other peers the wall also blocks their own player (includeLayers); on the caster's machine it passes through ally copies, which each peer pushes out itself.
    public void Init(GameObject attacker, bool hasHitbox, ClassKit kit, AbilityType ability, float damageMultiplier)
    {
        owner = attacker.GetComponent<Player>();
        if (kit == null || kit.UltimateFollowCaster)
            transform.SetParent(attacker.transform);
        tracksFacing = kit == null || !kit.UltimateIgnoreAimRotation;
        fxDistance = kit != null ? kit.UltimateFxDistance : 0f;
        currentFrames = startFrames;
        initTime = Time.time;

        if (healthBarRoot != null)
            healthBarRoot.SetParent(attacker.transform, false);

        Collider2D wall = GetComponent<Collider2D>();
        if (wall != null && attacker != LocalPlayer.Get())
            wall.includeLayers |= 1 << attacker.layer;
        if (owner != null)
            owner.RegisterShieldWall(wall);

        SortingLayer_Auto attackerSorting = attacker.GetComponent<SortingLayer_Auto>();
        if (attackerSorting != null && spriteRenderer != null)
            spriteRenderer.sortingOrder = attackerSorting.CurrentSortingOrder + 1;
    }

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (startFrames != null && startFrames.Length > 0)
            spriteRenderer.sprite = startFrames[0];
    }

    void Update()
    {
        if (owner == null)
        {
            if (stage != Stage.End)
                EnterEndStage();
        }
        else
        {
            float current = owner.CurrentShieldHealth;
            if (!Mathf.Approximately(current, lastReportedHealth))
            {
                lastReportedHealth = current;
                OnHealthChanged?.Invoke(current, MaxHealth);
            }

            if (owner.IsShieldActive)
                seenActive = true;
            else if (stage != Stage.End && (seenActive || Time.time - initTime > ActivationGraceSeconds))
                EnterEndStage();

            if (tracksFacing)
            {
                float angle = owner.ShieldFacingAngle;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                transform.localPosition = dir * fxDistance;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        AdvanceFrames();
    }

    void OnDestroy()
    {
        if (healthBarRoot != null)
            Destroy(healthBarRoot.gameObject);
    }

    private void EnterEndStage()
    {
        stage = Stage.End;
        currentFrames = endFrames;
        frameIndex = 0;
        frameTimer = 0f;
    }

    private void AdvanceFrames()
    {
        if (currentFrames == null || currentFrames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        float frameDuration = 1f / frameRate;
        frameTimer += Time.deltaTime;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;
            if (frameIndex >= currentFrames.Length)
            {
                if (stage == Stage.Start)
                {
                    stage = Stage.Loop;
                    currentFrames = loopFrames;
                    frameIndex = 0;
                }
                else if (stage == Stage.Loop)
                {
                    frameIndex = 0;
                }
                else
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (spriteRenderer != null && currentFrames != null && frameIndex < currentFrames.Length)
                spriteRenderer.sprite = currentFrames[frameIndex];
        }
    }
}
