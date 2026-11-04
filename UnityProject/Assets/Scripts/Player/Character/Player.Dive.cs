using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Dive Ultimate: the player vanishes at takeoff, untouchable and ignored by enemies, and comes down on the aimed spot, throwing enemies out of the impact.
public partial class Player
{
    private const float DiveMoveDelay = 0.1f;
    private const float DiveLandingGrace = 0.3f;
    private const float DiveLandingStep = 0.25f;

    private static readonly Collider2D[] DivePushBuffer = new Collider2D[64];

    private readonly NetworkVariable<bool> isDiving = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly List<Renderer> diveHiddenRenderers = new List<Renderer>();
    private readonly List<Canvas> diveHiddenCanvases = new List<Canvas>();

    public bool IsDiving => isDiving.Value;

    private bool IsLandable(Vector2 feet, ProceduralMapGenerator map)
    {
        Vector2 body = feet + (Center - (Vector2)transform.position);

        if (ObstacleQuery.IsBlocked(body, InnerRadius(bodyCollider)))
            return false;
        return map == null || !map.IsReady || map.IsOnMainland(body);
    }

    // Walks back from the aimed spot toward the player until the ground can be stood on; water and obstacles never catch the landing.
    private float LandingDistance(AbilityKit kit, float distance, Vector2 aim)
    {
        ProceduralMapGenerator map = FindAnyObjectByType<ProceduralMapGenerator>();
        float landing = distance;

        if (landing < 0f)
            landing = kit.FxDistance;
        while (landing > 0f && !IsLandable((Vector2)transform.position + aim * landing, map))
            landing -= DiveLandingStep;
        return Mathf.Max(0f, landing);
    }

    private void CollectEnabled<T>(List<T> list, System.Func<T, bool> isEnabled) where T : Component
    {
        foreach (T part in GetComponentsInChildren<T>())
        {
            if (isEnabled(part))
                list.Add(part);
        }
    }

    private void SetDiveHidden(bool hidden)
    {
        if (hidden)
        {
            CollectEnabled(diveHiddenRenderers, part => part.enabled);
            CollectEnabled(diveHiddenCanvases, part => part.enabled);
        }
        foreach (Renderer part in diveHiddenRenderers)
            part.enabled = !hidden;
        foreach (Canvas part in diveHiddenCanvases)
            part.enabled = !hidden;
        if (bodyCollider != null)
            bodyCollider.enabled = !hidden;
        if (hidden)
            return;
        diveHiddenRenderers.Clear();
        diveHiddenCanvases.Clear();
    }

    private void HandleDivingChanged(bool previous, bool current)
    {
        SetDiveHidden(current);
    }

    // The kit's FX origin puts the FX on the ground under the feet, which sit below the player's pivot; the player itself lands by its pivot.
    private void PlayDiveTakeoff(float chargeSize)
    {
        AbilityKit kit = Ability(AbilityType.Ultimate);
        GameObject fx;

        if (kit == null || kit.TakeoffFxPrefab == null)
            return;
        fx = Instantiate(kit.TakeoffFxPrefab, transform.position + (Vector3)FxOffset(AbilityType.Ultimate, Vector2.right, 0f), Quaternion.identity);
        fx.transform.localScale *= FxScaleMultiplier(AbilityType.Ultimate) * chargeSize;
        fx.GetComponent<IAttackFX>()?.Init(gameObject, false, kit, 1f);
    }

    [Rpc(SendTo.NotMe)]
    private void DiveTakeoffRpc(float chargeSize)
    {
        PlayDiveTakeoff(chargeSize);
    }

    // Moved early in the air, so the camera travels and other peers' smoothing plays out while the player is hidden.
    private IEnumerator DiveRoutine(AbilityKit kit, Vector2 landing)
    {
        yield return new WaitForSeconds(DiveMoveDelay);
        rb.position = landing;
        rb.linearVelocity = Vector2.zero;
        transform.position = landing;
        yield return new WaitForSeconds(Mathf.Max(0f, kit.DiveAirSeconds - DiveMoveDelay));
        isDiving.Value = false;
    }

    private void StartDive(AbilityKit kit, float distance)
    {
        float angle = AssistedAimAngle(AbilityType.Ultimate);
        Vector2 aim = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        float landing = LandingDistance(kit, distance, aim);

        invulnerableEndTime = Time.time + kit.DiveAirSeconds + DiveLandingGrace;
        PlayDiveTakeoff(castSizeMultiplier);
        if (NetworkObject.IsSpawned)
            DiveTakeoffRpc(castSizeMultiplier);
        SpawnAbilityFx(AbilityType.Ultimate, landing);
        isDiving.Value = true;
        StartCoroutine(DiveRoutine(kit, (Vector2)transform.position + aim * landing));
    }

    // Enemies are server-owned, so the impact's owner asks the server to throw them.
    public void RequestDivePush(Vector2 center, Vector2 size)
    {
        if (NetworkObject.IsSpawned)
            DivePushRpc(center, size);
        else
            ApplyDivePush(center, size);
    }

    [Rpc(SendTo.Server)]
    private void DivePushRpc(Vector2 center, Vector2 size)
    {
        ApplyDivePush(center, size);
    }

    private void ThrowFromImpact(Collider2D body, Vector2 center, AbilityKit kit)
    {
        Enemy enemy = body.GetComponent<Enemy>();
        Vector2 away;

        if (enemy == null)
            return;
        away = enemy.BodyCenter - center;
        if (away.sqrMagnitude < MinMoveInputSqr)
            away = Vector2.up;
        enemy.ApplyKnockback(away.normalized * kit.DiveKnockbackForce, kit.DiveKnockbackDuration);
    }

    private void ApplyDivePush(Vector2 center, Vector2 size)
    {
        AbilityKit kit = Ability(AbilityType.Ultimate);
        int count;

        if (kit == null)
            return;
        count = Physics2D.OverlapCapsule(center, size, CapsuleDirection2D.Horizontal, 0f, SolidFilter(EnemyLayerMask), DivePushBuffer);
        for (int i = 0; i < count; i++)
            ThrowFromImpact(DivePushBuffer[i], center, kit);
    }
}
