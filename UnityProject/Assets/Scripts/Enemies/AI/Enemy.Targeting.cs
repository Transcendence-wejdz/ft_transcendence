using UnityEngine;

// Target choice: the nearest living player, refreshed on a timer or when the set of living players changes.
public partial class Enemy
{
    private Player target;
    private int seenPlayersVersion = -1;
    private float nextRetargetTime;

    public Player Target => target;

    private bool NeedsNewTarget(EnemyFlowFieldManager manager)
    {
        if (target == null || target.IsDead || target.IsDiving)
            return true;
        if (seenPlayersVersion != manager.PlayersVersion)
            return true;
        return Time.time >= nextRetargetTime;
    }

    private void UpdateTarget()
    {
        EnemyFlowFieldManager manager = EnemyFlowFieldManager.Instance;
        Player previous = target;

        if (!NeedsNewTarget(manager))
            return;
        nextRetargetTime = Time.time + Kit.RetargetInterval;
        seenPlayersVersion = manager.PlayersVersion;
        target = manager.FindNearestLivingPlayer(rb.position);
        if (Kit.Behaviour != EnemyBehaviour.Encircle)
            return;
        if (target != previous)
            ResetEncircle();
        manager.RegisterEncircler(this);
    }
}
