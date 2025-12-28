using UnityEngine;
using System;

// [GDD 5.6] ���Ͽ� ����Ǵ� ����(��) ����
[Serializable]
public struct SavedEdgeInfo
{
    public EdgeType Type;      // Wall, Window, Door...
    public CoverType Cover;    // None, Low, High
    public float MaxHP;
    public float CurrentHP;
    public EdgeDataType DataType; 

    private SavedEdgeInfo(EdgeType type, CoverType cover, float maxHP, float currentHP, EdgeDataType dataType)
    {
        Type = type;
        Cover = cover;
        MaxHP = maxHP;
        CurrentHP = currentHP;
        DataType = dataType;
    }

    public static SavedEdgeInfo CreateOpen()
    {
        // An open edge has no HP, no cover, and no material data
        return new SavedEdgeInfo(EdgeType.Open, CoverType.None, 0, 0, EdgeDataType.None);
    }

    public static SavedEdgeInfo CreateWall(EdgeDataType wallMaterial, float maxHP = 100f, CoverType cover = CoverType.High)
    {
        // A wall must have a material type, and defaults to High cover and some HP
        if (wallMaterial == EdgeDataType.None)
        {
            Debug.LogWarning("Creating a wall without a specific EdgeDataType. Defaulting to ConcreteWall.");
            wallMaterial = EdgeDataType.Concrete; // Fallback
        }
        return new SavedEdgeInfo(EdgeType.Wall, cover, maxHP, maxHP, wallMaterial);
    }
    
    public static SavedEdgeInfo CreateWindow(EdgeDataType windowMaterial = EdgeDataType.Glass, float maxHP = 30f, CoverType cover = CoverType.Low)
    {
        if (windowMaterial == EdgeDataType.None) windowMaterial = EdgeDataType.Glass;
        return new SavedEdgeInfo(EdgeType.Window, cover, maxHP, maxHP, windowMaterial);
    }

    public static SavedEdgeInfo CreateDoor(EdgeDataType doorMaterial = EdgeDataType.Wood, float maxHP = 50f, CoverType cover = CoverType.None)
    {
        if (doorMaterial == EdgeDataType.None) doorMaterial = EdgeDataType.Wood;
        return new SavedEdgeInfo(EdgeType.Door, cover, maxHP, maxHP, doorMaterial);
    }

    public EdgeInfo ToEdgeInfo()
    {
        return new EdgeInfo
        {
            Type = Type,
            Cover = Cover,
            MaxHP = MaxHP,
            CurrentHP = CurrentHP,
            DataType = DataType // ����� ���� ����
        };
    }
}

// [GDD 5.6] Ÿ�� ���� ������ (��� �迭��)
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

// [������] TeamType Enum ���
[Serializable]
public struct SpawnPointData
{
    public Vector3 Position;
    public TeamType Team;    // 0, 1 ��� ��Ȯ�� Enum ���
    [Range(0f, 360f)]
    public float YRotation;
}