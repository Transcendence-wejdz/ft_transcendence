using System.Collections;
using UnityEngine;

// How decor looks when hit, broken, and reused by the pool.
public partial class DestructibleDecor
{
    private const float ShakeSeconds = 0.15f;
    private const float ShakeDistance = 0.04f;
    private const float ShakeFrequency = 60f;
    private const float BreakFxDarkening = 0.25f;

    [SerializeField] private ParticleSystem hitFxPrefab;
    [SerializeField] private ParticleSystem breakFxPrefab;
    [SerializeField] private Color breakFxColor = Color.white;

    private Coroutine shake;

    private void StopShake()
    {
        if (shake != null)
            StopCoroutine(shake);
        shake = null;
        spriteRenderer.transform.localPosition = spriteRestPosition;
    }

    private IEnumerator Shake()
    {
        float elapsed = 0f;
        float offset;

        while (elapsed < ShakeSeconds)
        {
            offset = Mathf.Sin(elapsed * ShakeFrequency) * ShakeDistance * (1f - elapsed / ShakeSeconds);
            spriteRenderer.transform.localPosition = spriteRestPosition + Vector3.right * offset;
            elapsed += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.transform.localPosition = spriteRestPosition;
        shake = null;
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (Collider2D part in colliders)
            part.enabled = enabled;
    }

    private void SetIntactLook(bool intact)
    {
        flipbook.enabled = intact;
        if (occluder != null)
            occluder.enabled = intact;
    }

    private void PlayFx(ParticleSystem prefab)
    {
        ParticleSystem fx;
        ParticleSystem.MainModule main;

        if (prefab == null || !gameObject.activeInHierarchy)
            return;
        fx = Instantiate(prefab, spriteRenderer.bounds.center, Quaternion.identity);
        main = fx.main;
        main.startColor = new ParticleSystem.MinMaxGradient(breakFxColor, Color.Lerp(breakFxColor, Color.black, BreakFxDarkening));
    }

    private void Break(bool withEffects)
    {
        broken = true;
        StopShake();
        if (withEffects)
            PlayFx(breakFxPrefab);
        SetCollidersEnabled(false);
        SetIntactLook(false);
        if (stumpSprite != null)
            spriteRenderer.sprite = stumpSprite;
        else
            spriteRenderer.enabled = false;
    }

    private void PlayHitFeedback()
    {
        PlayFx(hitFxPrefab);
        if (gameObject.activeInHierarchy)
            shake ??= StartCoroutine(Shake());
    }

    private void Repair()
    {
        broken = false;
        StopShake();
        spriteRenderer.enabled = true;
        SetCollidersEnabled(true);
        SetIntactLook(true);
    }
}
