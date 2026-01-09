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
    // ========================================================================
    // 1. 에디터 저장 데이터 (Static Data from Editor)
    // ========================================================================
    public PortalType Type;        // In, Out, Both
    public string LinkID;          // 연결 식별자
    public Direction ExitFacing;   // (Out인 경우) 나갈 때 바라볼 방향

    // ========================================================================
    // 2. 런타임 계산 데이터 (Runtime Data)
    // ========================================================================
    public List<PortalDestination> Destinations = new List<PortalDestination>();

    public int MovementCost = 1;

    public PortalInfo Clone()
    {
        return new PortalInfo
        {
            Type = this.Type,
            LinkID = this.LinkID,
            ExitFacing = this.ExitFacing,
            MovementCost = this.MovementCost,
            Destinations = (this.Destinations != null) ? new List<PortalDestination>(this.Destinations) : new List<PortalDestination>()
        };
    }
}

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

    // [Fix] 주석 해제: Tile.cs에서 참조 중이므로 반드시 필요함
    public float CurrentPillarHP;

    public SpawnType SpawnType;

    public SavedEdgeInfo[] Edges;

    public string RoleTag;

    // 포탈 데이터
    public PortalInfo PortalData;

    public void InitializeEdges()
    {
        if (Edges == null || Edges.Length != 4)
            Edges = new SavedEdgeInfo[4];
    }
}

[System.Serializable]
public struct SpawnPointData
{
    public GridCoords Coordinate;
    public string RoleTag;
}