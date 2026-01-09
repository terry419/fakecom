using System;
using System.Collections.Generic;
using UnityEngine;
using Core.DataStructures;

public static class Pathfinder
{
    private static readonly Direction[] _directions =
        { Direction.North, Direction.East, Direction.South, Direction.West };

    // Caches
    private static readonly Queue<GridCoords> _bfsQueue = new Queue<GridCoords>();
    private static readonly Dictionary<GridCoords, int> _bfsCost = new Dictionary<GridCoords, int>();

    private static readonly PriorityQueue<GridCoords> _astarFrontier = new PriorityQueue<GridCoords>();
    private static readonly Dictionary<GridCoords, GridCoords> _astarCameFrom = new Dictionary<GridCoords, GridCoords>();
    private static readonly Dictionary<GridCoords, int> _astarCostSoFar = new Dictionary<GridCoords, int>();

    // 1. Reachable Tiles (BFS)
    public static HashSet<GridCoords> GetReachableTiles(GridCoords start, int mobility, MapManager map)
    {
        var results = new HashSet<GridCoords>();
        if (map == null || !map.HasTile(start)) return results;

        _bfsQueue.Clear();
        _bfsCost.Clear();

        _bfsQueue.Enqueue(start);
        _bfsCost[start] = 0;
        results.Add(start);

        while (_bfsQueue.Count > 0)
        {
            GridCoords current = _bfsQueue.Dequeue();
            int currentCost = _bfsCost[current];
            if (currentCost >= mobility) continue;

            Tile currentTile = map.GetTile(current);

            // A. 일반 인접 이동 (상하좌우)
            for (int i = 0; i < _directions.Length; i++)
            {
                if (!IsValidMove(current, _directions[i], map, out GridCoords next, ignoreWalkability: false)) continue;

                int newCost = currentCost + 1;
                ProcessNeighbor(next, newCost, mobility, results);
            }

            // B. [New] 포탈 이동 체크
            // 현재 타일이 포탈이고, 유효한 출구가 있다면 그곳도 이웃으로 간주
            if (currentTile != null && currentTile.HasActiveExits())
            {
                GridCoords? portalExit = GetValidPortalExit(currentTile, map);
                if (portalExit.HasValue)
                {
                    // 포탈 이동 비용 적용 (기본 1, 데이터에 따라 다름)
                    int moveCost = currentTile.PortalData.MovementCost;
                    int newCost = currentCost + moveCost;
                    ProcessNeighbor(portalExit.Value, newCost, mobility, results);
                }
            }
        }
        return results;
    }

    // BFS 중복 코드 분리
    private static void ProcessNeighbor(GridCoords next, int newCost, int mobility, HashSet<GridCoords> results)
    {
        if (newCost <= mobility)
        {
            if (!_bfsCost.ContainsKey(next) || newCost < _bfsCost[next])
            {
                _bfsCost[next] = newCost;
                results.Add(next);
                _bfsQueue.Enqueue(next);
            }
        }
    }

    // 2. Find Path (A*)
    public static List<GridCoords> FindPath(GridCoords start, GridCoords end, MapManager map)
    {
        if (start.Equals(end)) return new List<GridCoords>();

        // [Fix] 3D 이동을 위해 Y축 제한 해제
        // if (start.y != end.y) return null; 

        if (map == null) return null;

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

            Tile currentTile = map.GetTile(current);

            // A. 일반 인접 이동
            for (int i = 0; i < _directions.Length; i++)
            {
                Direction dir = _directions[i];
                GridCoords nextCheck = GridUtils.GetNeighbor(current, dir);
                bool isDestination = nextCheck.Equals(end);

                if (!IsValidMove(current, dir, map, out GridCoords next, ignoreWalkability: isDestination)) continue;

                int newCost = _astarCostSoFar[current] + 1;
                ProcessAStarNeighbor(current, next, newCost, end);
            }

            if (currentTile != null)
            {
                // [Debug Log] 포탈 발견 시 상태 확인
                if (currentTile.PortalData != null)
                {
                    int destCount = currentTile.PortalData.Destinations?.Count ?? 0;
                    if (destCount == 0)
                    {
                        // 목적지가 없어서 이동 불가능한 상황
                        Debug.LogWarning($"[Pathfinder] Portal Found at {current}, but Destination List is EMPTY! (LinkID: {currentTile.PortalData.LinkID})");
                    }
                    else
                    {
                        // 정상적으로 목적지가 있는 상황
                        Debug.Log($"[Pathfinder] Portal Found at {current}. Destinations: {destCount}");
                    }
                }
            }

            // B. [New] 포탈 이동 (BFS와 동일 로직)
            if (currentTile != null && currentTile.HasActiveExits())
            {
                GridCoords? portalExit = GetValidPortalExit(currentTile, map);
                if (portalExit.HasValue)
                {
                    GridCoords next = portalExit.Value;
                    int moveCost = currentTile.PortalData.MovementCost;
                    int newCost = _astarCostSoFar[current] + moveCost;

                    ProcessAStarNeighbor(current, next, newCost, end);
                }
            }
        }

