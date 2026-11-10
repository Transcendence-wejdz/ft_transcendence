using System.Collections.Generic;
using UnityEngine;

// Y-sorts a sprite on the Player layer every frame from a configurable pivot.
public class SortingLayer_Auto : MonoBehaviour
{
    private const float SortPrecision = -100f;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int sortingOffset = 0;

    [Tooltip("Local-space Y-sort reference point offset from the transform origin. Drag the Scene view handle to align it with the sprite's visual base. Zero = sort on the transform's own position.")]
    [SerializeField] private Vector3 sortPivotOffset = Vector3.zero;

    [Tooltip("Projectiles and attack FX always draw over this sprite where they overlap it (low decor like bushes and rocks).")]
    [SerializeField] private bool fxDrawsOver = false;

    private static readonly List<SortingLayer_Auto> FxUnderlays = new List<SortingLayer_Auto>();

    public Vector3 SortPivotOffset
    {
        get => sortPivotOffset;
        set => sortPivotOffset = value;
    }

    public Vector3 SortPivotWorldPosition => transform.TransformPoint(sortPivotOffset);

    // Y-sort order for a world height, shared by everything sorted on the Player layer.
    public static int OrderForY(float worldY)
    {
        return Mathf.RoundToInt(worldY * SortPrecision);
    }

    // Pure, not a cached field - safe to read from another object's LateUpdate regardless of execution order.
    public int CurrentSortingOrder => OrderForY(SortPivotWorldPosition.y) + sortingOffset;

    public void SetSortingOffset(int value)
    {
        sortingOffset = value;
    }

    // Raises an FX order above every visible fxDrawsOver sprite its bounds overlap.
    public static int RaiseOverFxUnderlays(int order, Bounds fxBounds)
    {
        SpriteRenderer underlayRenderer;

        foreach (SortingLayer_Auto underlay in FxUnderlays)
        {
            underlayRenderer = underlay.spriteRenderer;
            if (underlayRenderer.isVisible && underlayRenderer.bounds.Intersects(fxBounds))
                order = Mathf.Max(order, underlay.CurrentSortingOrder + 1);
        }
        return order;
    }

    void OnEnable()
    {
        if (fxDrawsOver && spriteRenderer != null)
            FxUnderlays.Add(this);
    }

    void OnDisable()
    {
        FxUnderlays.Remove(this);
    }

    void LateUpdate()
    {
        spriteRenderer.sortingOrder = CurrentSortingOrder;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(SortPivotWorldPosition, 0.08f);
    }
}
