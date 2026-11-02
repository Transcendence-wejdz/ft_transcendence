using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Players never block each other: overlaps are pushed apart softly, and remote copies are shown already pushed.
public partial class Player
{
    private static float InnerRadius(Collider2D collider)
    {
        return Mathf.Min(collider.bounds.extents.x, collider.bounds.extents.y);
    }

    // A rider and its pig, or any mounted player, never push each other.
    private bool SkipsSoftCollision(Player other)
    {
        if (isMounted || other.isMounted)
            return true;
        return transform.parent == other.transform || other.transform.parent == transform;
    }

    // Half of the overlap with another player, pointing away from it.
    private Vector2 SeparationFrom(Player other, Vector2 center, float radius)
    {
        Vector2 offset = center - (Vector2)other.bodyCollider.bounds.center;
        float minDistance = radius + InnerRadius(other.bodyCollider);
        float distance = offset.magnitude;
        Vector2 away = Vector2.right;

        if (distance >= minDistance)
            return Vector2.zero;
        if (distance > MinMoveInputSqr)
            away = offset / distance;
        return away * ((minDistance - distance) * SoftCollisionShare);
    }

    // Players never block each other physically; the local player slides out of half of any overlap instead.
    private void UpdateSoftPlayerCollision()
    {
        Vector2 center;
        float radius;
        Vector2 separation = Vector2.zero;

        if (!this.IsLocallyControlled() || bodyCollider == null || rb == null)
            return;
        center = bodyCollider.bounds.center;
        radius = InnerRadius(bodyCollider);
        foreach (Player other in AllActiveInstances)
        {
            if (other == this || other.bodyCollider == null)
                continue;
            Physics2D.IgnoreCollision(bodyCollider, other.bodyCollider, true);
            if (!SkipsSoftCollision(other))
                separation += SeparationFrom(other, center, radius);
        }
        if (separation != Vector2.zero)
            AddPush(separation);
    }

    // Half the overlap between this remote copy and the local player, as the owner's soft collision will resolve it.
    private Vector2 PredictSoftPush(Player local)
    {
        Vector2 offset = BodyCenterNow(bodyCollider) - BodyCenterNow(local.bodyCollider);
        float minDistance = InnerRadius(bodyCollider) + InnerRadius(local.bodyCollider);
        float distance = offset.magnitude;
        Vector2 away = Vector2.right;

        if (distance >= minDistance)
            return Vector2.zero;
        if (distance > MinMoveInputSqr)
            away = offset / distance;
        return away * (minDistance - distance) * SoftCollisionShare;
    }

    // Shows a remote player's copy already pushed out of this peer's own player, until its owner's real (pushed) position arrives.
    private void LateUpdateRemotePushPrediction()
    {
        Player local;
        Vector2 push;

        if (!NetworkObject.IsSpawned || IsOwner || bodyCollider == null || transform.parent != null)
            return;
        local = LocalPlayer.GetPlayer();
        if (local == null || local == this || local.isMounted || local.bodyCollider == null)
            return;
        push = PredictSoftPush(local);
        if (push != Vector2.zero)
            transform.position += (Vector3)push;
    }

    // Queues a displacement (soft collision) that the next movement step turns into velocity, so walls still stop it.
    public void AddPush(Vector2 displacement)
    {
        pendingPush += displacement;
    }
}
