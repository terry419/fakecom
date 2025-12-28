using UnityEngine;
using System;

// [GDD 5.6] 파일에 저장되는 엣지(벽) 정보
[Serializable]
public struct SavedEdgeInfo
{
    public EdgeType Type;      // Wall, Window, Door...
    public CoverType Cover;    // None, Low, High
    public float MaxHP;
    public float CurrentHP;
    public EdgeDataType DataType; 

    public SavedEdgeInfo(EdgeType type, CoverType cover, float maxHP, EdgeDataType dataType = EdgeDataType.None)
    {
        Type = type;
        Cover = cover;
        MaxHP = maxHP;
        CurrentHP = maxHP;
        DataType = dataType;
    }

    public EdgeInfo ToEdgeInfo()
    {
        return new EdgeInfo
        {
            Type = Type,
            Cover = Cover,
            MaxHP = MaxHP,
            CurrentHP = CurrentHP,
            DataType = DataType // 저장된 재질 복원
        };
    }
}

// [GDD 5.6] 타일 저장 데이터 (희소 배열용)
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

// [수정됨] TeamType Enum 사용
[Serializable]
public struct SpawnPointData
{
    public Vector3 Position;
    public TeamType Team;    // 0, 1 대신 명확한 Enum 사용
    [Range(0f, 360f)]
    public float YRotation;
}