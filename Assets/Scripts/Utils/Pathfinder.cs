using System; // System.Math 사용을 위해 필수
using System.Collections.Generic;
using UnityEngine;
using Core.DataStructures;

public static class Pathfinder
{
    // 디버그 로그 출력 여부
    public static bool ShowDebugLogs = true;

    // [GC Optimization] 정적 캐싱
    private static readonly Direction[] _directions =
        { Direction.North, Direction.East, Direction.South, Direction.West };

    // BFS 캐시
    private static readonly Dictionary<GridCoords, int> _bfsCostCache = new Dictionary<GridCoords, int>();
    private static readonly Queue<GridCoords> _bfsQueueCache = new Queue<GridCoords>();

    // A* 캐시
    private static readonly PriorityQueue<GridCoords> _astarFrontier = new PriorityQueue<GridCoords>();
    private static readonly Dictionary<GridCoords, GridCoords> _astarCameFrom = new Dictionary<GridCoords, GridCoords>();
    private static readonly Dictionary<GridCoords, int> _astarCostSoFar = new Dictionary<GridCoords, int>();

    // ========================================================================
    // 1. 이동 가능 범위 계산 (BFS)
    // ========================================================================
    public static HashSet<GridCoords> GetReachableTiles(GridCoords start, int maxAP, MapManager map)
    {
        var result = new HashSet<GridCoords>();
        FillReachableTiles(start, maxAP, map, result);
        return result;
    }

    public static void FillReachableTiles(GridCoords start, int maxAP, MapManager map, ICollection<GridCoords> outputResults)
    {
        if (map == null || outputResults == null) return;

        Tile startTile = map.GetTile(start);
        if (startTile == null) return;

        _bfsCostCache.Clear();
        _bfsQueueCache.Clear();

        _bfsQueueCache.Enqueue(start);
        _bfsCostCache[start] = 0;
        outputResults.Add(start);

        while (_bfsQueueCache.Count > 0)
        {
            GridCoords current = _bfsQueueCache.Dequeue();
            int currentCost = _bfsCostCache[current];

            if (currentCost >= maxAP) continue;

            // [Optimization] yield return 제거 -> 방향 배열 직접 순회
            for (int i = 0; i < _directions.Length; i++)
            {
                Direction dir = _directions[i];

                // 유효성 검사 (검증 로직 분리)
                if (!IsValidMove(current, dir, map, out GridCoords next)) continue;

                int newCost = currentCost + 1;

                if (newCost <= maxAP)
                {
                    if (!_bfsCostCache.ContainsKey(next) || newCost < _bfsCostCache[next])
                    {
                        _bfsCostCache[next] = newCost;
                        outputResults.Add(next);
                        _bfsQueueCache.Enqueue(next);
                    }
                }
            }
        }
    }

    // ========================================================================
    // 2. 최단 경로 탐색 (A*)
    // ========================================================================
    public static List<GridCoords> FindPath(GridCoords start, GridCoords end, MapManager map)
    {
        if (start.Equals(end)) return new List<GridCoords>();

        // 층(Y) 체크
        if (start.y != end.y)
        {
            if (ShowDebugLogs) Debug.LogWarning($"[Pathfinder] Diff Level: {start} -> {end}");
            return null;
        }

        if (map == null) return null;

        Tile endTile = map.GetTile(end);
        if (endTile == null || !endTile.IsWalkable)
        {
            if (ShowDebugLogs) Debug.LogWarning($"[Pathfinder] Invalid End: {end}");
            return null;
        }

        _astarFrontier.Clear();
        _astarCameFrom.Clear();
        _astarCostSoFar.Clear();

        _astarFrontier.Enqueue(start, 0);
        _astarCameFrom[start] = start;
        _astarCostSoFar[start] = 0;

        int processedNodes = 0;

        while (_astarFrontier.Count > 0)
        {
            GridCoords current = _astarFrontier.Dequeue();
            processedNodes++;

            if (current.Equals(end))
            {
                var path = RetracePath(_astarCameFrom, start, end);
                if (ShowDebugLogs)
                    Debug.Log($"<color=cyan>[Pathfinder] Success:</color> Len:{path.Count}, Checked:{processedNodes}");
                return path;
            }

            // [Optimization] yield return 제거 -> 방향 배열 직접 순회
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

        if (ShowDebugLogs) Debug.LogWarning($"<color=red>[Pathfinder] Fail:</color> No path {start}->{end}");
        return null;
    }

    // ========================================================================
    // 3. 내부 유틸리티
    // ========================================================================

    /// <summary>
    /// [Core Logic] 현재 타일에서 특정 방향으로 이동이 가능한지 판별하고, 좌표를 반환합니다.
    /// 메모리 할당 없음.
    /// </summary>
    private static bool IsValidMove(GridCoords current, Direction dir, MapManager map, out GridCoords nextCoords)
    {
        nextCoords = GridUtils.GetNeighbor(current, dir);

        // 1. 맵 범위 / 타일 존재 체크
        // HasTile을 호출하지 않고 GetTile null 체크로 통합 (중복 조회 방지)
        Tile nextTile = map.GetTile(nextCoords);
        if (nextTile == null) return false;

        // 2. 현재 타일 정보 가져오기
        Tile currentTile = map.GetTile(current);
        if (currentTile == null) return false; // 이론상 발생 안 함

        // 3. 보행 가능 여부 (바닥, 중앙 장애물)
        if (!nextTile.IsWalkable) return false;

        // 4. 벽 판정 (나가는 곳)
        if (currentTile.IsPathBlockedByEdge(dir)) return false;

        // 5. 벽 판정 (들어오는 곳)
        Direction oppositeDir = GridUtils.GetOppositeDirection(dir);
        if (nextTile.IsPathBlockedByEdge(oppositeDir)) return false;

        return true;
    }

    // [Optimization] System.Math.Abs 사용 (정수 연산)
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

            if (!cameFrom.TryGetValue(current, out GridCoords parent))
            {
                Debug.LogError("[Pathfinder] Path linkage broken.");
                break;
            }
            current = parent;
        }

        path.Reverse();
        return path;
    }
}