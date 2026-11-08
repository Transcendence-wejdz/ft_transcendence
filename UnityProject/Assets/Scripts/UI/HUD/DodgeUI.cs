using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Dodge charges under the local player's feet, one segment per charge; the recharging one unwinds, and the bar fades out once all are full.
public class DodgeUI : MonoBehaviour
{
    private const float FullFadeSpeed = 3f;
    private const float RefillPopSeconds = 0.15f;

    [SerializeField] private GameObject pipTemplate;
    [Tooltip("World distance below the player's pivot, under the feet.")]
    [SerializeField] private float depthBelowPlayer = 1.12f;
    [Tooltip("Seconds the full bar stays visible before fading out.")]
    [SerializeField] private float fullHoldSeconds = 0.6f;
    [SerializeField] private float refillPopScale = 1.3f;
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.8f);

    private readonly List<Image> overlays = new List<Image>();
    private RectTransform rect;
    private RectTransform parentRect;
    private CanvasGroup group;
    private int shownCharges;
    private float lastNotFullTime = -10f;
    private float lastRefillTime = -10f;

    // Pips follow the class's charge count, which changes when the player switches class.
    private void RebuildPips(int count)
    {
        GameObject pip;

        foreach (Image overlay in overlays)
            Destroy(overlay.transform.parent.gameObject);
        overlays.Clear();
        for (int i = 0; i < count; i++)
        {
            pip = Instantiate(pipTemplate, pipTemplate.transform.parent);
            pip.SetActive(true);
            pip.AddComponent<Outline>().effectColor = outlineColor;
            overlays.Add(pip.transform.GetChild(0).GetComponent<Image>());
        }
    }

    // Spent pips are fully covered, ready ones clear, and the one recharging unwinds.
    private static float OverlayFill(int pip, int charges, float recharge)
    {
        if (pip < charges)
            return 0f;
        if (pip == charges)
            return 1f - recharge;
        return 1f;
    }

    private static int MaxCharges(Player player)
    {
        if (player == null)
            return 0;
        return player.MaxDodgeCharges;
    }

    // The canvas is an overlay, so the player's screen position is converted without a camera.
    private void FollowPlayer(Player player)
    {
        Camera cam = Camera.main;
        Vector2 local;

        if (cam == null)
            return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, cam.WorldToScreenPoint(player.transform.position + Vector3.down * depthBelowPlayer), null, out local))
            rect.localPosition = local;
    }

    private void TrackCharges(Player player, int max)
    {
        int charges = player.DodgeCharges;

        if (charges > shownCharges)
            lastRefillTime = Time.unscaledTime;
        shownCharges = charges;
        if (charges < max)
            lastNotFullTime = Time.unscaledTime;
    }

    // Visible while any charge is missing, then fades; a charge coming back gives a short pop.
    private void UpdateLook()
    {
        float target = 0f;
        float pop = Mathf.Clamp01((Time.unscaledTime - lastRefillTime) / RefillPopSeconds);

        if (Time.unscaledTime - lastNotFullTime < fullHoldSeconds)
            target = 1f;
        group.alpha = Mathf.MoveTowards(group.alpha, target, FullFadeSpeed * Time.unscaledDeltaTime);
        if (target > 0f)
            group.alpha = 1f;
        rect.localScale = Vector3.one * Mathf.Lerp(refillPopScale, 1f, pop);
    }

    void Awake()
    {
        rect = (RectTransform)transform;
        parentRect = (RectTransform)transform.parent;
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        pipTemplate.SetActive(false);
    }

    // Late so the bar follows the camera's final position.
    void LateUpdate()
    {
        Player player = LocalPlayer.GetPlayer();
        int max = MaxCharges(player);

        if (overlays.Count != max)
            RebuildPips(max);
        if (max == 0)
        {
            group.alpha = 0f;
            return;
        }
        for (int i = 0; i < overlays.Count; i++)
            overlays[i].fillAmount = OverlayFill(i, player.DodgeCharges, player.DodgeRecharge01);
        FollowPlayer(player);
        TrackCharges(player, max);
        UpdateLook();
    }
}
