using System.Collections.Generic;
using UnityEngine;

// Damage ticks of the zone: one hit per damageable inside, never its owner, stopped by shields.
public partial class ZoneAttackFX
{
    private bool IsOwner(IDamageable damageable)
    {
        MonoBehaviour behaviour = damageable as MonoBehaviour;

        return behaviour != null && behaviour.gameObject == owner;
    }

    private void TryDamage(Collider2D other)
    {
        IDamageable damageable;

        if (other.GetComponent<ShieldHoldFX>() != null)
            return;
        damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || !hitThisTick.Add(damageable))
            return;
        if (!IsOwner(damageable) && !Player.TryBlockAttack(owner, damageable, damage))
            damageable.RequestDamage(damage);
    }

    // One hit per damageable per tick, however many of its colliders overlap the zone.
    private void DamageInside()
    {
        if (hitbox == null || !hitbox.enabled)
            return;
        hitThisTick.Clear();
        hitbox.Overlap(filter, overlaps);
        foreach (Collider2D other in overlaps)
            TryDamage(other);
    }
}
