// Assets/Scripts/Data/Map/MapDataStructures.cs

using UnityEngine;
using System;
using System.Collections.Generic;

// [New] 포탈 도착지 정보 (좌표 + 도착 시 바라볼 방향)
[Serializable]
public struct PortalDestination
{
    public GridCoords Coordinate;
    public Direction Facing; // 도착 후 유닛이 바라볼 방향

    public PortalDestination(GridCoords coords, Direction facing)
    {
        Coordinate = coords;
        Facing = facing;
    }
}

[Serializable]
public class PortalInfo
{
    // [Refactor] 목적지 정보에 '방향'을 포함하기 위해 구조체 변경
    // 기존: List<GridCoords> -> 변경: List<PortalDestination>
    public List<PortalDestination> Destinations = new List<PortalDestination>();

    public int MovementCost = 1;

    // [Refactor] Clone 메서드도 변경된 구조에 맞춰 수정
    public PortalInfo Clone()
    {
        return new PortalInfo
        {
            // 리스트 Deep Copy
            Destinations = (this.Destinations != null) ? new List<PortalDestination>(this.Destinations) : new List<PortalDestination>(),
            MovementCost = this.MovementCost
        };
    }
}

// ... (SavedEdgeInfo, TileSaveData, SpawnPointData 등 나머지 구조체는 기존 유지) ...

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
    public static SavedEdgeInfo CreateWall(float hp = 100f, CoverType cover = CoverType.High) => new SavedEdgeInfo(EdgeType.Wall, cover, hp, hp);
    public static SavedEdgeInfo CreateWindow(float hp = 30f, CoverType cover = CoverType.Low) => new SavedEdgeInfo(EdgeType.Window, cover, hp, hp);
    public static SavedEdgeInfo CreateDoor(float hp = 50f, CoverType cover = CoverType.None) => new SavedEdgeInfo(EdgeType.Door, cover, hp, hp);

    public EdgeInfo ToEdgeInfo()
    {
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

    public string RoleTag;

    // 포탈 데이터 (위에서 정의한 PortalInfo 사용)
    public PortalInfo PortalData;

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