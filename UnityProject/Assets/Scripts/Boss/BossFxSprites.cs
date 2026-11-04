using UnityEngine;

// Sprites built at runtime for the boss effects, so the glow needs no imported texture.
public static class BossFxSprites
{
    private const int GlowTextureSize = 64;

    private static Sprite glow;
    private static Sprite square;

    public static Sprite Glow
    {
        get
        {
            if (glow == null)
                glow = CreateGlow();
            return glow;
        }
    }

    public static Sprite Square
    {
        get
        {
            Texture2D white = Texture2D.whiteTexture;

            if (square == null)
                square = Sprite.Create(white, new Rect(0f, 0f, white.width, white.height),
                    new Vector2(0.5f, 0.5f), white.width, 0, SpriteMeshType.FullRect);
            return square;
        }
    }

    // Soft white disc, one world unit wide, fading out toward its edge.
    private static Sprite CreateGlow()
    {
        Texture2D texture = new Texture2D(GlowTextureSize, GlowTextureSize, TextureFormat.RGBA32, false);
        float half = (GlowTextureSize - 1) * 0.5f;
        float distance;

        for (int y = 0; y < GlowTextureSize; y++)
        {
            for (int x = 0; x < GlowTextureSize; x++)
            {
                distance = Mathf.Clamp01(new Vector2(x - half, y - half).magnitude / half);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, (1f - distance) * (1f - distance)));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, GlowTextureSize, GlowTextureSize),
            new Vector2(0.5f, 0.5f), GlowTextureSize, 0, SpriteMeshType.FullRect);
    }
}
