using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Revive feedback on the downed player: hidden prompt while ridden and the revive bar filling over the ride.
public partial class Player
{
    // Hides the revive prompt while someone rides, read from the replicated transform.parent.
    private void UpdateMountInteractVisibility()
    {
        bool shouldShow;

        if (mountInteractRoot == null || !IsDead)
            return;
        shouldShow = !HasRider();
        if (mountInteractRoot.activeSelf != shouldShow)
            mountInteractRoot.SetActive(shouldShow);
    }

    private void TrackRideTime(bool riding)
    {
        if (!riding)
            riderSince = -1f;
        else if (riderSince < 0f)
        {
            riderSince = Time.time - pendingRideOffset;
            pendingRideOffset = 0f;
        }
        if (riding)
            ReviveFraction = Mathf.Clamp01((Time.time - riderSince) / mountDurationSeconds);
        else
            ReviveFraction = -1f;
    }

    // The bar shows on its whole canvas, in place of the name tag.
    private void ShowReviveBar(bool riding)
    {
        Canvas canvas = reviveProgress.GetComponentInParent<Canvas>(true);
        GameObject root = reviveProgress.gameObject;

        if (canvas != null)
            root = canvas.gameObject;
        if (root.activeSelf != riding)
            root.SetActive(riding);
        if (nameTag != null)
            nameTag.SetHidden(riding);
        if (riding)
            BrushFill.Set(reviveProgress, ReviveFraction);
    }

    // Revive bar filled over mountDurationSeconds while someone rides, timed locally from the replicated parenting.
    private void UpdateReviveProgress()
    {
        bool riding = IsDead && HasRider();

        TrackRideTime(riding);
        if (reviveProgress != null)
            ShowReviveBar(riding);
    }
}
