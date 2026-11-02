using UnityEngine;

// Crosshair sprites drawn at runtime from signed distance shapes, shared with the settings icons and preview.
public partial class CursorManager
{
    private const int CrosshairTextureSize = 64;
    private const float CrosshairOutlineWidth = 2f;
    private const float CrossArmOffset = 14f;
    private const float CrossArmLength = 8f;
    private const float CrossArmWidth = 2f;
    private const float RingRadius = 18f;
    private const float RingHalfWidth = 2f;
    private const float RingDotRadius = 3f;
    private const float DotRadius = 6f;

    private static readonly Color CrosshairOutlineColor = new Color(0f, 0f, 0f, 0.75f);

    private static Sprite[] crosshairFills;
    private static Sprite[] crosshairOutlines;

    public static Color CrosshairOutlineTint => CrosshairOutlineColor;

    private static float BoxDistance(Vector2 point, Vector2 halfSize)
    {
        Vector2 outside = new Vector2(Mathf.Abs(point.x) - halfSize.x, Mathf.Abs(point.y) - halfSize.y);
        Vector2 clamped = new Vector2(Mathf.Max(outside.x, 0f), Mathf.Max(outside.y, 0f));

        return clamped.magnitude + Mathf.Min(Mathf.Max(outside.x, outside.y), 0f);
    }

    // Four arms around a center gap.
    private static float CrossDistance(Vector2 point)
    {
        Vector2 horizontalArm = new Vector2(CrossArmLength, CrossArmWidth);
        Vector2 verticalArm = new Vector2(CrossArmWidth, CrossArmLength);
        float horizontal = Mathf.Min(BoxDistance(point - new Vector2(CrossArmOffset, 0f), horizontalArm), BoxDistance(point + new Vector2(CrossArmOffset, 0f), horizontalArm));
        float vertical = Mathf.Min(BoxDistance(point - new Vector2(0f, CrossArmOffset), verticalArm), BoxDistance(point + new Vector2(0f, CrossArmOffset), verticalArm));

        return Mathf.Min(horizontal, vertical);
    }

    private static float RingDistance(Vector2 point)
    {
        float ring = Mathf.Abs(point.magnitude - RingRadius) - RingHalfWidth;

        return Mathf.Min(ring, point.magnitude - RingDotRadius);
    }

    // Signed distance in pixels from the shape's edge (negative inside): 0 = cross with a center gap, 1 = ring with a center dot, 2 = dot.
    private static float ShapeDistance(int style, Vector2 point)
    {
        if (style == 0)
            return CrossDistance(point);
        if (style == 1)
            return RingDistance(point);
        return point.magnitude - DotRadius;
    }

    private static Color32 PixelAt(int style, int x, int y, float grow)
    {
        float center = CrosshairTextureSize * 0.5f;
        Vector2 point = new Vector2(x + 0.5f - center, y + 0.5f - center);
        float alpha = Mathf.Clamp01(0.5f - (ShapeDistance(style, point) - grow));

        return new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
    }

    private static Sprite DrawSprite(int style, float grow)
    {
        Texture2D texture = new Texture2D(CrosshairTextureSize, CrosshairTextureSize, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[CrosshairTextureSize * CrosshairTextureSize];

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < CrosshairTextureSize; y++)
        {
            for (int x = 0; x < CrosshairTextureSize; x++)
                pixels[y * CrosshairTextureSize + x] = PixelAt(style, x, y, grow);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, CrosshairTextureSize, CrosshairTextureSize), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void DrawAllCrosshairs()
    {
        int count = GameSettings.CursorStyleNames.Length;

        crosshairFills = new Sprite[count];
        crosshairOutlines = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            crosshairFills[i] = DrawSprite(i, 0f);
            crosshairOutlines[i] = DrawSprite(i, CrosshairOutlineWidth);
        }
    }

    public static Color CrosshairColor()
    {
        return GameSettings.CursorColorValues[GameSettings.CursorColor];
    }

    // White fill (to tint) or dark outline sprite of a crosshair style, drawn once and shared with the settings icons and preview.
    public static Sprite CrosshairSprite(int style, bool outline)
    {
        if (crosshairFills == null)
            DrawAllCrosshairs();
        if (outline)
            return crosshairOutlines[style];
        return crosshairFills[style];
    }
}
