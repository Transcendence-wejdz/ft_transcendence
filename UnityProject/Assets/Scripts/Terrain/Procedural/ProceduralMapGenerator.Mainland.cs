using System.Collections.Generic;
using UnityEngine;

// The land connected to the players' spawn: water noise can leave islets that a walking enemy could never leave.
public partial class ProceduralMapGenerator
{
    private static readonly Vector2Int[] LandSteps = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private readonly HashSet<Vector2Int> mainland = new HashSet<Vector2Int>();

    // Flood fill from the spawn; straight steps only, since two land cells touching by a corner leave no way through. An endless map has no bound, so it skips this.
    private void FindMainland()
    {
        Queue<Vector2Int> open = new Queue<Vector2Int>();
        Vector2Int cell;
        Vector2Int next;

        mainland.Clear();
        if (Island.radius <= 0f || IsWater(spawnCell.x, spawnCell.y))
            return;
        mainland.Add(spawnCell);
        open.Enqueue(spawnCell);
        while (open.Count > 0)
        {
            cell = open.Dequeue();
            foreach (Vector2Int step in LandSteps)
            {
                next = cell + step;
                if (!mainland.Contains(next) && !IsWater(next.x, next.y))
                {
                    mainland.Add(next);
                    open.Enqueue(next);
                }
            }
        }
    }

    // Without a computed mainland (endless map), any land counts.
    public bool IsOnMainland(Vector3 worldPosition)
    {
        Vector3Int cell;

        if (mainland.Count == 0)
            return !IsWaterAtWorldPosition(worldPosition);
        cell = landTilemap.WorldToCell(worldPosition);
        return mainland.Contains(new Vector2Int(cell.x, cell.y));
    }
}
