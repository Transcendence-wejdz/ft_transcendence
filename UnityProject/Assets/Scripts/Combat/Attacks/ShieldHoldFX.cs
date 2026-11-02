using UnityEngine;

// Start/loop/end flipbook for a held shield; ends once the owner's IsShieldActive was seen true then false.
public class ShieldHoldFX : MonoBehaviour, IAttackFX, IHealthStats
{
    private enum Stage { Start, Loop, End }

    private const float ActivationGraceSeconds = 1f;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] startFrames;
    [SerializeField] private Sprite[] loopFrames;
    [SerializeField] private Sprite[] endFrames;
    [SerializeField] private float frameRate = 24f;
    [Tooltip("Reparented onto the attacker in Init so it stays upright above their head instead of rotating with the shield sprite.")]
    [SerializeField] private Transform healthBarRoot;

    private Player owner;
    private Sprite[] currentFrames;
    private Stage stage = Stage.Start;
    private int frameIndex;
    private float frameTimer;
    private float lastReportedHealth = -1f;
    private bool tracksFacing = true;
    private bool seenActive;
    private float initTime;

    public Player Owner => owner;

    public float CurrentHealth
    {
        get
        {
            if (owner == null)
                return 0f;
            return owner.CurrentShieldHealth;
        }
    }

    public float MaxHealth
    {
        get
        {
            if (owner == null)
                return 0f;
            return owner.ShieldMaxHealth;
        }
    }

    public event System.Action<float, float> OnHealthChanged;

    private void SortOverAttacker(GameObject attacker)
    {
        SortingLayer_Auto attackerSorting = attacker.GetComponent<SortingLayer_Auto>();

        if (attackerSorting != null && spriteRenderer != null)
            spriteRenderer.sortingOrder = attackerSorting.CurrentSortingOrder + 1;
    }

    private void EnterEndStage()
    {
        stage = Stage.End;
        currentFrames = endFrames;
        frameIndex = 0;
        frameTimer = 0f;
    }

    private void ReportHealth()
    {
        float current = owner.CurrentShieldHealth;

        if (Mathf.Approximately(current, lastReportedHealth))
            return;
        lastReportedHealth = current;
        OnHealthChanged?.Invoke(current, MaxHealth);
    }

    // Ends once the shield was seen up then dropped, or if it never came up within the grace time.
    private void CheckShieldDropped()
    {
        if (owner.IsShieldActive)
        {
            seenActive = true;
            return;
        }
        if (stage == Stage.End)
            return;
        if (seenActive || Time.time - initTime > ActivationGraceSeconds)
            EnterEndStage();
    }

    private void FollowFacing()
    {
        float angle = owner.ShieldFacingAngle;
        float radians = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

        transform.localPosition = owner.FxOffset(AbilityType.Ultimate, dir);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void TrackOwner()
    {
        ReportHealth();
        CheckShieldDropped();
        if (tracksFacing)
            FollowFacing();
    }

    // Start plays once into Loop, Loop wraps, End destroys the FX; false once destroyed.
    private bool WrapStage()
    {
        if (stage == Stage.Start)
        {
            stage = Stage.Loop;
            currentFrames = loopFrames;
            frameIndex = 0;
            return true;
        }
        if (stage == Stage.Loop)
        {
            frameIndex = 0;
            return true;
        }
        Destroy(gameObject);
        return false;
    }

    private void ShowCurrentFrame()
    {
        if (spriteRenderer != null && currentFrames != null && frameIndex < currentFrames.Length)
            spriteRenderer.sprite = currentFrames[frameIndex];
    }

    private void AdvanceFrames()
    {
        float frameDuration = 1f / frameRate;

        if (currentFrames == null || currentFrames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }
        frameTimer += Time.deltaTime;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;
            if (frameIndex >= currentFrames.Length && !WrapStage())
                return;
            ShowCurrentFrame();
        }
    }

    public void Init(GameObject attacker, bool hasHitbox, AbilityKit ability, float damageMultiplier)
    {
        owner = attacker.GetComponent<Player>();
        if (spriteRenderer != null)
            spriteRenderer.color = CharacterCatalog.FxColorOf(attacker);
        FxGlow.Setup(gameObject, attacker, ability);
        if (ability == null || ability.FollowCaster)
            transform.SetParent(attacker.transform);
        tracksFacing = ability == null || !ability.IgnoreAimRotation;
        currentFrames = startFrames;
        initTime = Time.time;
        if (healthBarRoot != null)
            healthBarRoot.SetParent(attacker.transform, false);
        SortOverAttacker(attacker);
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
        if (owner != null)
            TrackOwner();
        else if (stage != Stage.End)
            EnterEndStage();
        AdvanceFrames();
    }

    void OnDestroy()
    {
        if (healthBarRoot != null)
            Destroy(healthBarRoot.gameObject);
    }
}
