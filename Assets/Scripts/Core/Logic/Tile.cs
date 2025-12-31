using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class Tile
{
    // ==================================================================================
    // 1. 기본 속성
    // ==================================================================================
    public GridCoords Coordinate { get; private set; }
    public FloorType FloorID { get; private set; }

    // 기둥 관련 속성 (전역 Enum 사용)
    public PillarType PillarID { get; private set; }
    private float _currentPillarHP;
    private float _maxPillarHP;

    private EdgeInfo[] _edges = new EdgeInfo[4];

    // ==================================================================================
    // 2. 이벤트 정의
    // ==================================================================================
    public event Action<PillarType, PillarType> OnPillarStateChanged; // (Old, New)
    public event Action<Direction, EdgeInfo> OnEdgeDestroyed;
    public event Action<Direction, EdgeInfo> OnEdgeDamaged;
    public event Action<Tile> OnWalkableStatusChanged;

    // ==================================================================================
    // 3. 점유자 관리
    // ==================================================================================
    private ITileOccupant _primaryUnit;
    private List<ITileOccupant> _items = new();
    private List<ITileOccupant> _obstacles = new();

    private bool _cachedIsWalkable = true;
    public bool IsWalkable => _cachedIsWalkable;

    // ==================================================================================
    // 4. 생성자 및 초기화
    // ==================================================================================
    public Tile(GridCoords coords, FloorType floorType, PillarType pillarType)
    {
        Coordinate = coords;
        FloorID = floorType;
        PillarID = pillarType;

        // 초기화: 벽 없음(Open)
        for (int i = 0; i < 4; i++) _edges[i] = EdgeInfo.Open;

        // Pillar HP는 초기화 시 0, 이후 InitializePillarHP로 주입
        _maxPillarHP = 0f;
        _currentPillarHP = 0f;

        UpdateCache();
    }

    public void InitializePillarHP(float maxHP, float currentHP = -1f)
    {
        _maxPillarHP = maxHP;
        _currentPillarHP = currentHP >= 0 ? currentHP : maxHP;
        UpdateCache();
    }

    // ==================================================================================
    // 5. 파괴 및 상태 변화 로직
    // ==================================================================================

    public void DamagePillar(float damage)
    {
        if (PillarID == PillarType.None || PillarID == PillarType.Debris) return;

        _currentPillarHP -= damage;

        PillarType oldState = PillarID;
        PillarType newState = GetNextPillarState();

        if (newState != oldState)
        {
            PillarID = newState;
            OnPillarStateChanged?.Invoke(oldState, newState);
            UpdateCache();
        }
    }

    private PillarType GetNextPillarState()
    {
        float hpPercent = _maxPillarHP > 0
            ? Mathf.Max(0f, _currentPillarHP / _maxPillarHP)
            : 0f;

        // 상태 전이 규칙: Standing -> Broken -> Debris
        if (hpPercent > EdgeConstants.PILLAR_BROKEN_THRESHOLD)
            return PillarType.Standing;
        else if (hpPercent > 0f)
            return PillarType.Broken;
        else
            return PillarType.Debris;
    }

    public void DamageEdge(Direction dir, float damage)
    {
        int idx = (int)dir;
        EdgeInfo oldEdge = _edges[idx];
        EdgeInfo newEdge = oldEdge.WithDamage(damage);

        if (newEdge.IsDestroyed && oldEdge.CurrentHP > 0)
        {
            _edges[idx] = EdgeInfo.Open;
            OnEdgeDestroyed?.Invoke(dir, oldEdge);
        }
        else if (newEdge.CurrentHP < oldEdge.CurrentHP)
        {
            _edges[idx] = newEdge;
            OnEdgeDamaged?.Invoke(dir, newEdge);
        }
    }

    // ==================================================================================
    // 6. 캐시 갱신
    // ==================================================================================
    private void UpdateCache()
    {
        bool oldState = _cachedIsWalkable;

        // 기둥이 있고 잔해가 아니면 차단
        bool pillarBlocking = (PillarID != PillarType.None && PillarID != PillarType.Debris);
        bool unitBlocking = _primaryUnit != null && _primaryUnit.IsBlockingMovement;
        bool obstacleBlocking = _obstacles.Any(o => o.IsBlockingMovement);

        _cachedIsWalkable = !pillarBlocking && !unitBlocking && !obstacleBlocking;

        if (oldState != _cachedIsWalkable)
        {
            OnWalkableStatusChanged?.Invoke(this);
        }
    }

    // ==================================================================================
    // 7. 데이터 로드/저장 및 접근자
    // ==================================================================================
    public void LoadFromSaveData(TileSaveData saveData)
    {
        this.Coordinate = saveData.Coords;
        this.FloorID = saveData.FloorID;
        this.PillarID = saveData.PillarID;

        if (saveData.Edges != null && saveData.Edges.Length == 4)
        {
            for (int i = 0; i < 4; i++)
                _edges[i] = saveData.Edges[i].ToEdgeInfo();
        }
        UpdateCache();
    }

    public void AddOccupant(ITileOccupant occupant)
    {
        if (occupant == null) return;
        switch (occupant.Type)
        {
            case OccupantType.Unit:
                if (_primaryUnit != null) throw new InvalidOperationException($"Tile {Coordinate} occupied.");
                _primaryUnit = occupant;
                break;
            case OccupantType.Item: _items.Add(occupant); break;
            case OccupantType.Obstacle: _obstacles.Add(occupant); break;
        }
        occupant.OnBlockingChanged += HandleOccupantStateChange;
        UpdateCache();
        occupant.OnAddedToTile(this);
    }

    public void RemoveOccupant(ITileOccupant occupant)
    {
        if (occupant == null) return;
        bool removed = false;
        switch (occupant.Type)
        {
            case OccupantType.Unit:
                if (_primaryUnit == occupant) { _primaryUnit = null; removed = true; }
                break;
            case OccupantType.Item: removed = _items.Remove(occupant); break;
            case OccupantType.Obstacle: removed = _obstacles.Remove(occupant); break;
        }

        if (removed)
        {
            occupant.OnBlockingChanged -= HandleOccupantStateChange;
            UpdateCache();
            occupant.OnRemovedFromTile(this);
        }
    }

    private void HandleOccupantStateChange(bool isBlocking) => UpdateCache();
    public EdgeInfo GetEdge(Direction dir) => _edges[(int)dir];
    public void SetEdge(Direction dir, EdgeInfo info) => _edges[(int)dir] = info;
}