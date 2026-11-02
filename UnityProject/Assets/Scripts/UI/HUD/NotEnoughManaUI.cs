using TMPro;
using UnityEngine;

// "Not enough mana" warning above the ability bar: pops in on a failed cast, holds, then fades.
public class NotEnoughManaUI : MonoBehaviour
{
    private const float PopSeconds = 0.12f;
    private const float ShakeSeconds = 0.2f;
    private const float ShakeFrequency = 90f;

    [SerializeField] private TMP_Text label;
    [SerializeField] private float holdDuration = 0.6f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float popScale = 1.25f;
    [SerializeField] private float shakeAmount = 3f;

    private float shownAt = -10f;
    private Vector2 restPosition;

    private void Show()
    {
        shownAt = Time.unscaledTime;
    }

    private float AlphaAt(float t)
    {
        if (t < holdDuration)
            return 1f;
        return 1f - (t - holdDuration) / fadeDuration;
    }

    // A short horizontal shake that dies out over ShakeSeconds.
    private float ShakeAt(float t)
    {
        if (t >= ShakeSeconds)
            return 0f;
        return Mathf.Sin(t * ShakeFrequency) * shakeAmount * (1f - t / ShakeSeconds);
    }

    private void Animate(float t)
    {
        float pop = Mathf.Clamp01(t / PopSeconds);

        label.alpha = AlphaAt(t);
        label.rectTransform.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, 1f - (1f - pop) * (1f - pop));
        label.rectTransform.anchoredPosition = restPosition + new Vector2(ShakeAt(t), 0f);
    }

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

    // Unscaled time so it still plays while a menu slows the game.
    void Update()
    {
        float t = Time.unscaledTime - shownAt;

        if (t <= holdDuration + fadeDuration)
            Animate(t);
        else if (label.alpha != 0f)
            label.alpha = 0f;
    }
}
