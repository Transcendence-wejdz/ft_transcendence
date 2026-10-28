using System.Collections;
using UnityEngine;

// Briefly tints the sprite on a hit or health drop: held at full tint, then eased back.
public class SpriteHitFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float duration = 0.18f;
    [SerializeField] private float holdFraction = 0.35f;

    private SpriteRenderer spriteRenderer;
    private IHealthStats stats;
    private float lastHealth;
    private Coroutine running;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        stats = GetComponent<IHealthStats>();
    }

    void OnEnable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged += HandleHealthChanged;
            StartCoroutine(CaptureBaselineNextFrame());
        }
    }

    // Deferred a frame: OnEnable can run before a sibling component's Awake sets the real starting health.
    private IEnumerator CaptureBaselineNextFrame()
    {
        yield return null;
        lastHealth = stats.CurrentHealth;
    }

    void OnDisable()
    {
        if (stats != null)
            stats.OnHealthChanged -= HandleHealthChanged;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
        running = null;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (current < lastHealth)
            Flash();
        lastHealth = current;
    }

    public void Flash()
    {
        if (spriteRenderer == null || !isActiveAndEnabled)
            return;
        if (running != null)
            StopCoroutine(running);
        running = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float fade = Mathf.Clamp01((t / duration - holdFraction) / (1f - holdFraction));
            spriteRenderer.color = Color.Lerp(flashColor, Color.white, fade * fade);
            yield return null;
        }
        spriteRenderer.color = Color.white;
        running = null;
    }
}
