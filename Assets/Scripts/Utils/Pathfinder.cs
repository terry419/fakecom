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

            for (int i = 0; i < _directions.Length; i++)
            {
                // BFS를 수행하고 Walkable을 탐색
                if (!IsValidMove(current, _directions[i], map, out GridCoords next, ignoreWalkability: false)) continue;

                int newCost = currentCost + 1;
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
        }
        return results;
    }

    // 2. Find Path (A*) - [수정됨] 단일 함수 사용하고 GetNeighbor 재사용
    public static List<GridCoords> FindPath(GridCoords start, GridCoords end, MapManager map)
    {
        if (start.Equals(end)) return new List<GridCoords>();
        if (start.y != end.y) return null;
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

            for (int i = 0; i < _directions.Length; i++)
            {
                Direction dir = _directions[i];

                // [수정됨] GridUtils.TryGetNeighbor -> GridUtils.GetNeighbor로 변경
                // 인접 좌표만 얻어옴 (Walkable 체크는 IsValidMove 내부에서 map.GetTile을 통해 처리)
                GridCoords nextCheck = GridUtils.GetNeighbor(current, dir);
                bool isDestination = nextCheck.Equals(end);

                // 목적지 타일이면 Walkable 검사 스킵 (ignoreWalkability: true)
                if (!IsValidMove(current, dir, map, out GridCoords next, ignoreWalkability: isDestination)) continue;

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

    // 3. Helper
    private static bool IsValidMove(GridCoords current, Direction dir, MapManager map, out GridCoords nextCoords, bool ignoreWalkability = false)
    {
        nextCoords = GridUtils.GetNeighbor(current, dir);
        Tile nextTile = map.GetTile(nextCoords);

        if (nextTile == null) return false;

        // 1. 현재 타일의 에지 체크
        Tile currentTile = map.GetTile(current);
        if (currentTile != null && currentTile.IsPathBlockedByEdge(dir)) return false;

        // 2. 다음 타일 Walkable 여부 (ignoreWalkability가 true면 무시함)
        if (!ignoreWalkability && !nextTile.IsWalkable) return false;

        // 3. 다음 타일의 에지 체크
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
