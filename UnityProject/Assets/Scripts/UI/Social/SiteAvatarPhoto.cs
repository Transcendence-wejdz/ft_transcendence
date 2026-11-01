using UnityEngine;
using UnityEngine.UI;

// Covers a name-based avatar with the player's site picture once it has loaded, cropped to a centered square.
public class SiteAvatarPhoto : MonoBehaviour
{
    [SerializeField] private RawImage photo;

    private string requestedId;

    private static Rect SquareCrop(Texture2D texture)
    {
        float width = Mathf.Min(1f, (float)texture.height / texture.width);
        float height = Mathf.Min(1f, (float)texture.width / texture.height);

        return new Rect((1f - width) / 2f, (1f - height) / 2f, width, height);
    }

    private void Show(string unityId, Texture2D texture)
    {
        if (this == null || requestedId != unityId || texture == null)
            return;
        photo.texture = texture;
        photo.uvRect = SquareCrop(texture);
        photo.gameObject.SetActive(true);
    }

    public void Load(string unityId)
    {
        requestedId = unityId;
        photo.gameObject.SetActive(false);
        SiteAvatarCache.Request(unityId, texture => Show(unityId, texture));
    }
}
