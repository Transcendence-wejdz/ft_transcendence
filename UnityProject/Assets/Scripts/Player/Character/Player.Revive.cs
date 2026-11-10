using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Revive feedback on the downed player: hidden prompt while ridden and the revive bar filling over the ride.
public partial class Player
{
    private const float AutoReviveSeconds = 5f;

    private float autoReviveStartTime = -1f;

    private bool IsAutoReviving => autoReviveStartTime >= 0f;

    // Timed on every peer for the bar, from the pig form rather than a 0 health that may just be a new spawn; Revive only acts on the owner, who alone writes its health.
    private void UpdateAutoRevive()
    {
        if (!AutoReviveActive || !isPig)
        {
            autoReviveStartTime = -1f;
            return;
        }
        if (!IsAutoReviving)
            autoReviveStartTime = Time.time;
        if (Time.time >= autoReviveStartTime + AutoReviveSeconds)
            Revive();
    }

    // The prompt follows the pig form, not a 0 health that may just be a new spawn; hidden while ridden.
    private void UpdateMountInteractVisibility()
    {
        bool shouldShow;

        if (mountInteractRoot == null)
            return;
        shouldShow = isPig && !HasRider();
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
    private void ShowReviveBar(bool visible)
    {
        Canvas canvas = reviveProgress.GetComponentInParent<Canvas>(true);
        GameObject root = reviveProgress.gameObject;

        if (canvas != null)
            root = canvas.gameObject;
        if (root.activeSelf != visible)
            root.SetActive(visible);
        if (nameTag != null)
            nameTag.SetHidden(visible || arrivalHold != null);
        if (visible)
            BrushFill.Set(reviveProgress, ReviveFraction);
    }

    // Revive bar filled over mountDurationSeconds while someone rides, timed locally from the replicated parenting; an auto revive fills it over its own, shorter time.
    private void UpdateReviveProgress()
    {
        bool riding = IsDead && HasRider();

        TrackRideTime(riding);
        UpdateAutoRevive();
        if (IsAutoReviving)
            ReviveFraction = Mathf.Clamp01((Time.time - autoReviveStartTime) / AutoReviveSeconds);
        if (reviveProgress != null)
            ShowReviveBar(riding || IsAutoReviving);
    }
}
