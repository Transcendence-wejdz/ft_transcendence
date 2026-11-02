using System.Collections.Generic;
using UnityEngine;

// Encircle rings: each target's circling enemies get evenly spaced places around it.
public partial class EnemyFlowFieldManager
{
    // Encircle enemies join once and are dropped here as soon as they die, despawn or stop chasing.
    public void RegisterEncircler(Enemy enemy)
    {
        encirclers.Add(enemy);
    }

    private void GroupFlankersByTarget()
    {
        List<Enemy> ring;
        Player target;

        encirclers.RemoveWhere(enemy => enemy == null || !enemy.isActiveAndEnabled);
        foreach (List<Enemy> group in ringsByTarget.Values)
            group.Clear();
        foreach (Enemy enemy in encirclers)
        {
            target = enemy.Target;
            if (!enemy.IsFlanking || target == null || target.IsDead)
                continue;
            if (!ringsByTarget.TryGetValue(target, out ring))
            {
                ring = new List<Enemy>();
                ringsByTarget[target] = ring;
            }
            ring.Add(enemy);
        }
    }

    // Fills ringScratch with the ring sorted by the angle each enemy stands at around the target.
    private void SortRingByAngle(Vector2 center, List<Enemy> ring)
    {
        ringScratch.Clear();
        ringScratch.AddRange(ring);
        foreach (Enemy enemy in ringScratch)
            enemy.RingAngle = AngleAround(center, enemy.BodyCenter);
        ringScratch.Sort((a, b) => a.RingAngle.CompareTo(b.RingAngle));
    }

    // Shifted half a spacing from where the enemies stand (straight behind for a lone one) so each has to go around.
    private float ComputeRingBaseAngle(float spacing)
    {
        Vector2 offsetSum = Vector2.zero;
        float offset;

        for (int i = 0; i < ringScratch.Count; i++)
        {
            offset = (ringScratch[i].RingAngle - i * spacing) * Mathf.Deg2Rad;
            offsetSum += new Vector2(Mathf.Cos(offset), Mathf.Sin(offset));
        }
        return Mathf.Atan2(offsetSum.y, offsetSum.x) * Mathf.Rad2Deg + spacing * 0.5f;
    }

    private bool RingSizeChanged(Player target)
    {
        int previousSize;

        if (!ringSizes.TryGetValue(target, out previousSize))
            return true;
        if (previousSize != ringScratch.Count)
            return true;
        return !ringBaseAngles.ContainsKey(target);
    }

    // Evenly spaced places around the target; the layout is kept until the group size changes.
    private void LayoutRing(Player target, List<Enemy> ring)
    {
        float spacing;
        float baseAngle;

        SortRingByAngle(target.Center, ring);
        spacing = 360f / ringScratch.Count;
        if (RingSizeChanged(target))
        {
            ringBaseAngles[target] = ComputeRingBaseAngle(spacing);
            ringSizes[target] = ringScratch.Count;
        }
        baseAngle = ringBaseAngles[target];
        for (int i = 0; i < ringScratch.Count; i++)
            ringScratch[i].RingSlotAngle = baseAngle + i * spacing;
    }

    private void AssignRingSlots()
    {
        nextSlotAssignTime = Time.time + SlotAssignInterval;
        GroupFlankersByTarget();
        foreach (KeyValuePair<Player, List<Enemy>> pair in ringsByTarget)
        {
            if (pair.Value.Count == 0)
                ringSizes.Remove(pair.Key);
            else
                LayoutRing(pair.Key, pair.Value);
        }
    }

    private static float AngleAround(Vector2 center, Vector2 point)
    {
        Vector2 offset = point - center;

        return Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
    }
}
