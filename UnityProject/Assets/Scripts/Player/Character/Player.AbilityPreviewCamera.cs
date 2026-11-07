using UnityEngine;

// Customization preview camera: frames what a spell drew so far, a projectile's fixed frame, or goes back to the character.
public partial class Player
{
    private const float PreviewFramingMargin = 1.15f;
    private const float PreviewMinOrthoSize = 1f;
    private const float PreviewCameraSpeed = 10f;

    private bool hasPreviewCameraBase;
    private float previewBaseOrthoSize;
    private Vector3 previewBaseCameraPosition;
    private Bounds previewFxBounds;
    private bool hasPreviewFxBounds;
    private bool hasPreviewFixedFrame;
    private Vector3 previewFixedCenter;

    private void GrowPreviewBounds()
    {
        foreach (GameObject fx in previewFx)
        {
            if (fx == null)
                continue;
            foreach (SpriteRenderer spriteRenderer in fx.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (spriteRenderer.sprite == null)
                    continue;
                if (hasPreviewFxBounds)
                    previewFxBounds.Encapsulate(spriteRenderer.bounds);
                else
                    previewFxBounds = spriteRenderer.bounds;
                hasPreviewFxBounds = true;
            }
        }
    }

    private void MovePreviewCamera(Vector3 position, float orthoSize)
    {
        float blend = Mathf.Clamp01(PreviewCameraSpeed * Time.unscaledDeltaTime);

        position.z = previewCamera.transform.position.z;
        previewCamera.transform.position = Vector3.Lerp(previewCamera.transform.position, position, blend);
        previewCamera.orthographicSize = Mathf.Lerp(previewCamera.orthographicSize, orthoSize, blend);
    }

    // Called every frame while the panel is open: frames everything the spell drew so far, or goes back to the character.
    public void TickPreviewCamera()
    {
        float orthoSize;

        if (previewCamera == null)
            return;
        if (!hasPreviewCameraBase)
        {
            hasPreviewCameraBase = true;
            previewBaseOrthoSize = previewCamera.orthographicSize;
            previewBaseCameraPosition = previewCamera.transform.localPosition;
        }
        TickPreviewCharge();
        if (hasPreviewFixedFrame)
        {
            MovePreviewCamera(previewFixedCenter, previewBaseOrthoSize);
            return;
        }
        GrowPreviewBounds();
        if (!hasPreviewFxBounds)
        {
            MovePreviewCamera(transform.TransformPoint(previewBaseCameraPosition), previewBaseOrthoSize);
            return;
        }
        orthoSize = Mathf.Max(previewFxBounds.extents.y, previewFxBounds.extents.x / previewCamera.aspect);
        MovePreviewCamera(previewFxBounds.center, Mathf.Max(PreviewMinOrthoSize, orthoSize * PreviewFramingMargin));
    }

    public void ResetPreviewCamera()
    {
        if (previewCamera == null || !hasPreviewCameraBase)
            return;
        previewCamera.orthographicSize = previewBaseOrthoSize;
        previewCamera.transform.localPosition = previewBaseCameraPosition;
    }
}
