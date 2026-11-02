using System.Collections.Generic;
using UnityEngine;

// Reading the field: downhill direction at a position, and the cached walkability queries behind it.
public partial class EnemyFlowField
{
    // Downhill direction at a position; false if outside the grid or unreachable.
    public bool TryGetDirection(Vector2 world, out Vector2 direction)
    {
        Vector2Int cell = ToCell(world);
        int x = cell.x - origin.x + Radius;
        int y = cell.y - origin.y + Radius;
        float here;

        direction = Vector2.zero;
        if (!InWindow(x, y))
            return false;
        here = cost[y * Size + x];
        if (here == Unreachable)
            here = LowestNeighborCost(x, y) + StraightCost;
        if (here >= Unreachable)
            return false;
        direction = SumDownhill(x, y, here);
        if (direction.sqrMagnitude < MinDirectionSqr)
            return false;
        direction.Normalize();
        return true;
    }

    // Each cheaper neighbor pulls toward itself, weighted by how steep the drop is.
    private Vector2 SumDownhill(int x, int y, float here)
    {
        Vector2 direction = Vector2.zero;
        int nx;
        int ny;
        int neighborCost;

        for (int d = 0; d < 8; d++)
        {
            nx = x + StepX[d];
            ny = y + StepY[d];
            if (!InWindow(nx, ny))
                continue;
            if (d >= 4 && (IsKnownBlocked(nx, y) || IsKnownBlocked(x, ny)))
                continue;
            neighborCost = cost[ny * Size + nx];
            if (neighborCost == Unreachable || neighborCost >= here)
                continue;
            direction += new Vector2(StepX[d], StepY[d]).normalized * ((here - neighborCost) / StepCost(d));
        }
        return direction;
    }

    private float LowestNeighborCost(int x, int y)
    {
        float lowest = Unreachable;
        int nx;
        int ny;

        for (int d = 0; d < 8; d++)
        {
            nx = x + StepX[d];
            ny = y + StepY[d];
            if (InWindow(nx, ny))
                lowest = Mathf.Min(lowest, cost[ny * Size + nx]);
        }
        return lowest;
    }

    // Window cells outside the grid count as blocked; each cell is queried at most once per rebuild.
    private bool IsBlocked(int x, int y)
    {
        int index = y * Size + x;

        if (!InWindow(x, y))
            return true;
        if (walkable[index] == Unknown)
        {
            walkable[index] = Free;
            if (QueryBlocked(origin.x + x - Radius, origin.y + y - Radius))
                walkable[index] = Blocked;
        }
        return walkable[index] == Blocked;
    }

    // Sampling never queries physics: cells the rebuild did not reach count as open.
    private bool IsKnownBlocked(int x, int y)
    {
        if (!InWindow(x, y))
            return false;
        return walkable[y * Size + x] == Blocked;
    }

    // Short-lived world cache shared by every field, in a wrapped 512x512 array so lookups never allocate.
    private static bool QueryBlocked(int cellX, int cellY)
    {
        int slot = (cellY & CacheMask) << CacheBits | (cellX & CacheMask);
        float now = Time.time;
        bool blocked;

        if (cacheCellX[slot] == cellX && cacheCellY[slot] == cellY && cacheExpires[slot] > now)
            return cacheBlocked[slot];
        blocked = ObstacleQuery.IsBlocked(new Vector2(cellX, cellY) * CellSize, EnemyFlowFieldManager.BodyRadius);
        cacheCellX[slot] = cellX;
        cacheCellY[slot] = cellY;
        cacheExpires[slot] = now + Random.Range(CacheMinLifetime, CacheMaxLifetime);
        cacheBlocked[slot] = blocked;
        return blocked;
    }

    // Steps 0-3 are straight, 4-7 diagonal.
    private static int StepCost(int step)
    {
        if (step >= 4)
            return DiagonalCost;
        return StraightCost;
    }

    private static bool InWindow(int x, int y)
    {
        return x >= 0 && y >= 0 && x < Size && y < Size;
    }
}
