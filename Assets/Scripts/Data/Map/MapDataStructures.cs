using UnityEngine;
using System;

[Serializable]
public struct SavedEdgeInfo
{
    public EdgeType Type;
    public CoverType Cover;
    public float MaxHP;
    public float CurrentHP;

    public SavedEdgeInfo(EdgeType type, CoverType cover, float maxHP, float currentHP)
    {
        Type = type;
        Cover = cover;
        MaxHP = maxHP;
        CurrentHP = currentHP;
    }

    public static SavedEdgeInfo CreateOpen() => new SavedEdgeInfo(EdgeType.Open, CoverType.None, 0, 0);

    public static SavedEdgeInfo CreateWall(float hp = 100f, CoverType cover = CoverType.High)
        => new SavedEdgeInfo(EdgeType.Wall, cover, hp, hp);

    public static SavedEdgeInfo CreateWindow(float hp = 30f, CoverType cover = CoverType.Low)
        => new SavedEdgeInfo(EdgeType.Window, cover, hp, hp);

    public static SavedEdgeInfo CreateDoor(float hp = 50f, CoverType cover = CoverType.None)
        => new SavedEdgeInfo(EdgeType.Door, cover, hp, hp);

    // [Fix] 누락되었던 변환 메서드 복구
    public EdgeInfo ToEdgeInfo()
    {
        // EdgeInfo의 팩토리 메서드를 사용하여 변환
        return EdgeInfo.CreateDamaged(Type, Cover, CurrentHP, MaxHP);
    }
}

[Serializable]
public struct TileSaveData
{
    public GridCoords Coords;
    public FloorType FloorID;
    public PillarType PillarID;
    public float CurrentPillarHP;
    public SavedEdgeInfo[] Edges;

    // [New] 타일 자체에 역할 태그 부여 (SSOT: 데이터 불일치 방지)
    // 예: "PlayerSpawn", "EnemySpawn_Alpha", "Objective_A"
    public string RoleTag;

    public void InitializeEdges()
    {
        if (Edges == null || Edges.Length != 4)
            Edges = new SavedEdgeInfo[4];
    }
}

[Serializable]
public struct SpawnPointData
{
    public Vector3 Position;
    public TeamType Team;
    public float YRotation;
}