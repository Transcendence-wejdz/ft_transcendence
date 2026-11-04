using UnityEngine;

// Fire left on the ground by an enraged charge: burning ground glow under looping flames, local to each peer, only burns this machine's player.
public class BossFirePatch : MonoBehaviour
{
    private const float BurnInterval = 0.5f;
    private const float FadeSeconds = 0.5f;
    private const int BelowEverything = -32000;

    private static float nextBurnTime;

    [SerializeField] private float radius = 0.8f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 12f;
    [SerializeField] private float minFlameScale = 0.6f;
    [SerializeField] private float maxFlameScale = 1.5f;
    [SerializeField] private Material groundMaterial;
    [SerializeField] private Color groundColor = new Color(1f, 0.3f, 0.05f, 0.45f);
    [SerializeField] private float groundDiameter = 2.4f;

    private float damagePerSecond;
    private float endTime;
    private int frameOffset;
    private SpriteRenderer ground;

    public void Init(float damage, float lifetime)
    {
        damagePerSecond = damage;
        endTime = Time.time + lifetime;
        frameOffset = Random.Range(0, frames.Length);
        transform.localScale *= Random.Range(minFlameScale, maxFlameScale);
        CreateGround();
    }

    private void CreateGround()
    {
        GameObject child = new GameObject("BurningGround");

        child.transform.SetParent(transform, false);
        child.transform.localScale = Vector3.one * (groundDiameter / transform.lossyScale.x);
        ground = child.AddComponent<SpriteRenderer>();
        ground.sharedMaterial = groundMaterial;
        ground.sprite = BossFxSprites.Glow;
        ground.sortingLayerID = spriteRenderer.sortingLayerID;
        ground.sortingOrder = BelowEverything;
    }

    // The shared timer keeps overlapping patches from stacking their damage.
    private void TryBurnLocalPlayer()
    {
        Player local;

        if (Time.time < nextBurnTime || GameOverCheck.IsRunOver)
            return;
        local = LocalPlayer.GetPlayer();
        if (local == null || local.IsDead)
            return;
        if ((local.Center - (Vector2)transform.position).sqrMagnitude > radius * radius)
            return;
        nextBurnTime = Time.time + BurnInterval;
        local.TakeDamage(damagePerSecond * BurnInterval);
    }

    private void UpdateLook()
    {
        float fade = Mathf.Clamp01((endTime - Time.time) / FadeSeconds);
        Color color = groundColor;

        spriteRenderer.color = new Color(1f, 1f, 1f, fade);
        color.a *= fade;
        ground.color = color;
        spriteRenderer.sprite = frames[(Mathf.FloorToInt(Time.time * frameRate) + frameOffset) % frames.Length];
    }

    void Update()
    {
        if (Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }
        UpdateLook();
        TryBurnLocalPlayer();
    }
}
