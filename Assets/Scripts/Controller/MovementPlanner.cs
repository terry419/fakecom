using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovementPlanner
{
    private readonly MapManager _mapManager;

    // [캐싱 전략] 마지막 계산 요청과 결과를 저장
    private GridCoords _lastStartCoords;
    private GridCoords _lastTargetCoords;
    private int _lastUnitAP;
    private PathCalculationResult _cachedResult;

    // 도달 가능 영역 캐시
    public HashSet<GridCoords> CachedReachableTiles { get; private set; }

    public MovementPlanner(MapManager mapManager)
    {
        _mapManager = mapManager;
    }

    /// <summary>
    /// 이동 가능 영역(BFS)을 계산하고 캐싱합니다. (턴 시작 시 호출)
    /// </summary>
    public void CalculateReachableArea(Unit unit)
    {
        if (unit == null) return;

        // Unit.CurrentMobility 사용
        int range = unit.HasStartedMoving
            ? unit.CurrentMobility
            : ((unit.CurrentAP >= 1) ? unit.Mobility : 0);

        // Pathfinder.GetReachableTiles 사용
        CachedReachableTiles = Pathfinder.GetReachableTiles(unit.Coordinate, range, _mapManager);
        InvalidatePathCache(); // 영역이 바뀌면 경로 캐시도 초기화
    }

    /// <summary>
    /// 경로를 계산하거나 캐시된 결과를 반환합니다.
    /// </summary>
    public PathCalculationResult CalculatePath(Unit unit, GridCoords target)
    {
        if (unit == null || _mapManager == null) return PathCalculationResult.Empty;

        // 1. 캐시 히트 검사 (시작점, 목표점, AP가 동일하면 재연산 X)
        if (_cachedResult != null &&
            _lastStartCoords.Equals(unit.Coordinate) &&
            _lastTargetCoords.Equals(target) &&
            _lastUnitAP == unit.CurrentAP)
        {
            return _cachedResult;
        }

        // 2. 실제 경로 계산 수행
        return PerformPathCalculation(unit, target);
    }

    private PathCalculationResult PerformPathCalculation(Unit unit, GridCoords target)
    {
        // Pathfinder.FindPath 사용
        List<GridCoords> fullPath = Pathfinder.FindPath(unit.Coordinate, target, _mapManager);

        if (fullPath == null || fullPath.Count == 0)
            return PathCalculationResult.Empty;

        // Unit.CurrentAP
        int maxRange = unit.HasStartedMoving
            ? unit.CurrentMobility
            : ((unit.CurrentAP >= 1) ? unit.Mobility : 0);

        List<GridCoords> valid = new List<GridCoords>();
        List<GridCoords> invalid = new List<GridCoords>();
        bool isBlocked = false;

        for (int i = 0; i < fullPath.Count; i++)
        {
            GridCoords step = fullPath[i];

            // (A) 유닛 충돌 체크 [cite: 866] MapManager.HasUnit
            if (_mapManager.HasUnit(step) && step != unit.Coordinate)
            {
                isBlocked = true;
                break; // 물리적 충돌은 즉시 중단
            }

            // (B) 지형 걷기 가능 여부 [cite: 860] MapManager.GetTile
            Tile t = _mapManager.GetTile(step);
            if (t != null && !t.IsWalkable) // [cite: 1895]
            {
                isBlocked = true;
                break;
            }

            // (C) AP(이동력) 체크
            if (i < maxRange)
                valid.Add(step);
            else
                invalid.Add(step); // AP 부족분은 Invalid로 분류
        }

        // 장애물로 막힌 경우, 막힌 지점 하나만 Invalid에 추가하여 시각적 피드백
        if (isBlocked && valid.Count < fullPath.Count)
        {
            invalid.Add(fullPath[valid.Count]);
        }
        else if (!isBlocked && invalid.Count > 0)
        {
            // 단순히 AP 부족으로 못 가는 경로는 전체 표시
            // (위 루프에서 이미 invalid에 추가됨)
        }

        // 3. 결과 캐싱 업데이트
        _lastStartCoords = unit.Coordinate;
        _lastTargetCoords = target;
        _lastUnitAP = unit.CurrentAP;

        _cachedResult = PathCalculationResult.Create(valid, invalid, isBlocked, 1); // Cost는 임시 1
        return _cachedResult;
    }

    public void InvalidatePathCache()
    {
        _cachedResult = null;
    }
}