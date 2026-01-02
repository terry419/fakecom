using System;
using System.Collections.Generic;
using UnityEngine;
using Core.DataStructures; // PriorityQueue

/// <summary>
/// [Optimization] Unity 단일 스레드 환경에 최적화된 Zero-Alloc 길찾기 연산기.
/// 정적 캐시를 재사용하여 GC 발생을 억제합니다.
/// </summary>
public static class Pathfinder
{
    // [GC Optimization] 방향 배열 캐싱
    private static readonly Direction[] _directions =
        { Direction.North, Direction.East, Direction.South, Direction.West };

    // [Static Cache] BFS용 (GetReachableTiles) - 재사용하여 할당 방지
    private static readonly Queue<GridCoords> _bfsQueue = new Queue<GridCoords>();
    private static readonly Dictionary<GridCoords, int> _bfsCost = new Dictionary<GridCoords, int>();

    // [Static Cache] A*용 (FindPath)
    private static readonly PriorityQueue<GridCoords> _astarFrontier = new PriorityQueue<GridCoords>();
    private static readonly Dictionary<GridCoords, GridCoords> _astarCameFrom = new Dictionary<GridCoords, GridCoords>();
    private static readonly Dictionary<GridCoords, int> _astarCostSoFar = new Dictionary<GridCoords, int>();

    // ========================================================================
    // 1. 이동 가능 범위 계산 (GetReachableTiles)
    // ========================================================================
    public static HashSet<GridCoords> GetReachableTiles(GridCoords start, int maxAP, MapManager map)
    {
        var results = new HashSet<GridCoords>(); // 반환용 컬렉션은 불가피하게 할당 (호출자가 관리)

        if (map == null || !map.HasTile(start)) return results;

        // [Performance] 캐시 초기화 (Clear는 내부 배열을 유지하므로 할당 없음)
        _bfsQueue.Clear();
        _bfsCost.Clear();

        _bfsQueue.Enqueue(start);
        _bfsCost[start] = 0;
        results.Add(start);

        while (_bfsQueue.Count > 0)
        {
            GridCoords current = _bfsQueue.Dequeue();
            int currentCost = _bfsCost[current];

            if (currentCost >= maxAP) continue;

            for (int i = 0; i < _directions.Length; i++)
            {
                Direction dir = _directions[i];

                // 유효성 검사 (MapManager 의존성 주입)
                if (!IsValidMove(current, dir, map, out GridCoords next)) continue;

                int newCost = currentCost + 1;

                if (newCost <= maxAP)
                {
                    if (!_bfsCost.ContainsKey(next) || newCost < _bfsCost[next])
                    {
                        _bfsCost[next] = newCost;
                        results.Add(next);
                        _bfsQueue.Enqueue(next);
                    }
                }
            }
        }

        return results;
    }

    // ========================================================================
    // 2. 최단 경로 탐색 (FindPath)
    // ========================================================================
    public static List<GridCoords> FindPath(GridCoords start, GridCoords end, MapManager map)
    {
        // [Issue #3 해결] 시작점과 도착점이 같으면 '이동 경로 없음(0칸)'으로 간주하여 빈 리스트 반환
        if (start.Equals(end)) return new List<GridCoords>();

        // 층간 이동 불가 등 기본 검사
        if (start.y != end.y) return null;
        if (map == null) return null;

        Tile endTile = map.GetTile(end);
        if (endTile == null || !endTile.IsWalkable) return null;

        // [Performance] 캐시 초기화
        _astarFrontier.Clear();
        _astarCameFrom.Clear();
        _astarCostSoFar.Clear();

        _astarFrontier.Enqueue(start, 0);
        _astarCameFrom[start] = start;
        _astarCostSoFar[start] = 0;

        bool found = false;

        while (_astarFrontier.Count > 0)
        {
            GridCoords current = _astarFrontier.Dequeue();

            if (current.Equals(end))
            {
                found = true;
                break;
            }

            for (int i = 0; i < _directions.Length; i++)
            {
                Direction dir = _directions[i];

                if (!IsValidMove(current, dir, map, out GridCoords next)) continue;

                int newCost = _astarCostSoFar[current] + 1;

                if (!_astarCostSoFar.ContainsKey(next) || newCost < _astarCostSoFar[next])
                {
                    _astarCostSoFar[next] = newCost;
                    int priority = newCost + GetHeuristic(next, end);
                    _astarFrontier.Enqueue(next, priority);
                    _astarCameFrom[next] = current;
                }
            }
        }

        return found ? RetracePath(_astarCameFrom, start, end) : null;
    }

    // ========================================================================
    // 3. 내부 헬퍼
    // ========================================================================
    private static bool IsValidMove(GridCoords current, Direction dir, MapManager map, out GridCoords nextCoords)
    {
        nextCoords = GridUtils.GetNeighbor(current, dir);

        Tile nextTile = map.GetTile(nextCoords);
        if (nextTile == null) return false;

        // 1. 현재 타일의 벽 체크
        Tile currentTile = map.GetTile(current);
        if (currentTile != null && currentTile.IsPathBlockedByEdge(dir)) return false;

        // 2. 다음 타일 보행 가능 여부
        if (!nextTile.IsWalkable) return false;

        // 3. 다음 타일의 맞은편 벽 체크
        Direction oppositeDir = GridUtils.GetOppositeDirection(dir);
        if (nextTile.IsPathBlockedByEdge(oppositeDir)) return false;

        return true;
    }

    private static int GetHeuristic(GridCoords a, GridCoords b)
    {
        return Math.Abs(a.x - b.x) + Math.Abs(a.z - b.z);
    }

    private static List<GridCoords> RetracePath(Dictionary<GridCoords, GridCoords> cameFrom, GridCoords start, GridCoords end)
    {
        var path = new List<GridCoords>();
        GridCoords current = end;

        while (!current.Equals(start))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }
}