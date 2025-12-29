using UnityEngine;
using System;

// [GDD 5.6] 맵 파일에 저장되는 엣지(벽) 데이터
[Serializable]
public struct SavedEdgeInfo
{
    public EdgeType Type;      // Wall, Window, Door...
    public CoverType Cover;    // None, Low, High
    public float MaxHP;
    public float CurrentHP;
    public EdgeDataType DataType;

    public SavedEdgeInfo(EdgeType type, CoverType cover, float maxHP, float currentHP, EdgeDataType dataType)
    {
        Type = type;
        Cover = cover;
        MaxHP = maxHP;
        CurrentHP = currentHP;
        DataType = dataType;
    }

    // ========================================================================
    // [Fix] EditorTile 등 기존 툴 호환을 위한 래퍼(Wrapper) 메서드 복구
    // * 기본값은 EdgeFactory의 상수를 참조하여 일관성 유지 *
    // ========================================================================

    public static SavedEdgeInfo CreateOpen()
    {
        return EdgeFactory.CreateOpen();
    }

    // EditorTile에서 (DataType, MaxHP, Cover)를 인자로 넘기므로 이를 받아주는 메서드가 필요합니다.
    public static SavedEdgeInfo CreateWall(EdgeDataType material, float maxHP = EdgeFactory.HP_WALL, CoverType cover = EdgeFactory.COVER_WALL)
    {
        if (material == EdgeDataType.None) material = EdgeDataType.Concrete;
        return new SavedEdgeInfo(EdgeType.Wall, cover, maxHP, maxHP, material);
    }

    public static SavedEdgeInfo CreateWindow(EdgeDataType material, float maxHP = EdgeFactory.HP_WINDOW, CoverType cover = EdgeFactory.COVER_WINDOW)
    {
        if (material == EdgeDataType.None) material = EdgeDataType.Glass;
        return new SavedEdgeInfo(EdgeType.Window, cover, maxHP, maxHP, material);
    }

    public static SavedEdgeInfo CreateDoor(EdgeDataType material, float maxHP = EdgeFactory.HP_DOOR, CoverType cover = EdgeFactory.COVER_DOOR)
    {
        if (material == EdgeDataType.None) material = EdgeDataType.Wood;
        return new SavedEdgeInfo(EdgeType.Door, cover, maxHP, maxHP, material);
    }

    // ========================================================================

    public EdgeInfo ToEdgeInfo()
    {
        return new EdgeInfo
        {
            Type = Type,
            Cover = Cover,
            MaxHP = MaxHP,
            CurrentHP = CurrentHP,
            DataType = DataType
        };
    }
}

// [GDD 5.6] 타일 저장 데이터
[Serializable]
public struct TileSaveData
{
    public GridCoords Coords;
    public FloorType FloorID;
    public PillarType PillarID;

    [Tooltip("0:North, 1:East, 2:South, 3:West")]
    public SavedEdgeInfo[] Edges;

    public void InitializeEdges()
    {
        if (Edges == null || Edges.Length != 4)
            Edges = new SavedEdgeInfo[4];
    }
}

// 스폰 포인트 데이터
[Serializable]
public struct SpawnPointData
{
    public Vector3 Position;
    public TeamType Team;
    [Range(0f, 360f)]
    public float YRotation;
}