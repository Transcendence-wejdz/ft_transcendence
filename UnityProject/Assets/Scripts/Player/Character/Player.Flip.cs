using UnityEngine;

// Paper-flip of the sprite when the player turns around, drawn by a squashed proxy.
public partial class Player
{
    private const float MinFlipWidth = 0.02f;

    [Header("Flip")]
    [SerializeField] private float flipDuration = 0.1f;

    private SpriteRenderer flipProxy;
    private bool flipTarget;
    private float flipProgress = 1f;

    // Turning back mid-flip replays the flip from where it is.
    private void ReverseFlip(bool left)
    {
        if (left == flipTarget)
            return;
        flipTarget = left;
        flipProgress = 1f - flipProgress;
    }

    private void EndFlip()
    {
        flipProgress = 1f;
        spriteRenderer.flipX = flipTarget;
        spriteRenderer.forceRenderingOff = false;
        if (flipProxy != null)
            flipProxy.enabled = false;
    }

    private void EnsureFlipProxy()
    {
        GameObject proxy;

        if (flipProxy != null)
            return;
        proxy = new GameObject("FlipProxy");
        proxy.transform.SetParent(transform, false);
        flipProxy = proxy.AddComponent<SpriteRenderer>();
    }

    // The real sprite flips sides halfway, when the proxy is edge-on.
    private void SetHalfwaySide()
    {
        if (flipProgress < 0.5f)
            spriteRenderer.flipX = !flipTarget;
        else
            spriteRenderer.flipX = flipTarget;
    }

    private void DrawFlipProxy()
    {
        float width = Mathf.Max(Mathf.Abs(Mathf.Cos(flipProgress * Mathf.PI)), MinFlipWidth);

        EnsureFlipProxy();
        flipProxy.enabled = true;
        flipProxy.gameObject.layer = gameObject.layer;
        flipProxy.sprite = spriteRenderer.sprite;
        flipProxy.flipX = spriteRenderer.flipX;
        flipProxy.color = spriteRenderer.color;
        flipProxy.sharedMaterial = spriteRenderer.sharedMaterial;
        flipProxy.sortingLayerID = spriteRenderer.sortingLayerID;
        flipProxy.sortingOrder = spriteRenderer.sortingOrder;
        flipProxy.transform.localScale = new Vector3(width, 1f, 1f);
        spriteRenderer.forceRenderingOff = true;
    }

    // Starts a paper-flip toward the facing side, or reverses the one in progress.
    private void FlipTowards(float directionX)
    {
        bool left = directionX < 0f;

        if (directionX == 0f)
            return;
        if (flipProgress < 1f)
        {
            ReverseFlip(left);
            return;
        }
        if (left == spriteRenderer.flipX)
            return;
        flipTarget = left;
        if (flipDuration <= 0f || !spriteRenderer.enabled)
            spriteRenderer.flipX = left;
        else
            flipProgress = 0f;
    }

    // Hides the real sprite and draws a proxy squashed on X, swapping sides when it is edge-on.
    private void LateUpdateFlip()
    {
        if (flipProgress >= 1f)
            return;
        flipProgress = Mathf.Min(1f, flipProgress + Time.deltaTime / flipDuration);
        SetHalfwaySide();
        if (flipProgress >= 1f || !spriteRenderer.enabled)
            EndFlip();
        else
            DrawFlipProxy();
    }
}
