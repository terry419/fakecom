using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛의 이동 경로를 계획하고, 이동력을 고려하여 유효성을 검사하는 핵심 클래스입니다.
/// </summary>
public class MovementPlanner
{
    private readonly MapManager _mapManager;

    // -------------------------------------------------------
    // Caching Variables
    // -------------------------------------------------------
    private GridCoords _lastStartCoords;
    private GridCoords _lastTargetCoords;
    private int _lastUnitMobility;
    private bool _lastUnitHasMoved;

    private int _lastMapVersion;
    private PathCalculationResult _cachedResult;

    public HashSet<GridCoords> CachedReachableTiles { get; private set; }

    public MovementPlanner(MapManager mapManager)
    {
        _mapManager = mapManager;
    }

    /// <summary>
    /// 유닛의 현재 남은 이동력을 가져옵니다. (Helper)
    /// </summary>
    private int GetRemainingMobility(Unit unit)
    {
        return unit.CurrentMobility;
    }

    public void CalculateReachableArea(Unit unit)
    {
        if (unit == null) return;

        int remainingMobility = GetRemainingMobility(unit);
        CachedReachableTiles = Pathfinder.GetReachableTiles(unit.Coordinate, remainingMobility, _mapManager);

        InvalidatePathCache();
    }

    public PathCalculationResult CalculatePath(Unit unit, GridCoords target)
    {
        if (unit == null || _mapManager == null) return PathCalculationResult.Empty;

        if (IsCacheValid(unit, target))
        {
            return _cachedResult;
        }

        return PerformPathCalculation(unit, target);
    }

    private bool IsCacheValid(Unit unit, GridCoords target)
    {
        return _cachedResult != null &&
               _lastStartCoords.Equals(unit.Coordinate) &&
               _lastTargetCoords.Equals(target) &&
               _lastUnitMobility == unit.CurrentMobility &&
               _lastUnitHasMoved == unit.HasStartedMoving &&
               _lastMapVersion == _mapManager.StateVersion;
    }

    private PathCalculationResult PerformPathCalculation(Unit unit, GridCoords target)
    {
        List<GridCoords> fullPath = Pathfinder.FindPath(unit.Coordinate, target, _mapManager);

        if (fullPath == null || fullPath.Count == 0)
        {
            return PathCalculationResult.Empty;
        }

        if (fullPath[0].Equals(unit.Coordinate))
        {
            fullPath.RemoveAt(0);
        }

        if (fullPath.Count == 0) return PathCalculationResult.Empty;

        int remainingMobility = GetRemainingMobility(unit);

        List<GridCoords> valid = new List<GridCoords>();
        List<GridCoords> invalid = new List<GridCoords>();
        bool isBlocked = false;

        for (int i = 0; i < fullPath.Count; i++)
        {
            GridCoords step = fullPath[i];

            Tile tile = _mapManager.GetTile(step);

            // 1. 타일 이동 불가(Pillar 등) 체크
            if (tile == null || !tile.IsWalkable)
            {
                isBlocked = true;
                invalid.Add(step);
                continue;
            }

            // 2. 다른 유닛 존재 여부 체크
            if (_mapManager.HasUnit(step))
            {
                isBlocked = true;
                invalid.Add(step);
                continue;
            }

            // 3. 이동력 소모 체크
            if (!isBlocked && (i + 1) <= remainingMobility)
            {
                valid.Add(step);
            }
            else
            {
                invalid.Add(step);
            }
        }

        // 실제 이동에 소모될 Mobility는 유효한 경로의 길이와 같습니다.
        int requiredMobility = valid.Count;

        _lastStartCoords = unit.Coordinate;
        _lastTargetCoords = target;
        _lastUnitMobility = unit.CurrentMobility;
        _lastUnitHasMoved = unit.HasStartedMoving;
        _lastMapVersion = _mapManager.StateVersion;

        _cachedResult = PathCalculationResult.Create(valid, invalid, isBlocked, requiredMobility);
        return _cachedResult;
    }

    public void InvalidatePathCache()
    {
        _cachedResult = null;
    }
}
