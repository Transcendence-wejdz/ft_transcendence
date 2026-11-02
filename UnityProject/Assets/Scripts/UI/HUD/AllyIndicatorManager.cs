using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Screen-edge arrows pointing toward allies currently outside the local camera's view.
public class AllyIndicatorManager : MonoBehaviour
{
    private class IndicatorState
    {
        public RectTransform Rect;
        public Vector2 SmoothedDir;
        public bool HasDir;
    }

    private const float EdgeMargin = 40f;
    private const float DirectionSmoothing = 8f;
    private const float MinAxis = 0.0001f;
    private const float ArrowUpOffset = 90f;

    // Indexed by ColorIndex: Black, Blue, Purple, Red, Yellow.
    private static readonly Color[] ColorsByColorIndex =
    {
        new Color(0.15f, 0.15f, 0.15f),
        new Color(0.25f, 0.45f, 0.95f),
        new Color(0.6f, 0.3f, 0.85f),
        new Color(0.85f, 0.25f, 0.25f),
        new Color(0.95f, 0.8f, 0.2f),
    };

    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private float arrowSize = 32f;

    private RectTransform canvasRect;
    private readonly Dictionary<Player, IndicatorState> indicators = new Dictionary<Player, IndicatorState>();

    private static bool IsOnScreen(Vector3 viewportPoint)
    {
        if (viewportPoint.z < 0f)
            return false;
        if (viewportPoint.x <= 0f || viewportPoint.x >= 1f)
            return false;
        return viewportPoint.y > 0f && viewportPoint.y < 1f;
    }

    // From the screen centre toward the ally, flipped when it is behind the camera.
    private static Vector2 RawDirection(Vector3 viewportPoint)
    {
        Vector2 rawDir = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);

        if (viewportPoint.z < 0f)
            rawDir = -rawDir;
        if (rawDir.sqrMagnitude < MinAxis)
            rawDir = Vector2.up;
        return rawDir.normalized;
    }

    // Smooths the direction itself (not just the final position), since raw per-frame jitter near an edge flips which axis the clamp binds to.
    private static Vector2 SmoothDirection(IndicatorState indicator, Vector2 rawDir)
    {
        float t;

        if (!indicator.HasDir)
        {
            indicator.SmoothedDir = rawDir;
            indicator.HasDir = true;
            return rawDir;
        }
        t = 1f - Mathf.Exp(-DirectionSmoothing * Time.deltaTime);
        indicator.SmoothedDir = Vector2.Lerp(indicator.SmoothedDir, rawDir, t).normalized;
        return indicator.SmoothedDir;
    }

    // Pinned to the screen edge along the direction, pointing outward.
    private static void PlaceArrow(RectTransform rect, Vector2 dir, Vector2 halfSize)
    {
        float scaleX = halfSize.x / Mathf.Max(Mathf.Abs(dir.x), MinAxis);
        float scaleY = halfSize.y / Mathf.Max(Mathf.Abs(dir.y), MinAxis);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - ArrowUpOffset;

        rect.anchoredPosition = dir * Mathf.Min(scaleX, scaleY);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private static void UpdateIndicator(Camera cam, Transform ally, IndicatorState indicator, Vector2 halfSize)
    {
        Vector3 viewportPoint = cam.WorldToViewportPoint(ally.position);
        bool onScreen = IsOnScreen(viewportPoint);

        if (indicator.Rect.gameObject.activeSelf == onScreen)
            indicator.Rect.gameObject.SetActive(!onScreen);
        if (onScreen)
        {
            indicator.HasDir = false;
            return;
        }
        PlaceArrow(indicator.Rect, SmoothDirection(indicator, RawDirection(viewportPoint)), halfSize);
    }

    private static Color ArrowColor(Player player)
    {
        return ColorsByColorIndex[Mathf.Clamp(player.ColorIndex, 0, ColorsByColorIndex.Length - 1)];
    }

    private RectTransform CreateArrow(Player player)
    {
        GameObject go = new GameObject("AllyIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        Image image = go.GetComponent<Image>();

        rect.SetParent(canvasRect, false);
        rect.sizeDelta = new Vector2(arrowSize, arrowSize);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        image.sprite = arrowSprite;
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.color = ArrowColor(player);
        go.SetActive(false);
        return rect;
    }

    private void HandleRegistered(Player player)
    {
        if (player == null || player.IsLocallyControlled() || indicators.ContainsKey(player))
            return;
        indicators.Add(player, new IndicatorState { Rect = CreateArrow(player) });
    }

    private void HandleUnregistered(Player player)
    {
        IndicatorState indicator;

        if (player == null || !indicators.TryGetValue(player, out indicator))
            return;
        if (indicator.Rect != null)
            Destroy(indicator.Rect.gameObject);
        indicators.Remove(player);
    }

    private void ClearIndicators()
    {
        foreach (IndicatorState indicator in indicators.Values)
        {
            if (indicator.Rect != null)
                Destroy(indicator.Rect.gameObject);
        }
        indicators.Clear();
    }

    private void Awake()
    {
        canvasRect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        Player.OnPlayerRegistered += HandleRegistered;
        Player.OnPlayerUnregistered += HandleUnregistered;
        foreach (Player player in Player.AllActiveInstances)
            HandleRegistered(player);
    }

    private void OnDisable()
    {
        Player.OnPlayerRegistered -= HandleRegistered;
        Player.OnPlayerUnregistered -= HandleUnregistered;
        ClearIndicators();
    }

    private void Update()
    {
        Camera cam = Camera.main;
        Vector2 halfSize;

        if (cam == null)
            return;
        halfSize = canvasRect.rect.size * 0.5f - Vector2.one * EdgeMargin;
        foreach (KeyValuePair<Player, IndicatorState> pair in indicators)
        {
            if (pair.Key != null && pair.Value.Rect != null)
                UpdateIndicator(cam, pair.Key.transform, pair.Value, halfSize);
        }
    }
}
