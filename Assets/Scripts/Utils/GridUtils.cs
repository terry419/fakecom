using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [GDD 1.3, 5.1, 5.6, 6.3]   ƿƼ.
/// </summary>
public static class GridUtils
{
    // ==================================================================================
    // 1.  
    // ==================================================================================
    public const float CELL_SIZE = 1.0f;
    public const float LEVEL_HEIGHT = 2.5f;
    public const float FLOOR_OFFSET = 0.2f;
    public const float HALF_LEVEL_THRESHOLD = 1.25f;

    public const float UNIT_HEIGHT = 2.0f;
    public const float SHOOT_ORIGIN_HEIGHT = 1.8f;
    public const float LOW_COVER_HEIGHT = 1.2f; 
    public const float HIGH_COVER_HEIGHT = 2.5f;

    // ==================================================================================
    // 2. ǥ ȯ
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

        // 50% Threshold  (Mathf.RoundToInt .5 ¦ ݿøϹǷ κ  )
        float adjustedY = worldPos.y - FLOOR_OFFSET;
        int y = Mathf.RoundToInt(adjustedY / LEVEL_HEIGHT);

        return new GridCoords(x, z, y);
    }

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
    // 3. 거리 계산 (GDD 6.3)
    // ==================================================================================
    /// <summary>
    /// [GDD 6.3] 유클리드 거리 기반 스킬 사거리 판정.
    /// 두 GridCoords 간의 평면(X,Z) 거리가 주어진 사거리(range) 내에 있는지 확인.
    /// 성능을 위해 제곱 거리로 비교하며, range는 타일 단위(float)로 받음.
    /// </summary>
    public static bool IsInRange(GridCoords a, GridCoords b, float range)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        float distanceSquared = (dx * dx) + (dz * dz);

        float rangeSquared = range * range;
        return distanceSquared <= rangeSquared;
    }

    // 길찾기 비용 (Y축 비용 1)
    public static int GetManhattanDistance(GridCoords a, GridCoords b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dz = Mathf.Abs(a.z - b.z);
        int dy = Mathf.Abs(a.y - b.y);
        return dx + dz + dy;
    }

    // ==================================================================================
    // 4. 방향 및 동기화 유틸리티
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

    // [추가] 인접 타일 정보 반환 : (이웃 좌표, 반대 방향) 튜플 리턴
    public static (GridCoords neighbor, Direction oppositeDir) GetOppositeEdge(GridCoords coords, Direction dir)
    {
        GridCoords neighbor = GetNeighbor(coords, dir);
        Direction opposite = GetOppositeDirection(dir);
        return (neighbor, opposite);
    }

    // [추가] 방향 벡터 반환 (그리드 좌표 기준)
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
}