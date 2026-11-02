using System.Collections.Generic;
using UnityEngine;

// Dial (bucketed Dijkstra) cost grid around one player; a cell is walkable when an enemy body fits at its center, enemies read directions in O(1).
public partial class EnemyFlowField
{
    public const float CellSize = 0.5f;
    public const int Radius = 54;

    private const int Size = Radius * 2 + 1;
    private const int Unreachable = int.MaxValue;
    private const int StraightCost = 10;
    private const int DiagonalCost = 14;
    private const int BucketCount = DiagonalCost + 1;

    private const sbyte Unknown = 0;
    private const sbyte Free = 1;
    private const sbyte Blocked = 2;

    private const int CacheBits = 9;
    private const int CacheSide = 1 << CacheBits;
    private const int CacheMask = CacheSide - 1;
    private const float CacheMinLifetime = 6f;
    private const float CacheMaxLifetime = 10f;
    private const float MinDirectionSqr = 0.0001f;

    private static readonly int[] StepX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] StepY = { 0, 0, 1, -1, 1, -1, 1, -1 };

    private static readonly List<int>[] buckets = CreateBuckets();
    private static readonly int[] cacheCellX = new int[CacheSide * CacheSide];
    private static readonly int[] cacheCellY = new int[CacheSide * CacheSide];
    private static readonly float[] cacheExpires = new float[CacheSide * CacheSide];
    private static readonly bool[] cacheBlocked = new bool[CacheSide * CacheSide];

    private readonly int[] cost = new int[Size * Size];
    private readonly sbyte[] walkable = new sbyte[Size * Size];
    private Vector2Int origin;

    public bool IsBuilt { get; private set; }
    public float BuiltAt { get; private set; }
    public float LastRequestedAt { get; set; }
    public Vector2Int GoalCell => origin;

    public static Vector2Int ToCell(Vector2 world)
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
        int goalIndex = Radius * Size + Radius;
        int pending = 1;

        origin = ToCell(goalWorld);
        ResetGrid();
        cost[goalIndex] = 0;
        buckets[0].Add(goalIndex);
        for (int current = 0; pending > 0; current++)
            pending += SettleBucket(current);
        IsBuilt = true;
        BuiltAt = Time.time;
    }

    private void ResetGrid()
    {
        for (int i = 0; i < cost.Length; i++)
        {
            cost[i] = Unreachable;
            walkable[i] = Unknown;
        }
        foreach (List<int> bucket in buckets)
            bucket.Clear();
    }

    // Expands every cell still at this cost; returns the change in pending cells (newly queued minus popped).
    private int SettleBucket(int current)
    {
        List<int> bucket = buckets[current % BucketCount];
        int delta = 0;
        int index;

        for (int b = 0; b < bucket.Count; b++)
        {
            index = bucket[b];
            delta--;
            if (cost[index] == current)
                delta += Expand(index, current);
        }
        bucket.Clear();
        return delta;
    }

    // Relaxes the 8 neighbors of one settled cell; returns how many were queued.
    private int Expand(int index, int current)
    {
        int x = index % Size;
        int y = index / Size;
        int queued = 0;
        int nx;
        int ny;
        int newCost;
        int neighborIndex;

        for (int d = 0; d < 8; d++)
        {
            nx = x + StepX[d];
            ny = y + StepY[d];
            if (IsBlocked(nx, ny))
                continue;
            if (d >= 4 && (IsBlocked(nx, y) || IsBlocked(x, ny)))
                continue;
            newCost = current + StepCost(d);
            neighborIndex = ny * Size + nx;
            if (newCost >= cost[neighborIndex])
                continue;
            cost[neighborIndex] = newCost;
            buckets[newCost % BucketCount].Add(neighborIndex);
            queued++;
        }
        return queued;
    }

}
