using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

/// <summary>
/// [GDD 5.6] 맵의 최소 단위 (Logic Class). 
/// 렌더링이나 물리 엔진 없이 순수 데이터와 상태 로직을 관리합니다.
/// </summary>
public class Tile
{
    // ==================================================================================
    // 1. 기본 데이터 & 이벤트
    // ==================================================================================
    public GridCoords Coordinate { get; private set; }
    public FloorType FloorID { get; private set; }
    public PillarType PillarID { get; private set; }

    private EdgeInfo[] _edges = new EdgeInfo[4];

    // [이벤트] 엣지 파괴 및 손상
    public event Action<Direction, EdgeInfo> OnEdgeDestroyed;
    public event Action<Direction, EdgeInfo> OnEdgeDamaged;

    // [이벤트] 이동 가능 여부 변경 (Pathfinder 갱신용)
    public event Action<Tile> OnWalkableStatusChanged;


    // ==================================================================================
    // 2. 점유 슬롯 (분리형 & 최적화)
    // ==================================================================================
    private ITileOccupant _primaryUnit;
    private List<ITileOccupant> _items = new();
    private List<ITileOccupant> _obstacles = new();

    // [최적화] 읽기 전용 래퍼 캐싱
    private ReadOnlyCollection<ITileOccupant> _readOnlyItems;

    // [캐시] 이동 가능 여부
    private bool _cachedIsWalkable = true;


    // ==================================================================================
    // 3. 생성자
    // ==================================================================================
    public Tile(GridCoords coords, FloorType floorType, PillarType pillarType = PillarType.None)
    {
        Coordinate = coords;
        FloorID = floorType;
        PillarID = pillarType;

        // 기본 엣지: Open
        for (int i = 0; i < 4; i++) _edges[i] = EdgeInfo.Open;

        // 리스트 래퍼 초기화 (GC 감소)
        _readOnlyItems = _items.AsReadOnly();

        UpdateCache();
    }


    // ==================================================================================
    // 4. 조회 (Getters)
    // ==================================================================================
    public bool IsWalkable => _cachedIsWalkable;
    public ITileOccupant PrimaryUnit => _primaryUnit;

    /// <summary>
    /// [주의] 현재 타일의 아이템 목록 뷰를 반환합니다. (O(1))
    /// 반환된 리스트를 foreach로 순회하는 도중 Add/RemoveOccupant가 호출되면 예외가 발생할 수 있습니다.
    /// 안전한 순회가 필요하다면 GetItemsCopy()를 사용하세요.
    /// </summary>
    public IReadOnlyList<ITileOccupant> Items => _readOnlyItems;

    /// <summary>
    /// [안전] 아이템 리스트의 복사본을 생성하여 반환합니다. (O(N))
    /// 순회 중 리스트 변경이 예상될 때 사용하세요.
    /// </summary>
    public List<ITileOccupant> GetItemsCopy() => new List<ITileOccupant>(_items);

    public EdgeInfo GetEdge(Direction dir) => _edges[(int)dir];


    // ==================================================================================
    // 5. 엣지 조작 (Logic Optimized)
    // ==================================================================================
    public void SetEdge(Direction dir, EdgeInfo newInfo)
    {
        _edges[(int)dir] = newInfo;
    }

    // [개선 1] 중복 대입 방지 및 로직 최적화
    public void DamageEdge(Direction dir, float damage)
    {
        EdgeInfo oldEdge = _edges[(int)dir];
        EdgeInfo newEdge = oldEdge.WithDamage(damage);

        // 1. 파괴됨 (이번 데미지로 인해 HP가 0이 됨)
        if (newEdge.IsDestroyed && oldEdge.CurrentHP > 0)
        {
            // 최종 상태(개방)로 바로 설정
            _edges[(int)dir] = EdgeInfo.Open;
            OnEdgeDestroyed?.Invoke(dir, oldEdge);
        }
        // 2. 손상됨 (아직 살아있음)
        else if (newEdge.CurrentHP < oldEdge.CurrentHP)
        {
            _edges[(int)dir] = newEdge;
            OnEdgeDamaged?.Invoke(dir, newEdge);
        }
        // 3. 변화 없음 (이미 파괴되었거나 데미지가 0)
    }


