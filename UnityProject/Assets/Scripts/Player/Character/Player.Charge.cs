using Unity.Netcode;
using UnityEngine;

// Hold to charge: the kit's charge FX plays on every peer, the player crawls, and the release deals more damage the longer it was held.
public partial class Player
{
    private AbilityKit chargingKit;
    private AbilityType chargeFxAbility;
    private ChargeFX chargeFx;
    private float chargeStartTime;
    private float chargeFxStartTime;
    private float castDamageMultiplier = 1f;
    private float castSizeMultiplier = 1f;

    private bool IsCharging => chargingKit != null;

    private static float ChargeProgress(AbilityKit kit, float startTime)
    {
        return Mathf.Clamp01((Time.time - startTime) / kit.ChargeSeconds);
    }

    private float ChargeSize()
    {
        if (!IsCharging)
            return 1f;
        return Mathf.Lerp(1f, chargingKit.ChargeSizeMultiplier, ChargeProgress(chargingKit, chargeStartTime));
    }

    // Kept for the cast the release triggers: the charge itself ends before that cast.
    private void CaptureCharge()
    {
        if (!IsCharging)
            return;
        castDamageMultiplier = Mathf.Lerp(1f, chargingKit.ChargeDamageMultiplier, ChargeProgress(chargingKit, chargeStartTime));
        castSizeMultiplier = ChargeSize();
    }

    private void ClearCapturedCharge()
    {
        castDamageMultiplier = 1f;
        castSizeMultiplier = 1f;
    }

    private void ShowChargeFx(AbilityType ability)
    {
        AbilityKit kit = Ability(ability);
        GameObject fx;

        HideChargeFx();
        if (kit == null || kit.ChargeFxPrefab == null)
            return;
        fx = Instantiate(kit.ChargeFxPrefab);
        chargeFx = fx.GetComponent<ChargeFX>();
        chargeFxAbility = ability;
        chargeFxStartTime = Time.time;
        chargeFx.Init(CharacterCatalog.FxColorOf(gameObject));
        UpdateChargeFx();
    }

    private void HideChargeFx()
    {
        if (chargeFx != null)
            Destroy(chargeFx.gameObject);
        chargeFx = null;
    }

    [Rpc(SendTo.NotMe)]
    private void ShowChargeFxRpc(AbilityType ability)
    {
        ShowChargeFx(ability);
    }

    [Rpc(SendTo.NotMe)]
    private void HideChargeFxRpc()
    {
        HideChargeFx();
    }

    private void BeginCharge(AbilityType ability, AbilityKit kit)
    {
        if (kit.ChargeFxPrefab == null)
            return;
        chargingKit = kit;
        chargeStartTime = Time.time;
        ShowChargeFx(ability);
        if (NetworkObject.IsSpawned)
            ShowChargeFxRpc(ability);
    }

    private void EndCharge()
    {
        if (!IsCharging)
            return;
        chargingKit = null;
        HideChargeFx();
        if (NetworkObject.IsSpawned)
            HideChargeFxRpc();
    }

    // Every peer poses and grows its copy from the replicated aim and its own timer, at the kit's charge FX placement.
    private void UpdateChargeFx()
    {
        AbilityKit kit = Ability(chargeFxAbility);
        Vector3 position;
        Quaternion rotation;
        float size;
        float angle;

        if (chargeFx == null || kit == null || kit.ChargeFxPrefab == null)
            return;
        size = Mathf.Lerp(1f, kit.ChargeSizeMultiplier, ChargeProgress(kit, chargeFxStartTime));
        angle = ChargeFxAngle;
        FxPose(chargeFxAbility, angle, -1f, out position, out rotation);
        position = transform.position + (Vector3)kit.ChargeFxOffset(new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)), AimIndicatorCenter);
        chargeFx.transform.SetPositionAndRotation(position, rotation);
        chargeFx.transform.localScale = kit.ChargeFxPrefab.transform.localScale * (FxScaleMultiplier(chargeFxAbility) * size);
    }
}
