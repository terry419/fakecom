using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [GDD 1.3, 5.1, 5.6, 6.3] 그리드 수학 및 유틸리티.
/// </summary>
public static class GridUtils
{
    // ==================================================================================
    // 1. 상수 정의 (GDD 5.1 Height Specs) - [전량 복구됨]
    // ==================================================================================
    public const float CELL_SIZE = 1.0f;
    public const float LEVEL_HEIGHT = 2.5f;
    public const float FLOOR_OFFSET = 0.2f;

    public const float HALF_LEVEL_THRESHOLD = 1.25f; // 층간 구분 임계값
    public const float UNIT_HEIGHT = 2.0f;           // 유닛 키
    public const float SHOOT_ORIGIN_HEIGHT = 1.8f;   // 사격 원점 (눈 높이)
    public const float LOW_COVER_HEIGHT = 1.2f;      // 반엄폐 높이
    public const float HIGH_COVER_HEIGHT = 2.5f;     // 완전엄폐 높이

    // ==================================================================================
    // 2. 좌표 변환
    // ==================================================================================
    public static Vector3 GridToWorld(GridCoords coords)
    {
        float worldX = coords.x * CELL_SIZE;
        float worldY = (coords.y * LEVEL_HEIGHT) + FLOOR_OFFSET;
        float worldZ = coords.z * CELL_SIZE;
        return new Vector3(worldX, worldY, worldZ);
    }

    public static GridCoords WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / CELL_SIZE);
        int z = Mathf.RoundToInt(worldPos.z / CELL_SIZE);
        float adjustedY = worldPos.y - FLOOR_OFFSET;
        int y = Mathf.RoundToInt(adjustedY / LEVEL_HEIGHT);
        return new GridCoords(x, z, y);
    }

    // [복구됨] 엣지(벽)의 월드 좌표 반환 (비주얼/이펙트용)
    public static Vector3 GetEdgeWorldPosition(GridCoords coords, Direction dir)
    {
        Vector3 center = GridToWorld(coords);
        float offset = CELL_SIZE * 0.5f;
        switch (dir)
        {
            case Direction.North: return center + new Vector3(0, 0, offset);
            case Direction.East: return center + new Vector3(offset, 0, 0);
            case Direction.South: return center + new Vector3(0, 0, -offset);
            case Direction.West: return center + new Vector3(-offset, 0, 0);
            default: return center;
        }
    }

    // ==================================================================================
    // 3. 거리 및 이동 비용 계산 (비평가 피드백 통합)
    // ==================================================================================

    // [복구됨] 스킬 사거리 판정 (유클리드 거리 제곱 비교)
    public static bool IsInRange(GridCoords a, GridCoords b, float range)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        float distanceSquared = (dx * dx) + (dz * dz);
        return distanceSquared <= (range * range);
    }

    // [복구 & 수정] A* 휴리스틱용 맨해튼 거리 (기존 GetManhattanDistance와 동일 역할)
    public static int GetHeuristicDistance(GridCoords from, GridCoords to)
    {
        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.z - to.z) + Mathf.Abs(from.y - to.y);
    }

    // [신규] 인접 타일 간 실제 이동 비용 (G값). 비평가 요청 반영.
    public static int GetAdjacentMovementCost(GridCoords from, GridCoords to)
    {
        if (!IsAdjacent(from, to)) return -1; // 이동 불가

        int baseCost = 1; // 수평 이동 기본 비용
        int levelDiff = Mathf.Abs(from.y - to.y);

        // GDD 5.2: 층간 이동 시 높이 차이만큼 비용 추가 (순간이동 판정)
        return baseCost + levelDiff;
    }

    // ==================================================================================
    // 4. 방향 및 인접성 유틸리티 - [전량 복구됨]
    // ==================================================================================

    public static GridCoords GetNeighbor(GridCoords coords, Direction dir)
    {
        switch (dir)
        {
            case Direction.North: return new GridCoords(coords.x, coords.z + 1, coords.y);
            case Direction.East: return new GridCoords(coords.x + 1, coords.z, coords.y);
            case Direction.South: return new GridCoords(coords.x, coords.z - 1, coords.y);
            case Direction.West: return new GridCoords(coords.x - 1, coords.z, coords.y);
            default: return coords;
        }
    }

    public static Direction GetOppositeDirection(Direction dir)
    {
        return (Direction)(((int)dir + 2) % 4);
    }

    public static (GridCoords neighbor, Direction oppositeDir) GetOppositeEdge(GridCoords coords, Direction dir)
    {
        GridCoords neighbor = GetNeighbor(coords, dir);
        Direction opposite = GetOppositeDirection(dir);
        return (neighbor, opposite);
    }

    public static GridCoords GetDirectionVector(Direction dir)
    {
        switch (dir)
        {
            case Direction.North: return new GridCoords(0, 1, 0);
            case Direction.East: return new GridCoords(1, 0, 0);
            case Direction.South: return new GridCoords(0, -1, 0);
            case Direction.West: return new GridCoords(-1, 0, 0);
            default: return new GridCoords(0, 0, 0);
        }
    }

    // [신규] 논리적 인접 여부 판단 (비평가 요청 반영)
    public static bool IsAdjacent(GridCoords from, GridCoords to)
    {
        int dx = Mathf.Abs(from.x - to.x);
        int dz = Mathf.Abs(from.z - to.z);
        int dy = Mathf.Abs(from.y - to.y);

        // 수평 인접 (상하좌우 1칸) AND 같은 층
        bool horizontal = (dx + dz == 1) && (dy == 0);

        // 수직 인접 (제자리에서 층만 이동 - 엘리베이터/사다리 등)
        bool vertical = (dx == 0 && dz == 0 && dy >= 1);

        return horizontal || vertical;
    }
}