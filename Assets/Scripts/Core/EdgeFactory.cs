using UnityEngine;

// [Refactoring] 벽/창문 생성 로직 및 기본값을 한곳에서 관리 (Factory Pattern)
public static class EdgeFactory
{
    // ========================================================================
    // 1. 기본 밸런스 데이터 (Single Source of Truth)
    // ========================================================================
    public const float HP_WALL = 100f;
    public const float HP_WINDOW = 30f;
    public const float HP_DOOR = 50f;

    public const CoverType COVER_WALL = CoverType.High;
    public const CoverType COVER_WINDOW = CoverType.Low;
    public const CoverType COVER_DOOR = CoverType.None;

    // ========================================================================
    // 2. 생성 팩토리 메서드
    // ========================================================================

    public static SavedEdgeInfo CreateOpen()
    {
        // 뚫린 공간 (데이터 없음)
        return new SavedEdgeInfo(EdgeType.Open, CoverType.None, 0, 0, EdgeDataType.None);
    }

    public static SavedEdgeInfo CreateWall(EdgeDataType material)
    {
        // 기본값 보정
        if (material == EdgeDataType.None)
        {
            Debug.LogWarning("[EdgeFactory] Wall material missing. Defaulting to Concrete.");
            material = EdgeDataType.Concrete;
        }

        return new SavedEdgeInfo(EdgeType.Wall, COVER_WALL, HP_WALL, HP_WALL, material);
    }

    public static SavedEdgeInfo CreateWindow(EdgeDataType material)
    {
        if (material == EdgeDataType.None) material = EdgeDataType.Glass;
        return new SavedEdgeInfo(EdgeType.Window, COVER_WINDOW, HP_WINDOW, HP_WINDOW, material);
    }

    public static SavedEdgeInfo CreateDoor(EdgeDataType material)
    {
        if (material == EdgeDataType.None) material = EdgeDataType.Wood;
        return new SavedEdgeInfo(EdgeType.Door, COVER_DOOR, HP_DOOR, HP_DOOR, material);
    }
}