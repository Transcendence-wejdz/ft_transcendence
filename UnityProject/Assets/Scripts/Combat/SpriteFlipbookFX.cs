using UnityEngine;

// Visual-only one-shot sprite animation (e.g. a projectile's impact): plays its frames once, then destroys itself.
public class SpriteFlipbookFX : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 10f;

    private float timer;
    private int frameIndex;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || frameRate <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;
        int next = Mathf.FloorToInt(timer * frameRate);
        if (next >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }

        if (next != frameIndex)
        {
            frameIndex = next;
            spriteRenderer.sprite = frames[frameIndex];
        }
    }
}
