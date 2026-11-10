using UnityEngine;

// Visual-only frozen copy of the offline player's visible sprites, shown while a session is created without a loading screen; it has no NetworkObject, so Netcode never spawns it.
public static class OfflinePlayerStandIn
{
    private static void CopySprite(SpriteRenderer source, Transform root)
    {
        GameObject copy = new GameObject(source.name);
        SpriteRenderer renderer;

        copy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        copy.transform.localScale = source.transform.lossyScale;
        copy.transform.SetParent(root, worldPositionStays: true);
        renderer = copy.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.color = source.color;
        renderer.flipX = source.flipX;
        renderer.flipY = source.flipY;
        renderer.drawMode = source.drawMode;
        renderer.size = source.size;
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
        renderer.maskInteraction = source.maskInteraction;
    }

    public static GameObject Create(GameObject player)
    {
        GameObject root = new GameObject("OfflinePlayerStandIn");

        root.transform.SetPositionAndRotation(player.transform.position, player.transform.rotation);
        foreach (SpriteRenderer source in player.GetComponentsInChildren<SpriteRenderer>())
        {
            if (source.enabled && source.sprite != null)
                CopySprite(source, root.transform);
        }
        return root;
    }
}