    // ==================================================================================
    // 6. 점유 조작 (Exception Msg & Safety)
    // ==================================================================================
    public void AddOccupant(ITileOccupant occupant)
    {
        if (occupant == null) return;

        switch (occupant.Type)
        {
            case OccupantType.Unit:
                // [개선 2] 예외 메시지 강화
                if (_primaryUnit != null)
                {
                    throw new InvalidOperationException(
                        $"Cannot add unit to {Coordinate}: already occupied by {_primaryUnit}. " +
                        $"Remove existing unit first using RemoveOccupant().");
                }
                _primaryUnit = occupant;
                break;

            case OccupantType.Item:
                _items.Add(occupant);
                break;

            case OccupantType.Obstacle:
                _obstacles.Add(occupant);
                break;

            default:
                Debug.LogWarning($"Unknown occupant type: {occupant.Type}");
                return;
        }

        occupant.OnBlockingChanged += HandleOccupantStateChange;

        // OnCoverChanged는 Tile 로직(이동가능성)에 영향이 없으므로 구독하지 않음.
        // 전술 매니저 등에서 직접 구독할 것.

        UpdateCache();
        occupant.OnAddedToTile(this);
    }

    public void RemoveOccupant(ITileOccupant occupant)
    {
        if (occupant == null)
        {
            Debug.LogWarning($"RemoveOccupant: Occupant is null at {Coordinate}");
            return;
        }

        bool removed = false;

        switch (occupant.Type)
        {
            case OccupantType.Unit:
                if (_primaryUnit == occupant)
                {
                    _primaryUnit = null;
                    removed = true;
                }
                else
                {
                    Debug.LogWarning($"RemoveOccupant: Unit mismatch or empty at {Coordinate}");
                }
                break;

            case OccupantType.Item:
                removed = _items.Remove(occupant);
                if (!removed) Debug.LogWarning($"RemoveOccupant: Item not found at {Coordinate}");
                break;

            case OccupantType.Obstacle:
                removed = _obstacles.Remove(occupant);
                if (!removed) Debug.LogWarning($"RemoveOccupant: Obstacle not found at {Coordinate}");
                break;

            default:
                Debug.LogError($"RemoveOccupant: Unknown type {occupant.Type}");
                return;
        }

        if (removed)
        {
            occupant.OnBlockingChanged -= HandleOccupantStateChange;
            UpdateCache();
            occupant.OnRemovedFromTile(this);
        }
    }


    // ==================================================================================
    // 7. 내부 로직
    // ==================================================================================
    private void HandleOccupantStateChange(bool isBlocking)
    {
        UpdateCache();
    }

    private void UpdateCache()
    {
        bool oldState = _cachedIsWalkable;

        bool unitBlocking = _primaryUnit != null && _primaryUnit.IsBlockingMovement;
        bool obstacleBlocking = _obstacles.Any(o => o.IsBlockingMovement);

        _cachedIsWalkable = !unitBlocking && !obstacleBlocking;

        if (oldState != _cachedIsWalkable)
        {
            OnWalkableStatusChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// [비평가 반영] 저장된 데이터(SaveData)로부터 타일 상태를 복원
    /// </summary>
    public void LoadFromSaveData(TileSaveData saveData)
    {
        // 1. 기본 정보 복원
        this.Coordinate = saveData.Coords;
        this.FloorID = saveData.FloorID;
        this.PillarID = saveData.PillarID;

        // 2. 엣지(벽) 정보 복원
        if (saveData.Edges != null && saveData.Edges.Length == 4)
        {
            for (int i = 0; i < 4; i++)
            {
                // SavedEdgeInfo -> EdgeInfo 변환 (DataType 포함됨)
                _edges[i] = saveData.Edges[i].ToEdgeInfo();
            }
        }

        // 3. 캐시 갱신
        UpdateCache();
    }

    public override string ToString() => $"Tile {Coordinate} [{FloorID}]";


}