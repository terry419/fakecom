using Cysharp.Threading.Tasks; // UniTask 사용
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    // 영혼(Controller) 참조
    private IUnitController _controller;
    public IUnitController Controller => _controller;

    private Collider _collider;

    [Header("Settings")]
    [SerializeField] private float _moveSpeed = 5.0f;

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

        // 2. 높이 정밀 보정 (Surface Snap)
        AdjustVerticalPosition(targetPos.y);

        if (Data != null)
            gameObject.name = $"{Data.UnitName}_{Coordinate}";
    }

    // 콜라이더 크기에 맞춰 발바닥을 바닥(surfaceY)에 딱 붙이는 로직
    private void AdjustVerticalPosition(float surfaceY)
    {
        if (_collider == null) return;

        float bottomOffset = 0f;

        if (_collider is BoxCollider box)
        {
            bottomOffset = (box.center.y - box.size.y * 0.5f) * transform.lossyScale.y;
        }
        else if (_collider is CapsuleCollider capsule)
        {
            bottomOffset = (capsule.center.y - capsule.height * 0.5f) * transform.lossyScale.y;
        }

        // 목표 위치 = 표면 높이 - 발바닥 오프셋
        float targetY = surfaceY - bottomOffset;
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
    }

    // 이동 중에 목표 Y값을 계산하는 헬퍼 (AdjustVerticalPosition과 동일 로직)
    private float GetTargetHeight(float surfaceY)
    {
        if (_collider == null) return surfaceY + 0.05f; // 콜라이더 없으면 살짝 띄움

        float bottomOffset = 0f;
        if (_collider is BoxCollider box)
            bottomOffset = (box.center.y - box.size.y * 0.5f) * transform.lossyScale.y;
        else if (_collider is CapsuleCollider capsule)
            bottomOffset = (capsule.center.y - capsule.height * 0.5f) * transform.lossyScale.y;

        return surfaceY - bottomOffset;
    }

    public void OnRemovedFromTile(Tile tile) { }

    // ========================================================================
    // 5. 컨트롤러 연결 (빙의 시스템 핵심)
    // ========================================================================

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

    public async UniTask MovePathAsync(List<GridCoords> path, MapManager mapManager)
    {
        if (path == null || path.Count == 0) return;

        foreach (var nextCoords in path)
        {
            // [Priority 3] 이동 경로상 장애물(다른 유닛 등) 체크
            Tile nextTile = mapManager.GetTile(nextCoords);
            // Tile.Occupants가 IReadOnlyList로 변경되었으므로 .Count 사용 가능
            if (nextTile != null && nextTile.Occupants.Count > 0)
            {
                Debug.LogWarning($"Path blocked at {nextCoords}");
                break;
            }

            // 1. 점유 상태 갱신
            Tile currentTile = mapManager.GetTile(Coordinate);
            if (currentTile != null)
            {
                try { currentTile.RemoveOccupant(this); }
                catch { /* 로그 생략 */ }
            }

            if (nextTile != null) nextTile.AddOccupant(this);

            Coordinate = nextCoords;

            // 2. 물리적 이동 (애니메이션)
            Vector3 targetPos = GridUtils.GridToWorld(nextCoords);

            // [Fix] 하드코딩(0.05f) 대신 콜라이더 기준 높이 계산 적용
            targetPos.y = GetTargetHeight(targetPos.y);

            // 부드러운 이동
            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, _moveSpeed * Time.deltaTime);
                await UniTask.Yield();
            }
            transform.position = targetPos; // 오차 보정

            // 3. AP 차감
            ConsumeAP(1);
        }
    }

    public void ConsumeAP(int amount)
    {
        CurrentAP = Mathf.Max(0, CurrentAP - amount);
    }

    public void ResetAP()
    {
        CurrentAP = MaxAP > 0 ? MaxAP : 2; // 데이터 없으면 임시 2
    }

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