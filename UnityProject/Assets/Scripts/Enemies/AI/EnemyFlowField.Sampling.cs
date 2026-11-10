using System.Collections.Generic;
using UnityEngine;

// Reading the field: downhill direction at a position, and the cached walkability queries behind it.
public partial class EnemyFlowField
{
    // Direction toward cheaper cells (downhill) at a position; false if outside the grid or unreachable.
    public bool TryGetPathDirection(Vector2 world, out Vector2 direction)
    {
        Vector2Int cell = WorldToCell(world);
        int x = cell.x - goalCell.x + GridRadius;
        int y = cell.y - goalCell.y + GridRadius;
        Vector2 escapeStep;
        float hereCost;

        direction = Vector2.zero;
        if (!IsInGrid(x, y))
            return false;
        hereCost = costToGoal[y * GridSide + x];
        if (hereCost == Unreachable)
            hereCost = CheapestNeighborIgnoringCorners(x, y, out escapeStep) + StraightStepCost;
        if (hereCost >= Unreachable)
            return false;
        direction = BlendedDownhillDirection(x, y, hereCost);
        if (!PointsToCheaperCell(x, y, hereCost, direction))
            direction = DirectionToCheapestNeighbor(x, y, hereCost);
        if (direction.sqrMagnitude < MinDirectionSqr && hereCost > 0f && CheapestNeighborIgnoringCorners(x, y, out escapeStep) < hereCost)
            direction = escapeStep;
        if (direction.sqrMagnitude < MinDirectionSqr)
            return false;
        direction.Normalize();
        return true;
    }

    // Cost of a neighbor the body can step to and that is cheaper than here; Unreachable otherwise.
    private int CheaperNeighborCost(int x, int y, int stepIndex, float hereCost)
    {
        int neighborX = x + StepX[stepIndex];
        int neighborY = y + StepY[stepIndex];
        int neighborCost;

        if (!IsInGrid(neighborX, neighborY))
            return Unreachable;
        if (stepIndex >= 4 && (WasCellFoundBlocked(neighborX, y) || WasCellFoundBlocked(x, neighborY)))
            return Unreachable;
        neighborCost = costToGoal[neighborY * GridSide + neighborX];
        if (neighborCost >= hereCost)
            return Unreachable;
        return neighborCost;
    }

    // The averaged pull is only trusted when the cell it points at is cheaper: behind an obstacle both sides cancel and it would point back and forth.
    private bool PointsToCheaperCell(int x, int y, float hereCost, Vector2 direction)
    {
        Vector2 aim;

        if (direction.sqrMagnitude < MinDirectionSqr)
            return false;
        aim = direction.normalized;
        for (int stepIndex = 0; stepIndex < 8; stepIndex++)
        {
            if (RoundToStep(aim.x) == StepX[stepIndex] && RoundToStep(aim.y) == StepY[stepIndex])
                return CheaperNeighborCost(x, y, stepIndex, hereCost) != Unreachable;
        }
        return false;
    }

    private static int RoundToStep(float value)
    {
        if (value > StepAxisThreshold)
            return 1;
        if (value < -StepAxisThreshold)
            return -1;
        return 0;
    }

    // Steepest neighbor, first one on a tie: the cost strictly drops at each cell, so the path can never loop.
    private Vector2 DirectionToCheapestNeighbor(int x, int y, float hereCost)
    {
        Vector2 direction = Vector2.zero;
        int lowest = Unreachable;
        int neighborCost;

        for (int stepIndex = 0; stepIndex < 8; stepIndex++)
        {
            neighborCost = CheaperNeighborCost(x, y, stepIndex, hereCost);
            if (neighborCost >= lowest)
                continue;
            lowest = neighborCost;
            direction = new Vector2(StepX[stepIndex], StepY[stepIndex]);
        }
        return direction;
    }

    // Each cheaper neighbor pulls toward itself, weighted by how steep the drop is.
    private Vector2 BlendedDownhillDirection(int x, int y, float hereCost)
    {
        Vector2 direction = Vector2.zero;
        int neighborCost;

        for (int stepIndex = 0; stepIndex < 8; stepIndex++)
        {
            neighborCost = CheaperNeighborCost(x, y, stepIndex, hereCost);
            if (neighborCost == Unreachable)
                continue;
            direction += new Vector2(StepX[stepIndex], StepY[stepIndex]).normalized * ((hereCost - neighborCost) / StepCost(stepIndex));
        }
        return direction;
    }

    // Cheapest neighbor whatever the corners, for a body already overlapping a blocked cell.
    private float CheapestNeighborIgnoringCorners(int x, int y, out Vector2 step)
    {
        float lowest = Unreachable;
        int neighborX;
        int neighborY;

        step = Vector2.zero;
        for (int stepIndex = 0; stepIndex < 8; stepIndex++)
        {
            neighborX = x + StepX[stepIndex];
            neighborY = y + StepY[stepIndex];
            if (!IsInGrid(neighborX, neighborY) || costToGoal[neighborY * GridSide + neighborX] >= lowest)
                continue;
            lowest = costToGoal[neighborY * GridSide + neighborX];
            step = new Vector2(StepX[stepIndex], StepY[stepIndex]);
        }
        return lowest;
    }

    // Cells outside the grid count as blocked; each cell is queried at most once per rebuild.
    private bool IsCellBlocked(int x, int y)
    {
        int cellIndex = y * GridSide + x;

        if (!IsInGrid(x, y))
            return true;
        if (cellStates[cellIndex] == CellUnknown)
        {
            cellStates[cellIndex] = CellFree;
            if (IsWorldCellBlockedCached(goalCell.x + x - GridRadius, goalCell.y + y - GridRadius))
                cellStates[cellIndex] = CellBlocked;
        }
        return cellStates[cellIndex] == CellBlocked;
    }

    // Sampling never queries physics: cells the rebuild did not reach count as open.
    private bool WasCellFoundBlocked(int x, int y)
    {
        if (!IsInGrid(x, y))
            return false;
        return cellStates[y * GridSide + x] == CellBlocked;
    }

    // Short-lived world cache shared by every field, in a wrapped 512x512 array so lookups never allocate.
    private bool IsWorldCellBlockedCached(int cellX, int cellY)
    {
        int slot = (cellY & CacheMask) << CacheBits | (cellX & CacheMask);
        float now = Time.time;
        bool blocked;

        if (largeBody)
            slot ^= LargeCacheSalt;
        if (cacheCellX[slot] == cellX && cacheCellY[slot] == cellY && cacheLarge[slot] == largeBody && cacheExpires[slot] > now)
            return cacheBlocked[slot];
        blocked = ObstacleQuery.IsBlocked(new Vector2(cellX, cellY) * CellSize, queryRadius);
        cacheCellX[slot] = cellX;
        cacheCellY[slot] = cellY;
        cacheLarge[slot] = largeBody;
        cacheExpires[slot] = now + Random.Range(CacheMinLifetime, CacheMaxLifetime);
        cacheBlocked[slot] = blocked;
        return blocked;
    }

    // Steps 0-3 are straight, 4-7 diagonal.
    private static int StepCost(int step)
    {
        if (step >= 4)
            return DiagonalStepCost;
        return StraightStepCost;
    }

    private static bool IsInGrid(int x, int y)
    {
        return x >= 0 && y >= 0 && x < GridSide && y < GridSide;
    }
}
