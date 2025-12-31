using UnityEngine;

// [Refactoring] 전역 Enum을 사용하도록 수정
public static class EdgeFactory
{
    public static SavedEdgeInfo CreateOpen()
    {
        return SavedEdgeInfo.CreateOpen();
    }

    public static SavedEdgeInfo CreateWall()
    {
        // EdgeConstants 상수를 참조하여 생성
        return new SavedEdgeInfo(
            EdgeType.Wall,
            CoverType.High,
            EdgeConstants.HP_WALL,
            EdgeConstants.HP_WALL
        );
    }

    public static SavedEdgeInfo CreateWindow()
    {
        return new SavedEdgeInfo(
            EdgeType.Window,
            CoverType.Low,
            EdgeConstants.HP_WINDOW,
            EdgeConstants.HP_WINDOW
        );
    }

    public static SavedEdgeInfo CreateDoor()
    {
        return new SavedEdgeInfo(
            EdgeType.Door,
            CoverType.None,
            EdgeConstants.HP_DOOR,
            EdgeConstants.HP_DOOR
        );
    }
}