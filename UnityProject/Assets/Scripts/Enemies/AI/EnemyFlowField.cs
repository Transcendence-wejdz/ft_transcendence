using System.Collections.Generic;
using UnityEngine;

// Grid around one player storing the walking cost from each cell to that player; enemies move toward cheaper cells.
public partial class EnemyFlowField
{
    public const float CellSize = 0.5f;
    public const int GridRadius = 54;

    private const int GridSide = GridRadius * 2 + 1;
    private const int Unreachable = int.MaxValue;
    private const int StraightStepCost = 10;
    private const int DiagonalStepCost = 14;
    private const int BucketCount = DiagonalStepCost + 1;

    private const sbyte CellUnknown = 0;
    private const sbyte CellFree = 1;
    private const sbyte CellBlocked = 2;

    private const int CacheBits = 9;
    private const int CacheSide = 1 << CacheBits;
    private const int CacheMask = CacheSide - 1;
    private const float CacheMinLifetime = 6f;
    private const float CacheMaxLifetime = 10f;
    private const float MinDirectionSqr = 0.0001f;
    private const float StepAxisThreshold = 0.38f;
    private const float SmallBodyRadius = 0.35f;
    private const float LargeBodyRadius = 0.55f;
    private const int LargeCacheSalt = (CacheSide / 2) << CacheBits | (CacheSide / 2);

    private static readonly int[] StepX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] StepY = { 0, 0, 1, -1, 1, -1, 1, -1 };

    private static readonly List<int>[] buckets = CreateBuckets();
    private static readonly int[] cacheCellX = new int[CacheSide * CacheSide];
    private static readonly int[] cacheCellY = new int[CacheSide * CacheSide];
    private static readonly float[] cacheExpires = new float[CacheSide * CacheSide];
    private static readonly bool[] cacheBlocked = new bool[CacheSide * CacheSide];
    private static readonly bool[] cacheLarge = new bool[CacheSide * CacheSide];

    private readonly int[] costToGoal = new int[GridSide * GridSide];
    private readonly sbyte[] cellStates = new sbyte[GridSide * GridSide];
    private readonly bool largeBody;
    private readonly float queryRadius;
    private Vector2Int goalCell;

    public bool IsBuilt { get; private set; }
    public float BuiltAt { get; private set; }
    public float LastRequestedAt { get; set; }
    public Vector2Int GoalCell => goalCell;

    public EnemyFlowField(bool largeBody)
    {
        this.largeBody = largeBody;
        queryRadius = SmallBodyRadius;
        if (largeBody)
            queryRadius = LargeBodyRadius;
    }

    // Bodies share one field per size class, so a gap it routes through always fits them.
    public static bool IsLargeBody(float bodyRadius)
    {
        return bodyRadius > SmallBodyRadius;
    }

    public static Vector2Int WorldToCell(Vector2 world)
    {
        return Vector2Int.RoundToInt(world / CellSize);
    }

    private static List<int>[] CreateBuckets()
    {
        List<int>[] lists = new List<int>[BucketCount];

        for (int i = 0; i < lists.Length; i++)
            lists[i] = new List<int>(256);
        return lists;
    }

    // Recomputes costs outward from the goal over walkable cells; edge costs are 10/14, so a ring of 15 buckets replaces the heap.
    public void Rebuild(Vector2 goalWorld)
    {
        int goalIndex = GridRadius * GridSide + GridRadius;
        int pending = 1;

        goalCell = WorldToCell(goalWorld);
        ResetGrid();
        costToGoal[goalIndex] = 0;
        buckets[0].Add(goalIndex);
        for (int currentCost = 0; pending > 0; currentCost++)
            pending += ExpandCellsAtCost(currentCost);
        IsBuilt = true;
        BuiltAt = Time.time;
    }

    private void ResetGrid()
    {
        for (int i = 0; i < costToGoal.Length; i++)
        {
            costToGoal[i] = Unreachable;
            cellStates[i] = CellUnknown;
        }
        foreach (List<int> bucket in buckets)
            bucket.Clear();
    }

    // Expands every cell still at this cost; returns the change in pending cells (newly queued minus popped).
    private int ExpandCellsAtCost(int currentCost)
    {
        List<int> bucket = buckets[currentCost % BucketCount];
        int pendingChange = 0;
        int cellIndex;

        for (int i = 0; i < bucket.Count; i++)
        {
            cellIndex = bucket[i];
            pendingChange--;
            if (costToGoal[cellIndex] == currentCost)
                pendingChange += ExpandNeighbors(cellIndex, currentCost);
        }
        bucket.Clear();
        return pendingChange;
    }

    // Lowers the cost of the 8 neighbors of one cell when this path is cheaper; returns how many were queued.
    private int ExpandNeighbors(int cellIndex, int currentCost)
    {
        int x = cellIndex % GridSide;
        int y = cellIndex / GridSide;
        int queued = 0;
        int neighborX;
        int neighborY;
        int newCost;
        int neighborCellIndex;

        for (int stepIndex = 0; stepIndex < 8; stepIndex++)
        {
            neighborX = x + StepX[stepIndex];
            neighborY = y + StepY[stepIndex];
            if (IsCellBlocked(neighborX, neighborY))
                continue;
            if (stepIndex >= 4 && (IsCellBlocked(neighborX, y) || IsCellBlocked(x, neighborY)))
                continue;
            newCost = currentCost + StepCost(stepIndex);
            neighborCellIndex = neighborY * GridSide + neighborX;
            if (newCost >= costToGoal[neighborCellIndex])
                continue;
            costToGoal[neighborCellIndex] = newCost;
            buckets[newCost % BucketCount].Add(neighborCellIndex);
            queued++;
        }
        return queued;
    }

}
