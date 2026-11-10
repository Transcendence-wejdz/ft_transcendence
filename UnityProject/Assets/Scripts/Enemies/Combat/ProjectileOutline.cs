using UnityEngine;

// Bright red outline of an enemy shot: flat red copies of its sprite, shifted all around it and drawn behind it.
public class ProjectileOutline
{
    private const int CopyCount = 8;
    private const float WorldThickness = 0.05f;

    private static readonly Color OutlineColor = new Color(1f, 0f, 0f, 1f);

    private readonly SpriteRenderer[] copies = new SpriteRenderer[CopyCount];

    public ProjectileOutline(SpriteRenderer source, Material material)
    {
        float thickness = WorldThickness / source.transform.lossyScale.x;

        for (int i = 0; i < CopyCount; i++)
        {
            float angle = i * Mathf.PI * 2f / CopyCount;
            GameObject copy = new GameObject("Outline");

            copy.transform.SetParent(source.transform, false);
            copy.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * thickness;
            copies[i] = copy.AddComponent<SpriteRenderer>();
            copies[i].sharedMaterial = material;
            copies[i].color = OutlineColor;
            copies[i].sortingLayerID = source.sortingLayerID;
            copies[i].sortingOrder = source.sortingOrder - 1;
        }
        SetSprite(source.sprite);
    }

    public void SetSprite(Sprite sprite)
    {
        foreach (SpriteRenderer copy in copies)
            copy.sprite = sprite;
    }
}