        return found ? RetracePath(_astarCameFrom, start, end) : null;
    }

    private static void ProcessAStarNeighbor(GridCoords current, GridCoords next, int newCost, GridCoords end)
    {
        if (!_astarCostSoFar.ContainsKey(next) || newCost < _astarCostSoFar[next])
        {
            _astarCostSoFar[next] = newCost;
            int priority = newCost + GetHeuristic(next, end);
            _astarFrontier.Enqueue(next, priority);
            _astarCameFrom[next] = current;
        }
    }

    // 3. Helper Logic

    /// <summary>
    /// [New] 포탈의 목적지 후보들을 순회하며 실제 이동 가능한 첫 번째 좌표를 반환합니다.
    /// </summary>
    private static GridCoords? GetValidPortalExit(Tile tile, MapManager map)
    {
        if (tile.PortalData == null || tile.PortalData.Destinations == null) return null;

        // [Fix] foreach 타입 추론 (PortalDestination)
        foreach (var destEntry in tile.PortalData.Destinations)
        {
            // 구조체에서 좌표 추출
            GridCoords target = destEntry.Coordinate;

            // 1. 맵 범위 및 타일 존재 확인
            if (!map.HasTile(target)) continue;

            // 2. 유닛 점유 확인
            if (map.HasUnit(target)) continue;

            // 3. 타일 Walkable 확인
            Tile destTile = map.GetTile(target);
            if (destTile != null && destTile.IsWalkable)
            {
                return target; // 유효한 좌표 반환
            }
        }

        return null;
    }
    private static bool IsValidMove(GridCoords current, Direction dir, MapManager map, out GridCoords nextCoords, bool ignoreWalkability = false)
    {
        nextCoords = GridUtils.GetNeighbor(current, dir);
        Tile nextTile = map.GetTile(nextCoords);

        if (nextTile == null) return false;

        // 1. 현재 타일의 에지 체크
        Tile currentTile = map.GetTile(current);
        if (currentTile != null && currentTile.IsPathBlockedByEdge(dir)) return false;

        // 2. 다음 타일 Walkable 여부
        if (!ignoreWalkability && !nextTile.IsWalkable) return false;

        // 3. 다음 타일의 에지 체크
        Direction oppositeDir = GridUtils.GetOppositeDirection(dir);
        if (nextTile.IsPathBlockedByEdge(oppositeDir)) return false;

        return true;
    }

    private static int GetHeuristic(GridCoords a, GridCoords b)
    {
        // [Fix] 3D 맨해튼 거리 (Y축 추가)
        return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) + Math.Abs(a.z - b.z);
    }

    private static List<GridCoords> RetracePath(Dictionary<GridCoords, GridCoords> cameFrom, GridCoords start, GridCoords end)
    {
        var path = new List<GridCoords>();
        GridCoords current = end;

        // 안전장치: 경로가 끊겨있을 경우 무한루프 방지
        int safetyCount = 0;
        int maxDepth = 1000;

        while (!current.Equals(start))
        {
            path.Add(current);

            if (!cameFrom.ContainsKey(current))
            {
                // 경로 추적 실패 (버그 상황)
                Debug.LogError($"Path retrace failed. No history for {current}");
                return new List<GridCoords>();
            }

            current = cameFrom[current];

            if (++safetyCount > maxDepth)
            {
                Debug.LogError("Path retrace Infinite Loop Detected!");
                break;
            }
        }
        path.Reverse();
        return path;
    }
}