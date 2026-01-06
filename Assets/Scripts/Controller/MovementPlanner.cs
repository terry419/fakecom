using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛의 이동 경로를 계산하고, AP 및 이동력 제한을 적용하여 유효성을 검증하는 로직 클래스입니다.
/// PlayerController의 비즈니스 로직을 분담합니다.
/// </summary>
public class MovementPlanner
{
    private readonly MapManager _mapManager;

    // -------------------------------------------------------
    // Caching (호버링 시 매 프레임 계산 부하 방지)
    // -------------------------------------------------------
    private GridCoords _lastStartCoords;
    private GridCoords _lastTargetCoords;
    private int _lastUnitAP;
    private int _lastUnitMobility;
    private bool _lastUnitHasMoved; // 이동 시작 여부 상태 캐싱

    private PathCalculationResult _cachedResult;

    /// <summary>
    /// CalculateReachableArea를 통해 미리 계산된 이동 가능 타일 집합입니다.
    /// Visualizer가 바닥을 칠할 때 사용합니다.
    /// </summary>
    public HashSet<GridCoords> CachedReachableTiles { get; private set; }

    // 생성자 주입
    public MovementPlanner(MapManager mapManager)
    {
        _mapManager = mapManager;
    }

    /// <summary>
    /// 턴 시작 또는 이동 종료 후, 유닛이 갈 수 있는 모든 범위를 미리 계산합니다. (BFS)
    /// </summary>
    public void CalculateReachableArea(Unit unit)
    {
        if (unit == null) return;

        // 이동 가능 거리 판정:
        // 이미 이동 중이라면 -> 남은 이동력(CurrentMobility) 만큼만 이동 가능
        // 아직 이동 안했다면 -> AP가 1 이상일 때 전체 이동력(Mobility) 만큼 이동 가능
        int range = unit.HasStartedMoving
            ? unit.CurrentMobility
            : ((unit.CurrentAP >= 1) ? unit.Mobility : 0);

        CachedReachableTiles = Pathfinder.GetReachableTiles(unit.Coordinate, range, _mapManager);

        // 상태가 변했으므로 경로 캐시도 초기화
        InvalidatePathCache();
    }

    /// <summary>
    /// 목표 지점까지의 경로를 계산하고, AP 비용 및 장애물 여부를 판단하여 결과 객체를 반환합니다.
    /// </summary>
    public PathCalculationResult CalculatePath(Unit unit, GridCoords target)
    {
        if (unit == null || _mapManager == null) return PathCalculationResult.Empty;

        // 1. 캐시 유효성 검사 (입력 조건이 모두 동일하면 이전 결과 반환)
        if (IsCacheValid(unit, target))
        {
            return _cachedResult;
        }

        // 2. 실제 경로 계산 수행
        return PerformPathCalculation(unit, target);
    }

    private bool IsCacheValid(Unit unit, GridCoords target)
    {
        return _cachedResult != null &&
               _lastStartCoords.Equals(unit.Coordinate) &&
               _lastTargetCoords.Equals(target) &&
               _lastUnitAP == unit.CurrentAP &&
               _lastUnitMobility == unit.CurrentMobility &&
               _lastUnitHasMoved == unit.HasStartedMoving;
    }

    private PathCalculationResult PerformPathCalculation(Unit unit, GridCoords target)
    {
        // A* 알고리즘으로 최단 경로 탐색 (단순 지형 기준)
        List<GridCoords> fullPath = Pathfinder.FindPath(unit.Coordinate, target, _mapManager);

        // 경로가 없으면 Empty 반환
        if (fullPath == null || fullPath.Count == 0) return PathCalculationResult.Empty;

        // -------------------------------------------------------
        // 유효성 검증 로직 (Validation Logic)
        // -------------------------------------------------------

        // 1. 최대 이동 가능 거리 설정
        int maxRange = unit.HasStartedMoving
            ? unit.CurrentMobility
            : ((unit.CurrentAP >= 1) ? unit.Mobility : 0);

        // 2. AP 비용 산정 (이동 모드 개시 비용)
        // 이미 이동 중이면 0, 아니면 1 소모
        int costAP = unit.HasStartedMoving ? 0 : 1;

        List<GridCoords> valid = new List<GridCoords>();
        List<GridCoords> invalid = new List<GridCoords>();
        bool isBlocked = false;

        for (int i = 0; i < fullPath.Count; i++)
        {
            GridCoords step = fullPath[i];

            // (A) 물리적 장애물(다른 유닛) 체크
            // 내 위치가 아니고, 다른 유닛이 있다면 길막힘 판정
            if (step != unit.Coordinate && _mapManager.HasUnit(step))
            {
                isBlocked = true;
                invalid.Add(step);
                // 여기서 break 하면 장애물 뒤 경로는 아예 안보임
                // 빨간색 선으로 계속 보여주려면 break 없이 invalid에 추가만 함
                // 기획 의도에 따라 break;를 넣을 수도 있음. 현재는 경로가 끊긴 것을 표현하기 위해 계속 진행
                continue;
            }

            // (B) 이동력(Mobility) 범위 체크
            // i는 0부터 시작 (0번째는 첫 번째 이동 타일) -> 거리 1
            // 경로 리스트의 길이는 거리와 동일함
            if (!isBlocked && (i + 1) <= maxRange)
            {
                valid.Add(step);
            }
            else
            {
                // 범위 밖이거나 이미 앞에서 막혔다면 Invalid
                invalid.Add(step);
            }
        }

        // (C) 최종 막힘 판정 보정
        // 만약 유효 경로가 하나도 없거나, 목표 지점이 Invalid에 있다면 Blocked로 간주할 수도 있음
        // 여기서는 물리적 유닛 충돌(HasUnit)이 발생했을 때를 IsBlocked로 설정함

        // 캐시 데이터 갱신
        _lastStartCoords = unit.Coordinate;
        _lastTargetCoords = target;
        _lastUnitAP = unit.CurrentAP;
        _lastUnitMobility = unit.CurrentMobility;
        _lastUnitHasMoved = unit.HasStartedMoving;

        _cachedResult = PathCalculationResult.Create(valid, invalid, isBlocked, costAP);
        return _cachedResult;
    }

    /// <summary>
    /// 턴 종료나 유닛 변경 시 캐시를 강제로 비웁니다.
    /// </summary>
    public void InvalidatePathCache()
    {
        _cachedResult = null;
    }
}