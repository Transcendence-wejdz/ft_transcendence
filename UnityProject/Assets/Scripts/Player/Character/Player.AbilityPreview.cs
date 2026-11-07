using System.Collections.Generic;
using UnityEngine;

// Customization preview: shows a spell alone, its charge first when it has one, centered by the camera, without hitbox, cost, cooldown or mirrored FX; a spell without FX plays on the character instead.
public partial class Player
{
    private const string PreviewLayerName = "CharacterPreview";
    private const string PreviewOnlyLayerName = "PreviewLight";
    private const float PreviewProjectileLead = 0.7f;

    private readonly List<GameObject> previewFx = new List<GameObject>();
    private bool isPreviewCharging;
    private float previewChargeEndTime;

    private float ChargeFxAngle
    {
        get
        {
            if (isPreviewCharging)
                return 0f;
            return AimAngleDegrees;
        }
    }

    // Only the preview camera renders this layer, so neither the FX nor its light reach the Lobby.
    private static void MoveToPreviewLayer(GameObject fx)
    {
        int layer = LayerMask.NameToLayer(PreviewOnlyLayerName);

        foreach (Transform child in fx.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    // A kit whose charge prefab has no ChargeFX can't play a charge.
    private static bool HasRealCharge(AbilityKit kit)
    {
        return kit.ChargeFxPrefab != null && kit.ChargeFxPrefab.GetComponent<ChargeFX>() != null;
    }

    private static float FullChargeSize(AbilityKit kit)
    {
        if (!HasRealCharge(kit))
            return 1f;
        return kit.ChargeSizeMultiplier;
    }

    private void PlayPreviewAnimation(AbilityType ability, AbilityKit kit)
    {
        if (ability == AbilityType.Special)
            PlayAbilityAnimation(SpecialHash, SpecialAnimSpeedHash, kit.AnimSpeed);
        else if (ability == AbilityType.Ultimate)
            PlayAbilityAnimation(UltimateHash, UltimateAnimSpeedHash, kit.AnimSpeed);
        else
            PlayAbilityAnimation(AttackHash, AttackAnimSpeedHash, kit.AnimSpeed);
    }

    private void AddPreviewFx(GameObject fx)
    {
        gameObject.layer = layerBeforePreview;
        MoveToPreviewLayer(fx);
        previewFx.Add(fx);
    }

    // A projectile crosses a fixed frame at the idle zoom, since framing its whole flight would zoom out.
    private void FramePreviewProjectile(GameObject fx)
    {
        hasPreviewFixedFrame = true;
        previewFixedCenter = fx.transform.position + Vector3.right * (previewBaseOrthoSize * previewCamera.aspect * PreviewProjectileLead);
    }

    private void SpawnPreviewShot(AbilityType ability, AbilityKit kit)
    {
        GameObject fx = SpawnLocalFx(0f, false, ability, -1f, FullChargeSize(kit));
        ShieldHoldFX shield;

        if (fx == null)
        {
            PlayPreviewAnimation(ability, kit);
            return;
        }
        shield = fx.GetComponent<ShieldHoldFX>();
        if (shield != null)
            shield.RemoveHealthBar();
        AddPreviewFx(fx);
        if (kit.Kind == AbilityKind.Projectile)
            FramePreviewProjectile(fx);
    }

    // Returns how long the charge lasts before the shot, so the next loop waits for it.
    public float PlayAbilityPreview(AbilityType ability)
    {
        AbilityKit kit = Ability(ability);

        if (kit == null || !this.IsLocallyControlled())
            return 0f;
        if (!HasRealCharge(kit))
        {
            SpawnPreviewShot(ability, kit);
            return 0f;
        }
        isPreviewCharging = true;
        previewChargeEndTime = Time.time + kit.ChargeSeconds;
        ShowChargeFx(ability);
        AddPreviewFx(chargeFx.gameObject);
        return kit.ChargeSeconds;
    }

    private void TickPreviewCharge()
    {
        if (!isPreviewCharging || Time.time < previewChargeEndTime)
            return;
        isPreviewCharging = false;
        HideChargeFx();
        SpawnPreviewShot(chargeFxAbility, Ability(chargeFxAbility));
    }

    // The character only comes back while the camera is on: the panel restores its layer itself when closing.
    public void ClearAbilityPreview()
    {
        if (isPreviewCharging)
            HideChargeFx();
        isPreviewCharging = false;
        foreach (GameObject fx in previewFx)
        {
            if (fx != null)
                Destroy(fx);
        }
        previewFx.Clear();
        hasPreviewFxBounds = false;
        hasPreviewFixedFrame = false;
        if (previewCamera != null && previewCamera.gameObject.activeSelf)
            gameObject.layer = LayerMask.NameToLayer(PreviewLayerName);
    }
}
