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

    private Player player;

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

    private void ApplyColor(int classIndex, int colorIndex)
    {
        Color color = CharacterCatalog.Instance != null ? CharacterCatalog.Instance.GetFxLightColor(colorIndex) : Color.white;
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

    // Hidden while the player is a pig (dead), since a downed player can't aim or fight; the aim angle itself doesn't change while paused.
    void Update()
    {
        if (player == null)
            return;

        bool visible = !player.IsDead;
        ringRenderer.enabled = visible;
        if (tipRenderer != null)
            tipRenderer.enabled = visible;
        if (outlineRenderer != null)
            outlineRenderer.enabled = visible;
        if (!visible)
            return;

        transform.rotation = Quaternion.Euler(0f, 0f, player.AimAngleDegrees - 90f);
    }
}
