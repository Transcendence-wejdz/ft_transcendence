using UnityEngine;

// Y-sorts a moving FX on the Player layer and lifts it over overlapped fxDrawsOver decor.
public class FXSortingAuto : MonoBehaviour
{
    private SpriteRenderer[] renderers;
    private int[] relativeOrders;

    // Caches each renderer's authored order relative to the lowest one, so layering inside the FX is kept.
    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        relativeOrders = new int[renderers.Length];
        int lowest = int.MaxValue;
        foreach (SpriteRenderer r in renderers)
            lowest = Mathf.Min(lowest, r.sortingOrder);
        for (int i = 0; i < renderers.Length; i++)
            relativeOrders[i] = renderers[i].sortingOrder - lowest;
    }

    // Re-sorts every frame since projectiles and slashes move.
    void LateUpdate()
    {
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        int order = SortingLayer_Auto.RaiseOverFxUnderlays(SortingLayer_Auto.OrderForY(transform.position.y), bounds);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = order + relativeOrders[i];
    }
}
