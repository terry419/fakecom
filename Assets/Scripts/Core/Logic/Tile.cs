using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class Tile
{
    // ========================================================================
    // 1. 기본 식별 데이터
    // ========================================================================
    public GridCoords Coordinate { get; private set; }
    public FloorType FloorID { get; private set; }

    // [Fix] 복잡한 함정 로직 제거하고 원래대로 복구
    public string RoleTag { get; private set; }

    public PillarType InitialPillarID { get; private set; }
    public float InitialPillarHP { get; private set; }

    // ========================================================================
    // 2. 구성 요소
    // ========================================================================
    private RuntimeEdge[] _edges = new RuntimeEdge[4];
    public SavedEdgeInfo[] TempSavedEdges { get; private set; }

    private ITileOccupant _primaryUnit;
    private List<ITileOccupant> _occupants = new List<ITileOccupant>();

    public IReadOnlyList<ITileOccupant> Occupants => _occupants;

    // [Data] 런타임 포탈 정보
    public PortalInfo PortalData { get; private set; }

    // ========================================================================
    // 3. 캐싱된 상태
    // ========================================================================
    private bool _cachedIsWalkable = true;
    public bool IsWalkable => _cachedIsWalkable;
    public event Action<Tile> OnWalkableStatusChanged;

    // ========================================================================
    // 4. 초기화 및 로드
    // ========================================================================
    public Tile(GridCoords coords, FloorType floorID, PillarType pillarID, string roleTag = null)
    {
        Coordinate = coords;
        FloorID = floorID;
        InitialPillarID = pillarID;
        RoleTag = roleTag;

        // 포탈 데이터 초기화
        PortalData = null;
    }

    // [핵심 Fix] 외부에서 강제로 RoleTag를 주입할 수 있는 안전장치 메서드
    public void ForceSetRoleTag(string tag)
    {
        RoleTag = tag;
    }

    public void LoadFromSaveData(TileSaveData saveData)
    {
        Coordinate = saveData.Coords;
        FloorID = saveData.FloorID;
        InitialPillarID = saveData.PillarID;
        InitialPillarHP = saveData.CurrentPillarHP;

        // 1. RoleTag 로드 (빈 값 방지)
        if (!string.IsNullOrEmpty(saveData.RoleTag))
        {
            RoleTag = saveData.RoleTag;
        }

        // [핵심 Fix] 유령 포탈 제거 로직
        // PortalData가 null이 아니더라도, 내부의 LinkID가 비어있으면 "가짜/기본값"입니다.
        // 따라서 ID가 유효한 경우에만 데이터를 복제합니다.
        if (saveData.PortalData != null && !string.IsNullOrEmpty(saveData.PortalData.LinkID))
        {
            PortalData = saveData.PortalData.Clone();
        }
        else
        {
            PortalData = null; // 가짜 데이터는 null로 밀어버림
        }

        // 3. 엣지 데이터 로드
        if (saveData.Edges != null && saveData.Edges.Length == 4)
        {
            TempSavedEdges = saveData.Edges;
        }
        else
        {
            TempSavedEdges = new SavedEdgeInfo[4];
            for (int i = 0; i < 4; i++) TempSavedEdges[i] = SavedEdgeInfo.CreateOpen();
        }
        UpdateCache();
    }
    // ========================================================================
    // 5. 헬퍼 메서드 (Helper Methods) - 원본 유지
    // ========================================================================

    public bool HasActiveExits()
    {
        return PortalData != null &&
               PortalData.Destinations != null &&
               PortalData.Destinations.Count > 0;
    }

    public void SetSharedEdge(Direction dir, RuntimeEdge edge) => _edges[(int)dir] = edge;
    public RuntimeEdge GetEdge(Direction dir) => _edges[(int)dir];

    public void AddOccupant(ITileOccupant occupant)
    {
        if (occupant == null) return;
        if (_occupants.Contains(occupant)) return;

        if (occupant.Type == OccupantType.Unit)
        {
            if (_primaryUnit != null)
                Debug.LogWarning($"Tile {Coordinate} already has a unit!");
            _primaryUnit = occupant;
        }

        _occupants.Add(occupant);
        occupant.OnBlockingChanged += HandleOccupantStateChange;
        UpdateCache();
        occupant.OnAddedToTile(this);
    }

    public void RemoveOccupant(ITileOccupant occupant)
    {
        if (occupant == null) return;

        if (_occupants.Remove(occupant))
        {
            if (_primaryUnit == occupant) _primaryUnit = null;

            occupant.OnBlockingChanged -= HandleOccupantStateChange;
            UpdateCache();
            occupant.OnRemovedFromTile(this);
        }
    }

    private void HandleOccupantStateChange(bool isBlocking) => UpdateCache();

    public void UpdateCache()
    {
        bool oldState = _cachedIsWalkable;

        if (FloorID == FloorType.None || FloorID == FloorType.Void)
        {
            _cachedIsWalkable = false;
        }
        else
        {
            bool isBlocked = _occupants.Any(o => o.IsBlockingMovement);
            _cachedIsWalkable = !isBlocked;
        }

        if (oldState != _cachedIsWalkable)
        {
            OnWalkableStatusChanged?.Invoke(this);
        }
    }

    public bool IsPathBlockedByEdge(Direction dir)
    {
        var edge = GetEdge(dir);
        if (edge == null)
        {
            // Debug.LogWarning($"Tile {Coordinate}: GetEdge({dir})가 null을 반환했습니다. 에지 연결 실패!");
            return false;
        }
        return edge.IsBlocking;
    }
}