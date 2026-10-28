using UnityEngine;

public partial class Player
{
    [Header("Flip")]
    [SerializeField] private float flipDuration = 0.1f;

    private SpriteRenderer flipProxy;
    private bool flipTarget;
    private float flipProgress = 1f;

    // Starts a paper-flip toward the facing side, or reverses the one in progress.
    private void FlipTowards(float directionX)
    {
        if (directionX == 0f)
            return;

        bool left = directionX < 0f;
        if (flipProgress < 1f)
        {
            if (left != flipTarget)
            {
                flipTarget = left;
                flipProgress = 1f - flipProgress;
            }
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
        spriteRenderer.flipX = flipProgress < 0.5f ? !flipTarget : flipTarget;

        if (flipProgress >= 1f || !spriteRenderer.enabled)
        {
            flipProgress = 1f;
            spriteRenderer.flipX = flipTarget;
            spriteRenderer.forceRenderingOff = false;
            if (flipProxy != null)
                flipProxy.enabled = false;
            return;
        }

        if (flipProxy == null)
        {
            GameObject proxy = new GameObject("FlipProxy");
            proxy.transform.SetParent(transform, false);
            flipProxy = proxy.AddComponent<SpriteRenderer>();
        }

        flipProxy.enabled = true;
        flipProxy.gameObject.layer = gameObject.layer;
        flipProxy.sprite = spriteRenderer.sprite;
        flipProxy.flipX = spriteRenderer.flipX;
        flipProxy.color = spriteRenderer.color;
        flipProxy.sharedMaterial = spriteRenderer.sharedMaterial;
        flipProxy.sortingLayerID = spriteRenderer.sortingLayerID;
        flipProxy.sortingOrder = spriteRenderer.sortingOrder;
        flipProxy.transform.localScale = new Vector3(Mathf.Max(Mathf.Abs(Mathf.Cos(flipProgress * Mathf.PI)), 0.02f), 1f, 1f);
        spriteRenderer.forceRenderingOff = true;
    }
}
