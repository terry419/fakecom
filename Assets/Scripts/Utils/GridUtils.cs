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
    public static int GetManhattanDistance(GridCoords a, GridCoords b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z) + Mathf.Abs(a.y - b.y);
    }

    // [신규] 인접 타일 간 실제 이동 비용 (G값). 비평가 요청 반영.
    public static int GetAdjacentMovementCost(GridCoords from, GridCoords to)
    {
        // 인접하지 않음 (층이 다르거나, 거리가 멀거나) -> 이동 불가
        if (!IsAdjacent(from, to)) return -1;

        // 같은 층 인접 타일 이동은 무조건 비용 1 (GDD 6.3.4 기본 원칙)
        return 1;
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
        // 1. 층이 다르면 아예 이웃이 아님 (자동 길찾기 단절 -> 유저가 상호작용으로 이동)
        if (from.y != to.y) return false;

        // 2. 같은 층 내에서 상하좌우 1칸 차이인지 확인
        int dx = Mathf.Abs(from.x - to.x);
        int dz = Mathf.Abs(from.z - to.z);

        return (dx + dz == 1);
    }
    public static float GetCellSize()
    {
        return CELL_SIZE;
    }
    public static Direction QuaternionToDirection(Quaternion rot)
    {
        // 로컬 Forward 벡터를 구함
        Vector3 forward = rot * Vector3.forward;

        // X축과 Z축 중 어느 쪽 성분이 더 큰지 비교하여 주축 결정
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
        {
            return forward.x > 0 ? Direction.East : Direction.West;
        }
        else
        {
            return forward.z > 0 ? Direction.North : Direction.South;
        }
    }
}