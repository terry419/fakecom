using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// [Refactoring Phase 3] 데이터 컨테이너 및 상태 집계자 (State Aggregator)
// 책임: "내 위에 무엇이 있는가?", "그래서 지나갈 수 있는가?" 판단만 수행.
public class Tile
{
    // ========================================================================
    // 1. 기본 식별 데이터
    // ========================================================================
    public GridCoords Coordinate { get; private set; }
    public FloorType FloorID { get; private set; }

    // [Loading] EnvironmentManager에게 넘겨줄 임시 데이터들
    public PillarType InitialPillarID { get; private set; }

    // [Fix] 누락되었던 변수 추가: 저장된 기둥 체력을 임시 보관
    public float InitialPillarHP { get; private set; }

    // ========================================================================
    // 2. 구성 요소 (Edges & Occupants)
    // ========================================================================

    // [SSOT] 공유되는 벽 객체 (참조형 Class)
    private RuntimeEdge[] _edges = new RuntimeEdge[4];

    // [Loading] EnvironmentManager가 배선하기 전까지 엣지 데이터를 보관하는 장소
    public SavedEdgeInfo[] TempSavedEdges { get; private set; }

    // 점유자 목록 (유닛, 아이템, 그리고 승격된 기둥 객체)
    private ITileOccupant _primaryUnit;
    private List<ITileOccupant> _occupants = new List<ITileOccupant>();

    // ========================================================================
    // 3. 캐싱된 상태 (State Cache)
    // ========================================================================

    // 매번 리스트를 뒤지면 느리므로 이동 가능 여부를 캐싱
    private bool _cachedIsWalkable = true;
    public bool IsWalkable => _cachedIsWalkable;

    // 상태 변화 알림 이벤트 (길찾기 시스템 등이 구독)
    public event Action<Tile> OnWalkableStatusChanged;

    // ========================================================================
    // 4. 초기화 및 로드
    // ========================================================================

    public Tile(GridCoords coords, FloorType floorID, PillarType pillarID)
    {
        Coordinate = coords;
        FloorID = floorID;
        InitialPillarID = pillarID;
    }

    // MapManager가 호출: 파일 데이터를 임시 슬롯에 적재
    public void LoadFromSaveData(TileSaveData saveData)
    {
        Coordinate = saveData.Coords;
        FloorID = saveData.FloorID;

        // 기둥 데이터 임시 저장 (실제 객체 생성은 EnvironmentManager가 수행)
        InitialPillarID = saveData.PillarID;
        InitialPillarHP = saveData.CurrentPillarHP; // [Fix] 저장된 체력 값 로드

        // 엣지 데이터는 바로 적용하지 않고 임시 저장 (이웃과 공유 연결을 위해)
        if (saveData.Edges != null && saveData.Edges.Length == 4)
        {
            TempSavedEdges = saveData.Edges;
        }
        else
        {
            TempSavedEdges = new SavedEdgeInfo[4];
            for (int i = 0; i < 4; i++) TempSavedEdges[i] = SavedEdgeInfo.CreateOpen();
        }

        UpdateCache(); // 초기 상태 갱신
    }

    // ========================================================================
    // 5. 환경 설정 (EnvironmentManager 전용)
    // ========================================================================

    // [Wiring] EnvironmentManager가 공유된 엣지 객체를 꽂아주는 메서드
    public void SetSharedEdge(Direction dir, RuntimeEdge edge)
    {
        _edges[(int)dir] = edge;
    }

    public RuntimeEdge GetEdge(Direction dir) => _edges[(int)dir];

    // ========================================================================
    // 6. 점유자 관리 (Occupant System)
    // ========================================================================

    public void AddOccupant(ITileOccupant occupant)
    {
        if (occupant == null) return;

        // 유닛은 중복 방지를 위해 별도 변수로도 추적
        if (occupant.Type == OccupantType.Unit)
        {
            if (_primaryUnit != null)
                Debug.LogWarning($"Tile {Coordinate} already has a unit!");
            _primaryUnit = occupant;
        }

        _occupants.Add(occupant);

        // 점유자의 상태(차단 여부)가 변하면 내 캐시도 갱신하도록 구독
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

    // ========================================================================
    // 7. 핵심 로직: 이동 가능 여부 판단
    // ========================================================================

    // Q: 이 타일'로' 들어갈 수 있는가? (Center Check)
    public void UpdateCache()
    {
        bool oldState = _cachedIsWalkable;

        // 1. 바닥이 없으면(Void) 불가
        if (FloorID == FloorType.None || FloorID == FloorType.Void)
        {
            _cachedIsWalkable = false;
        }
        else
        {
            // 2. 점유자 중 하나라도 길을 막고 있으면 불가 (기둥 객체 포함)
            bool isBlocked = _occupants.Any(o => o.IsBlockingMovement);
            _cachedIsWalkable = !isBlocked;
        }

        if (oldState != _cachedIsWalkable)
        {
            OnWalkableStatusChanged?.Invoke(this);
        }
    }

    // Q: 이 타일'에서' 저 방향으로 나갈 수 있는가? (Edge Check)
    public bool IsPathBlockedByEdge(Direction dir)
    {
        var edge = GetEdge(dir);
        if (edge == null) return false; // 엣지 데이터 없으면 통과 가능

        return edge.IsBlocking; // 벽이 있고, 안 부서졌으면 True
    }
}