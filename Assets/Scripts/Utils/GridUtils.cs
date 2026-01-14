using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [GDD 1.3, 5.1, 5.6, 6.3] 그리드 수학 및 유틸리티.
/// </summary>
public static class GridUtils
{
    // ==================================================================================
    // 1. 상수 정의 (GDD 5.1 Height Specs)
    // ==================================================================================
    public const float CELL_SIZE = 1.0f;
    public const float LEVEL_HEIGHT = 2.5f;
    public const float FLOOR_OFFSET = 0.2f;

    public const float HALF_LEVEL_THRESHOLD = 1.25f; // [복구] 고지대 판정 임계값
    public const float UNIT_HEIGHT = 2.0f;
    public const float SHOOT_ORIGIN_HEIGHT = 1.8f;
    public const float LOW_COVER_HEIGHT = 1.2f;
    public const float HIGH_COVER_HEIGHT = 2.5f;

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

    public static GridCoords WorldToGrid(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x / CELL_SIZE);
        int z = Mathf.RoundToInt(pos.z / CELL_SIZE);
        int y = Mathf.RoundToInt((pos.y - FLOOR_OFFSET) / LEVEL_HEIGHT);

        return new GridCoords(x, z, y);
    }

    // [복구] 삭제되어 컴파일 에러를 유발했던 메서드 복구
    // TilemapGenerator 등에서 이펙트나 벽 위치를 잡을 때 사용됨
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
    // 3. 거리 계산 (Distance Calculation)
    // ==================================================================================

    public static int GetManhattanDistance(GridCoords a, GridCoords b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z) + Mathf.Abs(a.y - b.y);
    }

    public static float GetWorldDistance(GridCoords a, GridCoords b)
    {
        return Vector3.Distance(GridToWorld(a), GridToWorld(b));
    }

    // [신규] 최적화된 거리 계산 (SqrMagnitude 사용) - 요청 반영
    public static float GetWorldDistanceSquared(GridCoords a, GridCoords b)
    {
        return (GridToWorld(a) - GridToWorld(b)).sqrMagnitude;
    }

    // ==================================================================================
    // 4. 고저차 및 방향 판정
    // ==================================================================================

    public static int GetHeightDifference(GridCoords a, GridCoords b)
    {
        return a.y - b.y;
    }

    // [수정] 단순 y 비교가 아닌, 실제 월드 높이 차이가 임계값을 넘는지 확인 (GDD 로직 복구)
    public static bool IsHighGround(GridCoords attacker, GridCoords target)
    {
        float attackerY = (attacker.y * LEVEL_HEIGHT);
        float targetY = (target.y * LEVEL_HEIGHT);

        // 공격자가 더 높고, 그 차이가 임계값(1.25m) 이상이어야 함
        return (attackerY - targetY) >= HALF_LEVEL_THRESHOLD;
    }

    /// <summary>
    /// 타겟 입장에서 공격자가 어느 방향(벽)에 있는지 반환 (엄폐 감지용)
    /// </summary>
    public static Direction GetIncomingAttackDirection(Vector3 attackerPos, Vector3 targetPos)
    {
        Vector3 dir = (attackerPos - targetPos).normalized;
        return Vector3ToDirection(dir);
    }

    /// <summary>
    /// 타겟(from) 입장에서 공격자(to)가 어느 방향에 있는가? (GridCoords 기반)
    /// </summary>
    public static Direction GetRelativeDirection(GridCoords from, GridCoords to)
    {
        int dx = to.x - from.x;
        int dz = to.z - from.z;

        if (Mathf.Abs(dx) > Mathf.Abs(dz))
        {
            return dx > 0 ? Direction.East : Direction.West;
        }
        else
        {
            return dz > 0 ? Direction.North : Direction.South;
        }
    }

    public static Direction Vector3ToDirection(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
        {
            return dir.x > 0 ? Direction.East : Direction.West;
        }
        else
        {
            return dir.z > 0 ? Direction.North : Direction.South;
        }
    }

    // ==================================================================================
    // 5. 인접 판정 유틸리티
    // ==================================================================================

    public static GridCoords GetNeighbor(GridCoords coords, Direction dir)
    {
        // [확인] GridCoords 생성자는 (x, z, y) 순서라고 가정함.
        // GetDirectionVector가 (x, z, 0)을 반환하므로 y값은 변하지 않음.
        GridCoords offset = GetDirectionVector(dir);
        return new GridCoords(coords.x + offset.x, coords.z + offset.z, coords.y + offset.y);
    }

    public static Direction GetOppositeDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.North: return Direction.South;
            case Direction.East: return Direction.West;
            case Direction.South: return Direction.North;
            case Direction.West: return Direction.East;
            default: return Direction.North;
        }
    }

    public static (GridCoords neighbor, Direction opposite) GetNeighborAndOpposite(GridCoords coords, Direction dir)
    {
        GridCoords neighbor = GetNeighbor(coords, dir);
        Direction opposite = GetOppositeDirection(dir);
        return (neighbor, opposite);
    }

    // [중요] GridCoords 구조체는 (x, z, y) 순서를 따른다고 가정합니다.
    // North = Z+1, East = X+1
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

    public static bool IsAdjacent(GridCoords from, GridCoords to)
    {
        if (from.y != to.y) return false;
        int dx = Mathf.Abs(from.x - to.x);
        int dz = Mathf.Abs(from.z - to.z);
        return (dx + dz == 1);
    }

    public static float GetCellSize()
    {
        return CELL_SIZE;
    }
}