using TMPro;
using UnityEngine;

// "Not enough mana" warning above the local player: pops in on a failed cast, holds, then fades.
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
    [Tooltip("World height above the player's feet, over the name tag.")]
    [SerializeField] private float heightAbovePlayer = 1.35f;

    private float shownAt = -10f;
    private Vector2 restPosition;
    private RectTransform rect;
    private RectTransform parentRect;

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

    // The canvas is an overlay, so the player's screen position is converted without a camera.
    private void FollowPlayer()
    {
        Player player = LocalPlayer.GetPlayer();
        Camera cam = Camera.main;
        Vector2 local;

        if (player == null || cam == null)
            return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, cam.WorldToScreenPoint(player.transform.position + Vector3.up * heightAbovePlayer), null, out local))
            rect.localPosition = local;
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
        rect = (RectTransform)transform;
        parentRect = (RectTransform)transform.parent;
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

    // Unscaled time so it still plays while a menu slows the game; late so it follows the camera's final position.
    void LateUpdate()
    {
        float t = Time.unscaledTime - shownAt;

        if (t <= holdDuration + fadeDuration)
        {
            FollowPlayer();
            Animate(t);
        }
        else if (label.alpha != 0f)
            label.alpha = 0f;
    }
}
