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

    // [Fix] RoleTag 추가 (스폰 로직 필수)
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

    // ========================================================================
    // 3. 캐싱된 상태
    // ========================================================================
    private bool _cachedIsWalkable = true;
    public bool IsWalkable => _cachedIsWalkable;
    public event Action<Tile> OnWalkableStatusChanged;

    // ========================================================================
    // 4. 초기화 및 로드
    // ========================================================================

    // [Fix] 생성자에 RoleTag 추가
    public Tile(GridCoords coords, FloorType floorID, PillarType pillarID, string roleTag = null)
    {
        Coordinate = coords;
        FloorID = floorID;
        InitialPillarID = pillarID;
        RoleTag = roleTag;
    }

    public void LoadFromSaveData(TileSaveData saveData)
    {
        Coordinate = saveData.Coords;
        FloorID = saveData.FloorID;
        InitialPillarID = saveData.PillarID;
        InitialPillarHP = saveData.CurrentPillarHP;
        RoleTag = saveData.RoleTag;

        // [Fix] SaveData 우선 적용
        if (!string.IsNullOrEmpty(saveData.RoleTag))
        {
            RoleTag = saveData.RoleTag;
        }

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
        if (edge == null) return false;
        return edge.IsBlocking;
    }
}