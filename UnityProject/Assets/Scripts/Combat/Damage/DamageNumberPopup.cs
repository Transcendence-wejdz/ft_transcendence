using System.Collections;
using TMPro;
using UnityEngine;

// One-shot floating damage number: pops in, rises with ease-out and fades over its last part, then destroys itself.
public class DamageNumberPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float riseDistance = 0.8f;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float randomXSpread = 0.3f;
    [SerializeField] private float popScale = 1.5f;
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float fadeStart = 0.55f;
    [SerializeField] private float bigHitAmount = 50f;
    [SerializeField] private float bigHitScale = 1.35f;
    [SerializeField] private Color bigHitColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color allyColor = new Color(0.62f, 0.7f, 0.8f, 1f);
    [SerializeField] private float allyScale = 0.75f;

    private Vector3 baseScale;

    private static float EaseOut(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private float AlphaAt(float progress)
    {
        if (progress < fadeStart)
            return 1f;
        return 1f - (progress - fadeStart) / (1f - fadeStart);
    }

    // Allies' hits are smaller and tinted so the local player's own numbers stand out.
    private void ApplyLook(float amount, bool own)
    {
        if (!own)
        {
            baseScale *= allyScale;
            label.color = allyColor;
        }
        else if (amount >= bigHitAmount)
        {
            baseScale *= bigHitScale;
            label.color = bigHitColor;
        }
    }

    private IEnumerator Animate()
    {
        Vector3 start = transform.position;
        Vector3 end = start + new Vector3(0f, riseDistance, 0f);
        float t = 0f;
        float p;
        float pop;

        while (t < duration)
        {
            t += Time.deltaTime;
            p = Mathf.Clamp01(t / duration);
            pop = Mathf.Clamp01(t / popDuration);
            transform.position = Vector3.Lerp(start, end, EaseOut(p));
            transform.localScale = baseScale * Mathf.Lerp(popScale, 1f, EaseOut(pop));
            canvasGroup.alpha = AlphaAt(p);
            yield return null;
        }
        Destroy(gameObject);
    }

    public void Show(float amount, bool own)
    {
        label.text = Mathf.RoundToInt(amount).ToString();
        baseScale = transform.localScale;
        ApplyLook(amount, own);
        transform.position += new Vector3(Random.Range(-randomXSpread, randomXSpread), 0f, 0f);
        StartCoroutine(Animate());
    }
}
