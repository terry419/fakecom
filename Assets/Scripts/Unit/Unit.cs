using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

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

    // 턴 내에서 현재 남아있는 이동력
    public int CurrentMobility { get; private set; }
    // 이번 턴에 이동 행동을 개시했는지 여부
    public bool HasStartedMoving { get; private set; }

    public int Mobility => Data != null ? Data.Mobility : 5;

    private IUnitController _controller;
    public IUnitController Controller => _controller;

    private Collider _collider;

    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 3.0f;
    [SerializeField] private float _rotateSpeed = 15.0f;

    // ========================================================================
    // 2. ITileOccupant 구현
    // ========================================================================
    public OccupantType Type => OccupantType.Unit;
    public bool IsBlockingMovement => true;
    public bool IsCover => false;

    public event Action<bool> OnBlockingChanged = delegate { };
    public event Action<bool> OnCoverChanged = delegate { };

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
            CurrentAP = MaxAP;
        }
        else
        {
            MaxAP = 2;
            CurrentAP = 2;
        }

        ResetState();
    }

    public void SpawnOnMap(GridCoords coords)
    {
        if (!ServiceLocator.TryGet(out MapManager mapManager))
            throw new InvalidOperationException($"[{nameof(Unit)}] MapManager not found.");

        Tile tile = mapManager.GetTile(coords);
        if (tile == null)
            throw new ArgumentException($"[{nameof(Unit)}] Invalid Spawn Coords: {coords}");

        tile.AddOccupant(this);

        Vector3 worldPos = GridUtils.GridToWorld(coords);
        float targetY = GetTargetHeight(worldPos.y);
        transform.position = new Vector3(worldPos.x, targetY, worldPos.z);
    }

    public void OnAddedToTile(Tile tile)
    {
        Coordinate = tile.Coordinate;
        if (Data != null)
            gameObject.name = $"{Data.UnitName}_{Coordinate}";
    }

    public void OnRemovedFromTile(Tile tile) { }

    // ========================================================================
    // 4. 컨트롤러 연결
    // ========================================================================

    public void SetController(IUnitController controller)
    {
        if (_controller != null) _controller.Unpossess();
        _controller = controller;
        if (_controller != null) _controller.Possess(this);
    }

    public async UniTask OnTurnStart()
    {
        ResetAP();
        if (_controller != null) await _controller.OnTurnStart();
    }

    public void OnTurnEnd() => _controller?.OnTurnEnd();

    // ========================================================================
    // 5. 이동 및 행동 로직
    // ========================================================================

    // [Issue #1] 로직 중복 제거를 위한 Helper 메서드
    public int GetAvailableMoveDistance()
    {
        if (HasStartedMoving)
            return CurrentMobility;
        else
            return (CurrentAP >= 1) ? Mobility : 0;
    }

    public async UniTask MovePathAsync(List<GridCoords> path, MapManager mapManager)
    {
        if (path == null || path.Count == 0) return;

        // 1. 이동 시작 전 AP 선불 차감 처리
        if (!HasStartedMoving)
        {
            if (CurrentAP < 1)
            {
                Debug.LogWarning("Not enough AP to start moving.");
                return;
            }
            ConsumeAP(1);
            HasStartedMoving = true;
        }

        // 2. [Issue #2] 거리 검증 추가 (Critical)
        int distance = path.Count;
        if (distance > CurrentMobility)
        {
            Debug.LogError($"[Unit] Invalid Move: Path distance ({distance}) > Current Mobility ({CurrentMobility}). Move rejected.");
            return;
        }

        // 3. 이동력 차감
        CurrentMobility -= distance;

        float speed = _moveSpeed > 0 ? _moveSpeed : 3.0f;

        foreach (var nextCoords in path)
        {
            Tile nextTile = mapManager.GetTile(nextCoords);
            Tile currentTile = mapManager.GetTile(Coordinate);

            if (currentTile != null) currentTile.RemoveOccupant(this);
            if (nextTile != null) nextTile.AddOccupant(this);

            Vector3 targetPos = GridUtils.GridToWorld(nextCoords);
            targetPos.y = GetTargetHeight(targetPos.y);

            Vector3 direction = (targetPos - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                RotateTo(targetRot).Forget();
            }

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                await UniTask.Yield();
            }
            transform.position = targetPos;
        }
    }

    private async UniTaskVoid RotateTo(Quaternion targetRot)
    {
        float t = 0;
        Quaternion startRot = transform.rotation;
        while (t < 1f)
        {
            t += Time.deltaTime * _rotateSpeed;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            await UniTask.Yield();
        }
        transform.rotation = targetRot;
    }

    public void ConsumeAP(int amount)
    {
        CurrentAP = Mathf.Max(0, CurrentAP - amount);
    }

    public void ResetAP()
    {
        CurrentAP = MaxAP > 0 ? MaxAP : 2;
        ResetState();
    }

    private void ResetState()
    {
        CurrentMobility = Mobility;
        HasStartedMoving = false;
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

    // ========================================================================
    // 6. 유틸리티
    // ========================================================================

    private float GetTargetHeight(float surfaceY)
    {
        if (_collider == null) return surfaceY;
        return surfaceY + _collider.bounds.extents.y;
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