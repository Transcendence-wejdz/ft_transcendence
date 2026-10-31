using UnityEngine;

// Ground ring at the player's feet pointing at the mouse: ring plus a bolder tip, tinted to the player's color over a dark outline.
public class MouseDirectionIndicator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer ringRenderer;
    [SerializeField] private SpriteRenderer tipRenderer;
    [SerializeField] private SpriteRenderer outlineRenderer;
    [Range(0f, 1f)]
    [SerializeField] private float ringAlpha = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float tipAlpha = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float outlineAlpha = 0.55f;

    private const float SpriteUpOffset = 90f;

    private Player player;

    private void ApplyColor(int classIndex, int colorIndex)
    {
        Color color = Color.white;

        if (CharacterCatalog.Instance != null)
            color = CharacterCatalog.Instance.GetFxLightColor(colorIndex);
        SetColor(ringRenderer, color, ringAlpha);
        SetColor(tipRenderer, color, tipAlpha);
        SetColor(outlineRenderer, Color.black, outlineAlpha);
    }

    private static void SetColor(SpriteRenderer target, Color color, float alpha)
    {
        if (target == null)
            return;
        color.a = alpha;
        target.color = color;
    }

    private void SetVisible(bool visible)
    {
        ringRenderer.enabled = visible;
        if (tipRenderer != null)
            tipRenderer.enabled = visible;
        if (outlineRenderer != null)
            outlineRenderer.enabled = visible;
    }

    void Awake()
    {
        if (ringRenderer == null)
            ringRenderer = GetComponent<SpriteRenderer>();
        player = GetComponentInParent<Player>();
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
    }

    // Hidden while the player is a pig (dead), since a downed player can't aim or fight; the aim angle itself doesn't change while paused.
    void Update()
    {
        bool visible;

        if (player == null)
            return;
        visible = !player.IsDead;
        SetVisible(visible);
        if (visible)
            transform.rotation = Quaternion.Euler(0f, 0f, player.AimAngleDegrees - SpriteUpOffset);
    }
}
