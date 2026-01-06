using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛의 이동 경로를 계산하고, 이동력 제한을 적용하여 유효성을 검증하는 로직 클래스입니다.
/// </summary>
public class MovementPlanner
{
    private readonly MapManager _mapManager;

    // -------------------------------------------------------
    // Caching Variables
    // -------------------------------------------------------
    private GridCoords _lastStartCoords;
    private GridCoords _lastTargetCoords;
    // [수정] AP 관련 캐싱 변수 삭제 (_lastUnitAP)
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
    /// 이동 가능 거리 계산 로직 공통화 (Helper)
    /// </summary>
    private int GetMaxMoveRange(Unit unit)
    {
        // [수정] AP 개념 삭제 -> 현재 남은 이동력(Mobility)이 곧 이동 가능 거리
        return unit.CurrentMobility;
    }

    public void CalculateReachableArea(Unit unit)
    {
        if (unit == null) return;

        int range = GetMaxMoveRange(unit);
        CachedReachableTiles = Pathfinder.GetReachableTiles(unit.Coordinate, range, _mapManager);

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
        // [수정] AP 비교 로직 삭제 (_lastUnitAP)
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

        int maxRange = GetMaxMoveRange(unit);

        // [수정] AP 비용 개념이 사라졌으므로 항상 0 처리 (혹은 PathCalculationResult 구조 수정 필요 시 0 전달)
        int requiredActionPoint = 0;

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

            // 2. 유닛 존재 여부 체크
            if (_mapManager.HasUnit(step))
            {
                isBlocked = true;
                invalid.Add(step);
                continue;
            }

            // 3. 이동력 범위 체크
            if (!isBlocked && (i + 1) <= maxRange)
            {
                valid.Add(step);
            }
            else
            {
                invalid.Add(step);
            }
        }

        _lastStartCoords = unit.Coordinate;
        _lastTargetCoords = target;
        // [수정] AP 캐싱 삭제
        _lastUnitMobility = unit.CurrentMobility;
        _lastUnitHasMoved = unit.HasStartedMoving;
        _lastMapVersion = _mapManager.StateVersion;

        _cachedResult = PathCalculationResult.Create(valid, invalid, isBlocked, requiredActionPoint);
        return _cachedResult;
    }

    public void InvalidatePathCache()
    {
        _cachedResult = null;
    }
}