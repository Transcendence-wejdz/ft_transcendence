using UnityEngine;
using UnityEngine.UI;

// Red screen-edge vignette pulse, built at runtime.
public class ScreenEdgeFlash : MonoBehaviour
{
    private const int TextureSize = 128;
    private const int CanvasSortingOrder = 500;
    private const float MinPulseDuration = 0.05f;
    private const float VignetteInner = 0.55f;
    private const float VignetteSpan = 0.85f;

    private Image image;
    private float intensity;
    private float fadeSpeed = 3f;

    // Alpha grows from the inner radius outward, squared for a soft rim.
    private static float VignetteAlpha(int x, int y)
    {
        float dx = (x / (TextureSize - 1f)) * 2f - 1f;
        float dy = (y / (TextureSize - 1f)) * 2f - 1f;
        float alpha = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - VignetteInner) / VignetteSpan);

        return alpha * alpha;
    }

    private static Sprite BuildVignetteSprite()
    {
        Texture2D tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, VignetteAlpha(x, y)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f));
    }

    private static GameObject CreateCanvasRoot()
    {
        GameObject root = new GameObject("ScreenEdgeFlash");
        Canvas canvas = root.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;
        return root;
    }

    private static Image CreateVignette(Transform parent)
    {
        GameObject imageGO = new GameObject("Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = (RectTransform)imageGO.transform;
        Image vignette = imageGO.GetComponent<Image>();

        imageGO.transform.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        vignette.raycastTarget = false;
        vignette.sprite = BuildVignetteSprite();
        vignette.color = new Color(1f, 0.05f, 0.05f, 0f);
        return vignette;
    }

    public static ScreenEdgeFlash Create()
    {
        GameObject root = CreateCanvasRoot();
        ScreenEdgeFlash flash = root.AddComponent<ScreenEdgeFlash>();

        flash.image = CreateVignette(root.transform);
        return flash;
    }

    public void Pulse(float strength, float duration)
    {
        intensity = Mathf.Max(intensity, strength);
        fadeSpeed = strength / Mathf.Max(MinPulseDuration, duration);
    }

    void Update()
    {
        Color c;

        if (image == null)
            return;
        intensity = Mathf.MoveTowards(intensity, 0f, fadeSpeed * Time.unscaledDeltaTime);
        c = image.color;
        c.a = intensity;
        image.color = c;
    }
}
