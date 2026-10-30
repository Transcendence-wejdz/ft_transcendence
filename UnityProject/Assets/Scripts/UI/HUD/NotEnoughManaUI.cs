using TMPro;
using UnityEngine;

// "Not enough mana" warning above the ability bar: pops in on a failed cast, holds, then fades.
public class NotEnoughManaUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float holdDuration = 0.6f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float popScale = 1.25f;
    [SerializeField] private float shakeAmount = 3f;

    private float shownAt = -10f;
    private Vector2 restPosition;

    void Awake()
    {
        restPosition = label.rectTransform.anchoredPosition;
        label.alpha = 0f;
    }

    void OnEnable()
    {
        Player.OnNotEnoughMana += Show;
    }

    void OnDisable()
    {
        Player.OnNotEnoughMana -= Show;
    }

    private void Show()
    {
        shownAt = Time.unscaledTime;
    }

    // Unscaled time so it still plays while a menu slows the game.
    void Update()
    {
        float t = Time.unscaledTime - shownAt;
        if (t > holdDuration + fadeDuration)
        {
            if (label.alpha != 0f)
                label.alpha = 0f;
            return;
        }

        label.alpha = t < holdDuration ? 1f : 1f - (t - holdDuration) / fadeDuration;
        float pop = Mathf.Clamp01(t / 0.12f);
        label.rectTransform.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, 1f - (1f - pop) * (1f - pop));
        float shake = t < 0.2f ? Mathf.Sin(t * 90f) * shakeAmount * (1f - t / 0.2f) : 0f;
        label.rectTransform.anchoredPosition = restPosition + new Vector2(shake, 0f);
    }
}
