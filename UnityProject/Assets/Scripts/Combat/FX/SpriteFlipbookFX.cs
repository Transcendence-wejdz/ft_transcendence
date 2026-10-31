using UnityEngine;

// Visual-only one-shot sprite animation (e.g. a projectile's impact): plays its frames once, then destroys itself.
public class SpriteFlipbookFX : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 10f;

    private float timer;
    private int frameIndex;

    private bool HasFrames => frames != null && frames.Length > 0;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && HasFrames)
            spriteRenderer.sprite = frames[0];
    }

    void Update()
    {
        int next;

        if (!HasFrames || frameRate <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        timer += Time.deltaTime;
        next = Mathf.FloorToInt(timer * frameRate);
        if (next >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }
        if (next == frameIndex)
            return;
        frameIndex = next;
        spriteRenderer.sprite = frames[frameIndex];
    }
}
