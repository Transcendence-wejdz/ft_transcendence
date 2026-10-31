using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Hits stopped by a raised shield: projectiles and attacks meeting it from the front, its health taking the damage.
public partial class Player
{
    private void ReduceShieldHealth(float amount)
    {
        shieldHealth.Value = Mathf.Max(0f, shieldHealth.Value - amount);
        if (shieldHealth.Value <= 0f && isShieldActive.Value)
            EndShield();
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
    private void ApplyShieldDamageRpc(float amount)
    {
        ReduceShieldHealth(amount);
    }

    private static ShieldHoldFX ActiveShieldOn(Collider2D collider)
    {
        ShieldHoldFX shield = collider.GetComponent<ShieldHoldFX>();

        if (shield == null || shield.Owner == null || !shield.Owner.IsShieldActive)
            return null;
        return shield;
    }

    // A shield only stops what comes at its front, so its owner's own shots fly out through it.
    private bool FacesAgainst(Vector2 travel)
    {
        float radians = shieldFacingAngle.Value * Mathf.Deg2Rad;

        return Vector2.Dot(travel, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians))) < 0f;
    }

    // Raised shield walls the from-to line crosses, on the ShieldWall layer only (queries ignore the collision matrix).
    private static int LinecastShieldWalls(Vector2 from, Vector2 to)
    {
        ContactFilter2D filter = new ContactFilter2D { useLayerMask = true, layerMask = ShieldWallMask, useTriggers = true };

        return Physics2D.Linecast(from, to, filter, ShieldLineBuffer);
    }

    // Any peer: the first raised shield a projectile moving from -> to runs into from the front, null if none.
    public static ShieldHoldFX FindBlockingShield(Vector2 from, Vector2 to)
    {
        Vector2 travel = to - from;
        int count;
        ShieldHoldFX shield;

        if (travel.sqrMagnitude < MinTravelSqr)
            return null;
        count = LinecastShieldWalls(from, to);
        for (int i = 0; i < count; i++)
        {
            shield = ActiveShieldOn(ShieldLineBuffer[i].collider);
            if (shield != null && shield.Owner.FacesAgainst(travel))
                return shield;
        }
        return null;
    }

    // Any peer: a hit whose attacker-to-target line runs into a raised shield from the front is absorbed by it.
    public static bool TryAbsorbWithShield(Vector2 attackerPos, Vector2 targetPos, float damage)
    {
        ShieldHoldFX shield = FindBlockingShield(attackerPos, targetPos);

        if (shield == null)
            return false;
        shield.Owner.ApplyShieldDamage(damage);
        return true;
    }

    private static Vector2 AttackerCenter(GameObject attacker)
    {
        Player player = attacker.GetComponent<Player>();

        if (player == null)
            return attacker.transform.position;
        return player.Center;
    }

    // Attacker's machine only (where the hitbox lives): a player's attack on another player that meets a raised shield from the front damages the shield instead.
    public static bool TryBlockAttack(GameObject attacker, IDamageable target, float damage)
    {
        Player victim = target as Player;

        if (victim == null || attacker == null)
            return false;
        return TryAbsorbWithShield(AttackerCenter(attacker), victim.Center, damage);
    }

    // Callable from any peer: the shield's owner writes its health.
    public void ApplyShieldDamage(float amount)
    {
        if (!NetworkObject.IsSpawned || IsOwner)
        {
            ReduceShieldHealth(amount);
            return;
        }
        ApplyShieldDamageRpc(amount);
    }
}
