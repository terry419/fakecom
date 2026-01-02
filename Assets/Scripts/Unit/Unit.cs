using UnityEngine;
using System;
using Cysharp.Threading.Tasks; // UniTask 사용

// [Body] 데이터와 행동 수행만 담당 (판단 로직 X)
public class Unit : MonoBehaviour, ITileOccupant
{
    // ========================================================================
    // 1. 데이터 및 상태
    // ========================================================================
    public UnitDataSO Data { get; private set; }
    public GridCoords Coordinate { get; private set; }

    public int CurrentHP { get; private set; }
    public int CurrentAP { get; private set; }
    public int MaxAP { get; private set; }

    // [New] 영혼(Controller) 참조
    private IUnitController _controller;
    public IUnitController Controller => _controller;

    private Collider _collider;

    // ========================================================================
    // 2. ITileOccupant 구현
    // ========================================================================
    public OccupantType Type => OccupantType.Unit;
    public bool IsBlockingMovement => true;
    public bool IsCover => false;

    public event Action<bool> OnBlockingChanged;
    public event Action<bool> OnCoverChanged;

    // ========================================================================
    // 3. 초기화 및 스폰
    // ========================================================================

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    public void Initialize(UnitDataSO data)
    {
        Data = data;
        gameObject.name = Data != null ? $"{Data.UnitName}" : "Unit";

        if (Data != null)
        {
            CurrentHP = Data.MaxHP;
            MaxAP = Data.MaxAP;
            CurrentAP = 0;
        }
    }

    public void SpawnOnMap(GridCoords coords)
    {
        if (!ServiceLocator.TryGet(out MapManager mapManager))
            throw new InvalidOperationException($"[{nameof(Unit)}] MapManager not found.");

        Tile tile = mapManager.GetTile(coords);
        if (tile == null)
            throw new ArgumentException($"[{nameof(Unit)}] Invalid Spawn Coords: {coords}");

        tile.AddOccupant(this);
    }

    public void OnAddedToTile(Tile tile)
    {
        Coordinate = tile.Coordinate;

        // 1. 기본 위치 이동 (XZ 기준)
        Vector3 targetPos = GridUtils.GridToWorld(tile.Coordinate);
        transform.position = targetPos;

        // 2. [New] 높이 정밀 보정 (Surface Snap)
        AdjustVerticalPosition(targetPos.y);

        if (Data != null)
            gameObject.name = $"{Data.UnitName}_{Coordinate}";
    }
    private void AdjustVerticalPosition(float surfaceY)
    {
        if (_collider == null) return;

        // [Fix] bounds 대신 로컬 데이터 사용 (Physics Latency 해결)
        if (_collider is BoxCollider box)
        {
            // 피벗에서 발바닥까지의 거리 계산
            // (Center.y - Size.y/2) * Scale.y
            float bottomOffset = (box.center.y - box.size.y * 0.5f) * transform.lossyScale.y;

            // 목표 위치 = 표면 높이 - 발바닥 오프셋
            float targetY = surfaceY - bottomOffset;

            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        }
        else if (_collider is CapsuleCollider capsule)
        {
            // 캡슐의 경우 (보통 Center가 중앙, Height가 전체 키)
            float bottomOffset = (capsule.center.y - capsule.height * 0.5f) * transform.lossyScale.y;
            float targetY = surfaceY - bottomOffset;

            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        }
        // 기타 콜라이더는 기존 방식 사용 (필요 시)
    }

    public void OnRemovedFromTile(Tile tile) { }

    // ========================================================================
    // 5. 컨트롤러 연결 (빙의 시스템 핵심)
    // ========================================================================

    /// <summary>
    /// 이 유닛을 제어할 컨트롤러(영혼)를 설정합니다.
    /// </summary>
    public void SetController(IUnitController controller)
    {
        if (_controller != null) _controller.Unpossess();
        _controller = controller;
        if (_controller != null) _controller.Possess(this);
    }

    public async UniTask OnTurnStart()
    {
        if (_controller != null) await _controller.OnTurnStart();
    }
    public void OnTurnEnd() => _controller?.OnTurnEnd();

    // ========================================================================
    // 6. 상태 관리
    // ========================================================================
    public void ResetAP() => CurrentAP = MaxAP;
    public void ConsumeAP(int amount) => CurrentAP = Mathf.Max(0, CurrentAP - amount);

    public void TakeDamage(int damage)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        if (CurrentHP <= 0)
        {
            Debug.Log($"Unit {name} died.");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet(out MapManager mapManager))
        {
            Tile tile = mapManager.GetTile(Coordinate);
            if (tile != null) tile.RemoveOccupant(this);
        }
        if (ServiceLocator.TryGet(out UnitManager unitManager))
        {
            unitManager.UnregisterUnit(this);
        }
    }
}