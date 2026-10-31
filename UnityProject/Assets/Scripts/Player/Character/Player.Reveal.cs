using UnityEngine;

// Local player only: opens a see-through circle in decor that hides it.
public partial class Player
{
    [Header("Occlusion Reveal")]
    [Range(0f, 4f)]
    [Tooltip("Radius (world units) of the see-through circle opened in decor that hides the local player. 0 = off.")]
    [SerializeField] private float revealRadius = 0.8f;
    [Range(0f, 1f)]
    [Tooltip("Decor opacity at the center of the circle.")]
    [SerializeField] private float revealAlpha = 0.3f;
    [Range(0f, 1f)]
    [Tooltip("Width of the circle's stippled rim, as a fraction of the radius.")]
    [SerializeField] private float revealSoftness = 0.4f;
    [Range(16f, 128f)]
    [Tooltip("Pixel grid of the stippled rim, matching the sprites' Pixels Per Unit.")]
    [SerializeField] private float revealPixelsPerUnit = 64f;
    [Range(1f, 30f)]
    [Tooltip("How fast the circle opens and closes.")]
    [SerializeField] private float revealSpeed = 6f;

    private static readonly int RevealCenterId = Shader.PropertyToID("_RevealCenter");
    private static readonly int RevealRadiusId = Shader.PropertyToID("_RevealRadius");
    private static readonly int RevealAlphaId = Shader.PropertyToID("_RevealAlpha");
    private static readonly int RevealSoftnessId = Shader.PropertyToID("_RevealSoftness");
    private static readonly int RevealPixelsPerUnitId = Shader.PropertyToID("_RevealPixelsPerUnit");

    // Local player only: feeds the circle to the reveal shader and flags every decor whose sprite covers the player from in front.
    private int RevealSortingOrder()
    {
        if (sortingLayerAuto != null)
            return sortingLayerAuto.CurrentSortingOrder;
        return spriteRenderer.sortingOrder;
    }

    private void SetRevealGlobals(Vector3 center)
    {
        Shader.SetGlobalVector(RevealCenterId, center);
        Shader.SetGlobalFloat(RevealRadiusId, revealRadius);
        Shader.SetGlobalFloat(RevealAlphaId, revealAlpha);
        Shader.SetGlobalFloat(RevealSoftnessId, revealSoftness);
        Shader.SetGlobalFloat(RevealPixelsPerUnitId, revealPixelsPerUnit);
    }

    private void LateUpdateReveal()
    {
        int layer;
        int order;
        Vector3 center;

        if (!this.IsLocallyControlled() || spriteRenderer == null)
            return;
        layer = spriteRenderer.sortingLayerID;
        order = RevealSortingOrder();
        center = spriteRenderer.bounds.center;
        SetRevealGlobals(center);
        foreach (RevealOccluder occluder in RevealOccluder.Active)
            occluder.SetTarget(revealRadius > 0f && occluder.Covers(center, layer, order), revealSpeed);
        RevealOccluder.TickAll(Time.deltaTime);
    }
}
