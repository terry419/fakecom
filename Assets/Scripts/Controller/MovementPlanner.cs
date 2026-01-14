using System.Collections.Generic;
using UnityEngine;

public class MovementPlanner
{
    private readonly MapManager _mapManager;

    // ... (기존 캐싱 변수 유지) ...
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

    private int GetRemainingMobility(Unit unit) => unit.CurrentMobility;

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
        if (IsCacheValid(unit, target)) return _cachedResult;
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
        // 1. A* 경로 탐색 (지형/유닛만 고려된 경로)
        List<GridCoords> fullPath = Pathfinder.FindPath(unit.Coordinate, target, _mapManager);

        if (fullPath == null || fullPath.Count == 0) return PathCalculationResult.Empty;
        if (fullPath[0].Equals(unit.Coordinate)) fullPath.RemoveAt(0);
        if (fullPath.Count == 0) return PathCalculationResult.Empty;

        int remainingMobility = GetRemainingMobility(unit);
        List<GridCoords> valid = new List<GridCoords>();
        List<GridCoords> invalid = new List<GridCoords>();
        bool isBlocked = false;

        GridCoords currentCoords = unit.Coordinate; // 시작점

        for (int i = 0; i < fullPath.Count; i++)
        {
            GridCoords nextCoords = fullPath[i];
            Tile nextTile = _mapManager.GetTile(nextCoords);
            Tile currentTile = _mapManager.GetTile(currentCoords); // 현재 위치 타일

            // [Check 1] 타일 자체 이동 불가 (기둥 등)
            if (nextTile == null || !nextTile.IsWalkable)
            {
                isBlocked = true;
                invalid.Add(nextCoords);
                continue; // 경로가 끊겼으므로 이후는 체크 불필요하지만 루프는 돔 (invalid 처리)
            }

            // [Check 2] 유닛 충돌 체크
            if (_mapManager.HasUnit(nextCoords))
            {
                isBlocked = true;
                invalid.Add(nextCoords);
                continue;
            }

            // [Check 3] (신규) 벽(Edge) 통과 여부 체크
            // 현재 타일에서 다음 타일로 가는 방향을 구함
            if (GridUtils.IsAdjacent(currentCoords, nextCoords))
            {
                Direction moveDir = GridUtils.GetRelativeDirection(currentCoords, nextCoords);

                // 내 타일의 해당 방향 벽 확인
                if (currentTile.IsPathBlockedByEdge(moveDir))
                {
                    isBlocked = true;
                    Debug.Log($"[Planner] Blocked by Wall at {currentCoords} towards {moveDir}");
                }

                // (안전장치) 상대방 타일의 반대편 벽도 확인 (RuntimeEdge 공유 시 하나만 봐도 되지만 안전하게)
                /*
                Direction oppositeDir = GridUtils.GetOppositeDirection(moveDir);
                if (!isBlocked && nextTile.IsPathBlockedByEdge(oppositeDir))
                {
                    isBlocked = true;
                }
                */
            }

            if (isBlocked)
            {
                invalid.Add(nextCoords);
            }
            else
            {
                // 이동력 체크
                if ((i + 1) <= remainingMobility)
                {
                    valid.Add(nextCoords);
                    currentCoords = nextCoords; // 다음 스텝을 위해 현재 좌표 갱신
                }
                else
                {
                    invalid.Add(nextCoords);
                }
            }
        }

        int requiredMobility = valid.Count;

        _lastStartCoords = unit.Coordinate;
        _lastTargetCoords = target;
        _lastUnitMobility = unit.CurrentMobility;
        _lastUnitHasMoved = unit.HasStartedMoving;
        _lastMapVersion = _mapManager.StateVersion;

        _cachedResult = PathCalculationResult.Create(valid, invalid, isBlocked, requiredMobility);
        return _cachedResult;
    }

    public void InvalidatePathCache() => _cachedResult = null;
}