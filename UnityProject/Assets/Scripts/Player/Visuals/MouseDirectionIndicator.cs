using UnityEngine;

// Ground ring at the player's feet pointing at the mouse.
public class MouseDirectionIndicator : MonoBehaviour
{
    // Indexed by ColorIndex: Black, Blue, Purple, Red, Yellow.
    [SerializeField] private Color[] colorsByColorIndex =
    {
        new Color(0.15f, 0.15f, 0.15f),
        new Color(0.25f, 0.45f, 0.95f),
        new Color(0.6f, 0.3f, 0.85f),
        new Color(0.85f, 0.25f, 0.25f),
        new Color(0.95f, 0.8f, 0.2f),
    };

    private SpriteRenderer spriteRenderer;
    private Player player;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        if (colorIndex < 0 || colorIndex >= colorsByColorIndex.Length)
            return;

        spriteRenderer.color = colorsByColorIndex[colorIndex];
    }

    // Hidden while the player is a pig (dead), since a downed player can't aim or fight; the aim angle itself doesn't change while paused.
    void Update()
    {
        if (player == null)
            return;

        spriteRenderer.enabled = !player.IsDead;
        if (!spriteRenderer.enabled)
            return;

        transform.rotation = Quaternion.Euler(0f, 0f, player.AimAngleDegrees - 90f);
    }
}
